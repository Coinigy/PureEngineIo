using PureEngineIo.EmitterImp;
using PureEngineIo.Exceptions;
using PureEngineIo.Listeners;
using PureEngineIo.Parser;
using PureEngineIo.Thread;
using PureEngineIo.Transports.PollingImp;
using PureEngineIo.Transports.PollingXHRImp;
using PureEngineIo.Transports.WebSocketImp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Threading.Tasks;

namespace PureEngineIo
{
	public class PureEngineIoSocket : Emitter
	{
		public static readonly string EVENT_OPEN = "open";
		public static readonly string EVENT_CLOSE = "close";
		public static readonly string EVENT_PACKET = "packet";
		public static readonly string EVENT_DRAIN = "drain";
		public static readonly string EVENT_ERROR = "error";
		public static readonly string EVENT_DATA = "data";
		public static readonly string EVENT_MESSAGE = "message";
		public static readonly string EVENT_UPGRADE_ERROR = "upgradeError";
		public static readonly string EVENT_FLUSH = "flush";
		public static readonly string EVENT_HANDSHAKE = "handshake";
		public static readonly string EVENT_UPGRADING = "upgrading";
		public static readonly string EVENT_UPGRADE = "upgrade";
		public static readonly string EVENT_PACKET_CREATE = "packetCreate";
		public static readonly string EVENT_HEARTBEAT = "heartbeat";
		public static readonly string EVENT_TRANSPORT = "transport";

		public static bool PriorWebsocketSuccess;

		internal bool Upgrading;
		internal int PrevBufferLen;
		internal long PingInterval;
		internal long PingTimeout;
		public string Id;
		internal ImmutableList<string> Transports;
		internal ImmutableList<string> Upgrades;
		internal ImmutableList<Packet> WriteBuffer = ImmutableList<Packet>.Empty;
		internal ImmutableList<Action> CallbackBuffer = ImmutableList<Action>.Empty;

		public Transport Transport;
		private EasyTimer _pingTimeoutTimer;
		private EasyTimer _pingIntervalTimer;

		internal ReadyStateEnum ReadyState;
		internal static PureEngineIoOptions Options;

		public PureEngineIoSocket(string uri, PureEngineIoOptions options) : this(uri == null ? null : String2Uri(uri), options)
		{
			if(Options.Serializer == null)
				Options.Serializer = new Utf8JsonSerializer();
		}

		private static Uri String2Uri(string uri) => uri.StartsWith("http") || uri.StartsWith("ws") ? new Uri(uri) : new Uri("http://" + uri);

		public PureEngineIoSocket(Uri uri, PureEngineIoOptions options) : this(uri == null ? options : PureEngineIoOptions.FromURI(uri, options))
		{
			if (Options.Serializer == null)
				Options.Serializer = new Utf8JsonSerializer();
		}

		public PureEngineIoSocket(PureEngineIoOptions options)
		{
			if (Options.Serializer == null)
				Options.Serializer = new Utf8JsonSerializer();

			Options = options;
			
			if (Options.Host != null)
			{
				var pieces = options.Host.Split(':');
				Options.Hostname = pieces[0];
				if (pieces.Length > 1)
				{
					Options.Port = int.Parse(pieces[pieces.Length - 1]);
				}
			}

			if(Options.Query == null || Options.Query.Count  <= 0)
				Options.Query = Options.QueryString != null ? Helpers.DecodeQuerystring(Options.QueryString) : new Dictionary<string, string>();

			Options.Path = (Options.Path ?? "/engine.io").Replace("/$", "") + "/";
			Options.TimestampParam = (Options.TimestampParam ?? "t");
			Options.Transports = Options.Transports ?? ImmutableList<string>.Empty.Add(Polling.NAME).Add(WebSocket.NAME);
			Options.PolicyPort = Options.PolicyPort != 0 ? Options.PolicyPort : 843;
			//if (options.IgnoreServerCertificateValidation)
			//{
			//    ServerCertificate.IgnoreServerCertificateValidation();
			//}
		}

		public PureEngineIoSocket Open()
		{
			string transportName;
			if (Options.RememberUpgrade && PriorWebsocketSuccess && Transports.Contains(WebSocket.NAME))
			{
				transportName = WebSocket.NAME;
			}
			else
			{
				transportName = Transports[0];
			}
			ReadyState = ReadyStateEnum.OPENING;
			var transport = CreateTransport(transportName);
			SetTransport(transport);

			Task.Run(() =>
			{
				Logger.Log("Task.Run Open start");
				transport.Open();
				Logger.Log("Task.Run Open finish");
			});
			return this;
		}

