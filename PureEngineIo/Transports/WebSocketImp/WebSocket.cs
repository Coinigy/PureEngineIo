using PureEngineIo.Parser;
using PureWebSockets;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace PureEngineIo.Transports.WebSocketImp
{
	public class WebSocket : Transport
	{
		public static readonly string NAME = "websocket";

		internal PureWebSocket Ws;
		private readonly string _cookies;
		private readonly List<Tuple<string, string>> _myExtraHeaders;

		public WebSocket(PureEngineIoTransportOptions opts) : base(opts)
		{
			Name = NAME;
			_cookies = opts.GetCookiesAsString();
			_myExtraHeaders = new List<Tuple<string, string>>();
			foreach (var header in opts.ExtraHeaders)
			{
				_myExtraHeaders.Add(new Tuple<string, string>(header.Key, header.Value));
			}
		}

		protected override void DoOpen()
		{
			Logger.Log("DoOpen uri =" + Uri());

			var myheaders = new List<Tuple<string, string>>();
			myheaders.AddRange(_myExtraHeaders);
			myheaders.Add(new Tuple<string, string>("Cookie", _cookies));

			var options = new PureWebSocketOptions
			{
				Headers = myheaders
			};

			Ws = new PureWebSocket(Uri(), options);

			Ws.OnOpened += Ws_OnOpened;
			Ws.OnClosed += Ws_OnClosed;
			Ws.OnData += Ws_OnData;
			Ws.OnMessage += Ws_OnMessage;
			Ws.OnError += Ws_OnError;

			Ws.Connect();
		}

		private void Ws_OnError(Exception ex) => OnError("websocket error", ex);

		private void Ws_OnMessage(string message)
		{
			Logger.Log("ws_MessageReceived e.Message= " + message);
			OnData(message);
		}

		private void Ws_OnData(byte[] data)
		{
			// only really needed for binary
			Logger.Log("ws_DataReceived " + System.Text.Encoding.UTF8.GetString(data));
			// TODO 
			if (data.Length == 0)
			{
				return;
			}
			OnData(data);
		}

		private void Ws_OnClosed(System.Net.WebSockets.WebSocketCloseStatus reason)
		{
			Logger.Log("ws_Closed");
			Ws.OnOpened -= Ws_OnOpened;
			Ws.OnClosed -= Ws_OnClosed;
			Ws.OnData -= Ws_OnData;
			Ws.OnMessage -= Ws_OnMessage;
			Ws.OnError -= Ws_OnError;
			OnClose();
		}

		private void Ws_OnOpened()
		{
			Logger.Log("ws_Opened ");
			OnOpen();
		}

		protected internal override void Write(ImmutableList<Packet> packets)
		{
			Writable = false;
			foreach (var packet in packets)
			{
				Parser.Parser.EncodePacket(packet, new WriteEncodeCallback(this));
			}

			// fake drain
			// defer to next tick to allow Socket to clear writeBuffer
			//EasyTimer.SetTimeout(() =>
			//{
			Writable = true;
			Emit(EVENT_DRAIN);
			//}, 1);
		}

		protected override void DoClose()
		{
			if (Ws != null)
			{
				try
				{
					Ws.Disconnect();
				}
				catch (Exception e)
				{
					Logger.Log("DoClose ws.Close() Exception= " + e.Message);
				}
			}
		}

		public string Uri()
		{
			var query = Query == null ? new Dictionary<string, string>() : new Dictionary<string, string>(Query);
			var schema = Secure ? "wss" : "ws";
			var portString = "";

			if (TimestampRequests)
			{
				query.Add(TimestampParam, DateTime.Now.Ticks + "-" + Timestamps++);
			}

			var _query = Helpers.EncodeQuerystring(query);

			if (Port > 0 && (("wss" == schema && Port != 443) || ("ws" == schema && Port != 80)))
			{
				portString = ":" + Port;
			}

			if (_query.Length > 0)
			{
				_query = "?" + _query;
			}

			return schema + "://" + Hostname + portString + Path + _query;
		}
	}
}
