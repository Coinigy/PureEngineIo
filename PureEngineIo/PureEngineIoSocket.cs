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

        public static readonly int Protocol = Parser.Parser.Protocol;

        public static bool PriorWebsocketSuccess = false;

        internal bool Secure;
        internal bool Upgrade;
        internal bool TimestampRequests = true;
        internal bool Upgrading;
        internal bool RememberUpgrade;
        internal int Port;
        internal int PolicyPort;
        internal int PrevBufferLen;
        internal long PingInterval;
        internal long PingTimeout;
        public string Id;
        internal string Hostname;
        internal string Path;
        internal string TimestampParam;
        internal ImmutableList<string> Transports;
        internal ImmutableList<string> Upgrades;
        internal Dictionary<string, string> Query;
        internal ImmutableList<Packet> WriteBuffer = ImmutableList<Packet>.Empty;
        internal ImmutableList<Action> CallbackBuffer = ImmutableList<Action>.Empty;
        internal Dictionary<string, string> Cookies = new Dictionary<string, string>();

        public Transport Transport;
        private EasyTimer PingTimeoutTimer;
        private EasyTimer PingIntervalTimer;

        internal ReadyStateEnum ReadyState;
        internal bool Agent = false;
        internal bool ForceBase64 = false;
        internal bool ForceJsonp = false;

        public Dictionary<string, string> ExtraHeaders;

        public PureEngineIoSocket() : this(new PureEngineIoOptions())
        {
        }

        public PureEngineIoSocket(string uri) : this(uri, null)
        {
        }

        public PureEngineIoSocket(string uri, PureEngineIoOptions options) : this(uri == null ? null : String2Uri(uri), options)
        {
        }

        private static Uri String2Uri(string uri)
        {
            if (uri.StartsWith("http") || uri.StartsWith("ws"))
            {
                return new Uri(uri);
            }
            else
            {
                return new Uri("http://" + uri);
            }
        }

        public PureEngineIoSocket(Uri uri, PureEngineIoOptions options) : this(uri == null ? options : PureEngineIoOptions.FromURI(uri, options))
        {
        }

        public PureEngineIoSocket(PureEngineIoOptions options)
        {
            if (options.Host != null)
            {
                var pieces = options.Host.Split(':');
                options.Hostname = pieces[0];
                if (pieces.Length > 1)
                {
                    options.Port = int.Parse(pieces[pieces.Length - 1]);
                }
            }

            Secure = options.Secure;
            Hostname = options.Hostname;
            Port = options.Port;
            Query = options.QueryString != null ? Helpers.DecodeQuerystring(options.QueryString) : new Dictionary<string, string>();

            if (options.Query != null)
            {
                foreach (var item in options.Query)
                {
                    Query.Add(item.Key, item.Value);
                }
            }

            Upgrade = options.Upgrade;
            Path = (options.Path ?? "/engine.io").Replace("/$", "") + "/";
            TimestampParam = (options.TimestampParam ?? "t");
            TimestampRequests = options.TimestampRequests;
            Transports = options.Transports ?? ImmutableList<string>.Empty.Add(Polling.NAME).Add(WebSocket.NAME);
            PolicyPort = options.PolicyPort != 0 ? options.PolicyPort : 843;
            RememberUpgrade = options.RememberUpgrade;
            Cookies = options.Cookies;
            //if (options.IgnoreServerCertificateValidation)
            //{
            //    ServerCertificate.IgnoreServerCertificateValidation();
            //}
            ExtraHeaders = options.ExtraHeaders;
        }

        public PureEngineIoSocket Open()
        {
            string transportName;
            if (RememberUpgrade && PriorWebsocketSuccess && Transports.Contains(WebSocket.NAME))
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
                //TODO: logging
                Console.WriteLine("Task.Run Open start");
                transport.Open();
                Console.WriteLine("Task.Run Open finish");
            });
            return this;
        }

        internal Transport CreateTransport(string name)
        {
            var query = new Dictionary<string, string>(Query);
            query.Add("EIO", Parser.Parser.Protocol.ToString());
            query.Add("transport", name);
            if (Id != null)
            {
                query.Add("sid", Id);
            }
            var options = new PureEngineIoTransportOptions
            {
                Hostname = Hostname,
                Port = Port,
                Secure = Secure,
                Path = Path,
                Query = query,
                TimestampRequests = TimestampRequests,
                TimestampParam = TimestampParam,
                PolicyPort = PolicyPort,
                Socket = this,
                Agent = this.Agent,
                ForceBase64 = this.ForceBase64,
                ForceJsonp = this.ForceJsonp,
                Cookies = this.Cookies,
                ExtraHeaders = this.ExtraHeaders
            };

            if (name == WebSocket.NAME)
            {
                return new WebSocket(options);
            }
            else if (name == Polling.NAME)
            {
                return new PollingXHR(options);
            }

            throw new PureEngineIOException("CreateTransport failed");
        }

        internal void SetTransport(Transport transport)
        {
            //TODO: logging
            Console.WriteLine(string.Format("SetTransport setting transport '{0}'", transport.Name));

            if (this.Transport != null)
            {
                Console.WriteLine(string.Format("SetTransport clearing existing transport '{0}'", transport.Name));
                this.Transport.Off();
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
            //var log = LogManager.GetLogger(Global.CallerName());
            //log.Info(string.Format("OnDrain1 PrevBufferLen={0} WriteBuffer.Count={1}", PrevBufferLen, WriteBuffer.Count));

            for (int i = 0; i < PrevBufferLen; i++)
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
            //log.Info(string.Format("OnDrain2 PrevBufferLen={0} WriteBuffer.Count={1}", PrevBufferLen, WriteBuffer.Count));

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
            //log.Info(string.Format("OnDrain3 PrevBufferLen={0} WriteBuffer.Count={1}", PrevBufferLen, WriteBuffer.Count));

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
            //TODO: logging
            Console.WriteLine (string.Format("ReadyState={0} Transport.Writeable={1} Upgrading={2} WriteBuffer.Count={3}", ReadyState, Transport.Writable, Upgrading, WriteBuffer.Count));
            if (ReadyState != ReadyStateEnum.CLOSED && ReadyState == ReadyStateEnum.OPEN && this.Transport.Writable && !Upgrading && WriteBuffer.Count != 0)
            {
                Console.WriteLine(string.Format("Flush {0} packets in socket", WriteBuffer.Count));
                PrevBufferLen = WriteBuffer.Count;
                Transport.Send(WriteBuffer);
                Emit(EVENT_FLUSH);
                return true;
            }
            else
            {
                Console.WriteLine(string.Format("Flush Not Send"));
                return false;
            }
        }

        public void OnPacket(Packet packet)
        {
            if (ReadyState == ReadyStateEnum.OPENING || ReadyState == ReadyStateEnum.OPEN)
            {
                //TODO: logging
                Console.WriteLine(string.Format("socket received: type '{0}', data '{1}'", packet.Type, packet.Data));

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
                        code = packet.Data
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
                Console.WriteLine(string.Format("OnPacket packet received with socket readyState '{0}'", ReadyState));
            }
        }

        internal void OnHandshake(HandshakeData handshakeData)
        {
            //TODO: logging
            Console.WriteLine(nameof(OnHandshake));
            Emit(EVENT_HANDSHAKE, handshakeData);
            Id = handshakeData.Sid;
            Transport.Query.Add("sid", handshakeData.Sid);
            Upgrades = FilterUpgrades(handshakeData.Upgrades);
            PingInterval = handshakeData.PingInterval;
            PingTimeout = handshakeData.PingTimeout;
            OnOpen();
            // In case open handler closes socket
            if (ReadyStateEnum.CLOSED == this.ReadyState)
            {
                return;
            }
            SetPing();

            Off(EVENT_HEARTBEAT, new OnHeartbeatAsListener(this));
            On(EVENT_HEARTBEAT, new OnHeartbeatAsListener(this));

        }

        internal void SetPing()
        {
            //var log = LogManager.GetLogger(Global.CallerName());

            if (PingIntervalTimer != null)
            {
                PingIntervalTimer.Stop();
            }
            //TODO: logging
            Console.WriteLine(string.Format("writing ping packet - expecting pong within {0}ms", PingTimeout));

            PingIntervalTimer = EasyTimer.SetTimeout(() =>
            {
                //TODO: logging
                Console.WriteLine("EasyTimer SetPing start");

                if (Upgrading)
                {
                    // skip this ping during upgrade
                    SetPing();
                    Console.WriteLine("skipping Ping during upgrade");
                }
                else if (ReadyState == ReadyStateEnum.OPEN)
                {
                    Ping();
                    OnHeartbeat(PingTimeout);
                    Console.WriteLine("EasyTimer SetPing finish");
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
            //var log = LogManager.GetLogger(Global.CallerName());
            //log.Info(string.Format("SendPacket WriteBuffer.Add(packet) packet ={0}",packet.Type));
            WriteBuffer = WriteBuffer.Add(packet);
            CallbackBuffer = CallbackBuffer.Add(fn);
            Flush();
        }

        internal Task WaitForUpgrade()
        {
            var tcs = new TaskCompletionSource<object>();
            const int TIMEOUT = 1000;
            var sw = new System.Diagnostics.Stopwatch();

            try
            {
                sw.Start();
                while (Upgrading)
                {
                    if (sw.ElapsedMilliseconds > TIMEOUT)
                    {
                        //TODO: logging
                        Console.WriteLine("Wait for upgrade timeout");
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
            //TODO: logging
            //log.Info("socket open before call to flush()");
            ReadyState = ReadyStateEnum.OPEN;
            PriorWebsocketSuccess = WebSocket.NAME == Transport.Name;

            Flush();
            Emit(EVENT_OPEN);


            if (ReadyState == ReadyStateEnum.OPEN && Upgrade && Transport is Polling)
            //if (ReadyState == ReadyStateEnum.OPEN && Upgrade && this.Transport)
            {
                Console.WriteLine("OnOpen starting upgrade probes");
                _errorCount = 0;
                foreach (var upgrade in Upgrades)
                {
                    Probe(upgrade);
                }
            }
        }

        internal void Probe(string name)
        {
            //TODO: logging
            Console.WriteLine(string.Format("Probe probing transport '{0}'", name));

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

            this.Once(EVENT_CLOSE, onClose);
            this.Once(EVENT_UPGRADING, onUpgrade);

            parameters.Transport[0].Open();
        }

        public PureEngineIoSocket Close()
        {
            if (ReadyState == ReadyStateEnum.OPENING || ReadyState == ReadyStateEnum.OPEN)
            {
                //TODO: logging
                Console.WriteLine("Start");
                this.OnClose("forced close");

                Console.WriteLine("socket closing - telling transport to close");
                Transport.Close();

            }
            return this;
        }

        internal void OnClose(string reason, Exception desc = null)
        {
            if (ReadyState == ReadyStateEnum.OPENING || ReadyState == ReadyStateEnum.OPEN)
            {
                //TODO: logging
                Console.WriteLine(string.Format("OnClose socket close with reason: {0}", reason));

                // clear timers
                if (PingIntervalTimer != null)
                {
                    PingIntervalTimer.Stop();
                }
                if (PingTimeoutTimer != null)
                {
                    PingTimeoutTimer.Stop();
                }


                //WriteBuffer = WriteBuffer.Clear();
                //CallbackBuffer = CallbackBuffer.Clear();
                //PrevBufferLen = 0;

                EasyTimer.SetTimeout(() =>
                {
                    WriteBuffer = ImmutableList<Packet>.Empty;
                    CallbackBuffer = ImmutableList<Action>.Empty;
                    PrevBufferLen = 0;
                }, 1);


                if (this.Transport != null)
                {
                    // stop event from firing again for transport
                    this.Transport.Off(EVENT_CLOSE);

                    // ensure transport won't stay open
                    this.Transport.Close();

                    // ignore further transport communication
                    this.Transport.Off();
                }

                // set ready state
                this.ReadyState = ReadyStateEnum.CLOSED;

                // clear session id
                this.Id = null;

                // emit close events
                this.Emit(EVENT_CLOSE, reason, desc);
            }
        }

        public ImmutableList<string> FilterUpgrades(IEnumerable<string> upgrades)
        {
            var filterUpgrades = ImmutableList<string>.Empty;
            foreach (var upgrade in upgrades)
            {
                if (Transports.Contains(upgrade))
                {
                    filterUpgrades = filterUpgrades.Add(upgrade);
                }
            }
            return filterUpgrades;
        }

        internal void OnHeartbeat(long timeout)
        {
            if (PingTimeoutTimer != null)
            {
                PingTimeoutTimer.Stop();
                PingTimeoutTimer = null;
            }

            if (timeout <= 0)
            {
                timeout = PingInterval + PingTimeout;
            }

            PingTimeoutTimer = EasyTimer.SetTimeout(() =>
            {
                //TODO: logging
                Console.WriteLine("EasyTimer OnHeartbeat start");
                if (ReadyState == ReadyStateEnum.CLOSED)
                {
                    Console.WriteLine("EasyTimer OnHeartbeat ReadyState == ReadyStateEnum.CLOSED finish");
                    return;
                }
                OnClose("ping timeout");
                Console.WriteLine("EasyTimer OnHeartbeat finish");
            }, (int)timeout);

        }

        private int _errorCount = 0;

        internal void OnError(Exception exception)
        {
            //TODO: logging
            Console.WriteLine("socket error", exception);
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