		internal Transport CreateTransport(string name)
		{
			var query = new Dictionary<string, string>(Options.Query) {{"EIO", Parser.Parser.Protocol.ToString()}, {"transport", name}};
			if (Id != null)
			{
				query.Add("sid", Id);
			}
			var options = new PureEngineIoTransportOptions
			{
				Hostname = Options.Hostname,
				Port = Options.Port,
				Secure = Options.Secure,
				Path = Options.Path,
				Query = query,
				TimestampRequests = Options.TimestampRequests,
				TimestampParam = Options.TimestampParam,
				PolicyPort = Options.PolicyPort,
				Socket = this,
				Agent = Options.Agent,
				ForceBase64 = Options.ForceBase64,
				ForceJsonp = Options.ForceJsonp,
				Cookies = Options.Cookies,
				ExtraHeaders = Options.ExtraHeaders
			};

			if (name == WebSocket.NAME)
			{
				return new WebSocket(options);
			}

			if (name == Polling.NAME)
			{
				return new PollingXHR(options);
			}

			throw new PureEngineIOException("CreateTransport failed");
		}

		internal void SetTransport(Transport transport)
		{
			Logger.Log($"SetTransport setting transport '{transport.Name}'");

			if (Transport != null)
			{
				Logger.Log($"SetTransport clearing existing transport '{transport.Name}'");
				Transport.Off();
			}

			Transport = transport;

			Emit(EVENT_TRANSPORT, transport);

			transport.On(EVENT_DRAIN, new EventDrainListener(this));
			transport.On(EVENT_PACKET, new EventPacketListener(this));
			transport.On(EVENT_ERROR, new EventErrorListener(this));
			transport.On(EVENT_CLOSE, new EventCloseListener(this));
		}

		internal void OnDrain()
		{
			Logger.Log($"OnDrain1 PrevBufferLen={PrevBufferLen} WriteBuffer.Count={WriteBuffer.Count}");

			for (var i = 0; i < PrevBufferLen; i++)
			{
				try
				{
					CallbackBuffer[i]?.Invoke();
				}
				catch (ArgumentOutOfRangeException)
				{
					WriteBuffer = WriteBuffer.Clear();
					CallbackBuffer = CallbackBuffer.Clear();
					PrevBufferLen = 0;
				}
			}

			Logger.Log($"OnDrain2 PrevBufferLen={PrevBufferLen} WriteBuffer.Count={WriteBuffer.Count}");

			try
			{
				WriteBuffer = WriteBuffer.RemoveRange(0, PrevBufferLen);
				CallbackBuffer = CallbackBuffer.RemoveRange(0, PrevBufferLen);
			}
			catch (Exception)
			{
				WriteBuffer = WriteBuffer.Clear();
				CallbackBuffer = CallbackBuffer.Clear();
			}

			PrevBufferLen = 0;

			Logger.Log($"OnDrain3 PrevBufferLen={PrevBufferLen} WriteBuffer.Count={WriteBuffer.Count}");

			if (WriteBuffer.Count == 0)
			{
				Emit(EVENT_DRAIN);
			}
			else
			{
				Flush();
			}
		}

		internal bool Flush()
		{
			Logger.Log($"ReadyState={ReadyState} Transport.Writeable={Transport.Writable} Upgrading={Upgrading} WriteBuffer.Count={WriteBuffer.Count}");
			if (ReadyState != ReadyStateEnum.CLOSED && ReadyState == ReadyStateEnum.OPEN && Transport.Writable && !Upgrading && WriteBuffer.Count != 0)
			{
				Logger.Log($"Flush {WriteBuffer.Count} packets in socket");
				PrevBufferLen = WriteBuffer.Count;
				Transport.Send(WriteBuffer);
				Emit(EVENT_FLUSH);
				return true;
			}

			Logger.Log("Flush Not Send");
			return false;
		}

		public void OnPacket(Packet packet)
		{
			if (ReadyState == ReadyStateEnum.OPENING || ReadyState == ReadyStateEnum.OPEN)
			{
				Logger.Log($"socket received: type '{packet.Type}', data '{packet.Data}'");

				Emit(EVENT_PACKET, packet);
				Emit(EVENT_HEARTBEAT);

				if (packet.Type == Packet.OPEN)
				{
					OnHandshake(HandshakeData.FromString((string)packet.Data));

				}
				else if (packet.Type == Packet.PONG)
				{
					SetPing();
				}
				else if (packet.Type == Packet.ERROR)
				{
					var err = new PureEngineIOException("server error")
					{
						Code = packet.Data
					};
					Emit(EVENT_ERROR, err);
				}
				else if (packet.Type == Packet.MESSAGE)
				{
					Emit(EVENT_DATA, packet.Data);
					Emit(EVENT_MESSAGE, packet.Data);
				}
			}
			else
			{
				Logger.Log($"OnPacket packet received with socket readyState '{ReadyState}'");
			}
		}

		internal void OnHandshake(HandshakeData handshakeData)
		{
			Logger.Log(nameof(OnHandshake));
			Emit(EVENT_HANDSHAKE, handshakeData);
			Id = handshakeData.Sid;
			Transport.Query.Add("sid", handshakeData.Sid);
			Upgrades = FilterUpgrades(handshakeData.Upgrades);
			PingInterval = handshakeData.PingInterval;
			PingTimeout = handshakeData.PingTimeout;
			OnOpen();
			// In case open handler closes socket
			if (ReadyStateEnum.CLOSED == ReadyState)
			{
				return;
			}
			SetPing();

			Off(EVENT_HEARTBEAT, new OnHeartbeatAsListener(this));
			On(EVENT_HEARTBEAT, new OnHeartbeatAsListener(this));
		}

		internal void SetPing()
		{
			_pingIntervalTimer?.Stop();

			Logger.Log($"writing ping packet - expecting pong within {PingTimeout}ms");

			_pingIntervalTimer = EasyTimer.SetTimeout(() =>
			{
				Logger.Log("EasyTimer SetPing start");

				if (Upgrading)
				{
					// skip this ping during upgrade
					SetPing();
					Logger.Log("skipping Ping during upgrade");
				}
				else if (ReadyState == ReadyStateEnum.OPEN)
				{
					Ping();
					OnHeartbeat(PingTimeout);
					Logger.Log("EasyTimer SetPing finish");
				}
			}, (int)PingInterval);
		}

		internal void Ping() => SendPacket(Packet.PING);

		public void Write(string msg, Action fn = null) => Send(msg, fn);

		public void Write(byte[] msg, Action fn = null) => Send(msg, fn);

		public void Send(string msg, Action fn = null) => SendPacket(Packet.MESSAGE, msg, fn);

		public void Send(byte[] msg, Action fn = null) => SendPacket(Packet.MESSAGE, msg, fn);

		internal void SendPacket(string type) => SendPacket(new Packet(type), null);

		internal void SendPacket(string type, string data, Action fn) => SendPacket(new Packet(type, data), fn);

		internal void SendPacket(string type, byte[] data, Action fn) => SendPacket(new Packet(type, data), fn);

		internal void SendPacket(Packet packet, Action fn)
		{
			if (fn == null)
			{
				fn = () => { };
			}

			if (Upgrading)
			{
				WaitForUpgrade().Wait();
			}

			Emit(EVENT_PACKET_CREATE, packet);
			Logger.Log($"SendPacket WriteBuffer.Add(packet) packet ={packet.Type}");
			WriteBuffer = WriteBuffer.Add(packet);
			CallbackBuffer = CallbackBuffer.Add(fn);
			Flush();
		}

		internal Task WaitForUpgrade()
		{
			var tcs = new TaskCompletionSource<object>();
			const int timeout = 1000;
			var sw = new System.Diagnostics.Stopwatch();

			try
			{
				sw.Start();
				while (Upgrading)
				{
					if (sw.ElapsedMilliseconds > timeout)
					{
						Logger.Log("Wait for upgrade timeout");
						break;
					}
				}
				tcs.SetResult(null);
			}
			finally
			{
				sw.Stop();
			}

			return tcs.Task;
		}

		internal void OnOpen()
		{
			Logger.Log("socket open before call to flush()");
			ReadyState = ReadyStateEnum.OPEN;
			PriorWebsocketSuccess = WebSocket.NAME == Transport.Name;

			Flush();
			Emit(EVENT_OPEN);


			if (ReadyState == ReadyStateEnum.OPEN && Options.Upgrade && Transport is Polling)
			//if (ReadyState == ReadyStateEnum.OPEN && Upgrade && this.Transport)
			{
				Logger.Log("OnOpen starting upgrade probes");
				_errorCount = 0;
				foreach (var upgrade in Upgrades)
				{
					Probe(upgrade);
				}
			}
		}

		internal void Probe(string name)
		{
			Logger.Log($"Probe probing transport '{name}'");

			PriorWebsocketSuccess = false;

			var transport = CreateTransport(name);
			var parameters = new ProbeParameters
			{
				Transport = ImmutableList<Transport>.Empty.Add(transport),
				Failed = ImmutableList<bool>.Empty.Add(false),
				Cleanup = ImmutableList<Action>.Empty,
				Socket = this
			};

			var onTransportOpen = new OnTransportOpenListener(parameters);
			var freezeTransport = new FreezeTransportListener(parameters);

			// Handle any error that happens while probing
			var onError = new ProbingOnErrorListener(this, parameters.Transport, freezeTransport);
			var onTransportClose = new ProbingOnTransportCloseListener(onError);

			// When the socket is closed while we're probing
			var onClose = new ProbingOnCloseListener(onError);

			var onUpgrade = new ProbingOnUpgradeListener(freezeTransport, parameters.Transport);

			parameters.Cleanup = parameters.Cleanup.Add(() =>
			{
				if (parameters.Transport.Count < 1)
				{
					return;
				}

				parameters.Transport[0].Off(Transport.EVENT_OPEN, onTransportOpen);
				parameters.Transport[0].Off(Transport.EVENT_ERROR, onError);
				parameters.Transport[0].Off(Transport.EVENT_CLOSE, onTransportClose);
				Off(EVENT_CLOSE, onClose);
				Off(EVENT_UPGRADING, onUpgrade);
			});

			parameters.Transport[0].Once(Transport.EVENT_OPEN, onTransportOpen);
			parameters.Transport[0].Once(Transport.EVENT_ERROR, onError);
			parameters.Transport[0].Once(Transport.EVENT_CLOSE, onTransportClose);

			Once(EVENT_CLOSE, onClose);
			Once(EVENT_UPGRADING, onUpgrade);

			parameters.Transport[0].Open();
		}

		public PureEngineIoSocket Close()
		{
			if (ReadyState == ReadyStateEnum.OPENING || ReadyState == ReadyStateEnum.OPEN)
			{
				Logger.Log("Start");
				OnClose("forced close");

				Logger.Log("socket closing - telling transport to close");
				Transport.Close();

			}
			return this;
		}

		internal void OnClose(string reason, Exception desc = null)
		{
			if (ReadyState == ReadyStateEnum.OPENING || ReadyState == ReadyStateEnum.OPEN)
			{
				Logger.Log($"OnClose socket close with reason: {reason}");

				// clear timers
				_pingIntervalTimer?.Stop();
				_pingTimeoutTimer?.Stop();

				//WriteBuffer = WriteBuffer.Clear();
				//CallbackBuffer = CallbackBuffer.Clear();
				//PrevBufferLen = 0;

				EasyTimer.SetTimeout(() =>
				{
					WriteBuffer = ImmutableList<Packet>.Empty;
					CallbackBuffer = ImmutableList<Action>.Empty;
					PrevBufferLen = 0;
				}, 1);

				if (Transport != null)
				{
					// stop event from firing again for transport
					Transport.Off(EVENT_CLOSE);

					// ensure transport won't stay open
					Transport.Close();

					// ignore further transport communication
					Transport.Off();
				}

				// set ready state
				ReadyState = ReadyStateEnum.CLOSED;

				// clear session id
				Id = null;

				// emit close events
				Emit(EVENT_CLOSE, reason, desc);
			}
		}

		public ImmutableList<string> FilterUpgrades(IEnumerable<string> upgrades) => upgrades.Where(upgrade => Transports.Contains(upgrade)).Aggregate(ImmutableList<string>.Empty, (current, upgrade) => current.Add(upgrade));

		internal void OnHeartbeat(long timeout)
		{
			if (_pingTimeoutTimer != null)
			{
				_pingTimeoutTimer.Stop();
				_pingTimeoutTimer = null;
			}

			if (timeout <= 0)
			{
				timeout = PingInterval + PingTimeout;
			}

			_pingTimeoutTimer = EasyTimer.SetTimeout(() =>
			{
				Logger.Log("EasyTimer OnHeartbeat start");
				if (ReadyState == ReadyStateEnum.CLOSED)
				{
					Logger.Log("EasyTimer OnHeartbeat ReadyState == ReadyStateEnum.CLOSED finish");
					return;
				}
				OnClose("ping timeout");
				Logger.Log("EasyTimer OnHeartbeat finish");
			}, (int)timeout);

		}

		private int _errorCount;

		internal void OnError(Exception exception)
		{
			Logger.Log(exception);
			PriorWebsocketSuccess = false;

			//prevent endless loop
			if (_errorCount == 0)
			{
				_errorCount++;
				Emit(EVENT_ERROR, exception);
				OnClose("transport error", exception);
			}
		}
	}
}
