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

        internal PureWebSocket ws;
        private string Cookies;
        private List<Tuple<string, string>> MyExtraHeaders;

        public WebSocket(PureEngineIoTransportOptions opts) : base(opts)
        {
            Name = NAME;
            Cookies = opts.GetCookiesAsString();
            MyExtraHeaders = new List<Tuple<string, string>>();
            foreach (var header in opts.ExtraHeaders)
            {
                MyExtraHeaders.Add(new Tuple<string, string>(header.Key, header.Value));
            }
        }

        protected override void DoOpen()
        {
            //TODO: logging
            Console.WriteLine("DoOpen uri =" + Uri());

            var myheaders = new List<Tuple<string, string>>();
            myheaders.AddRange(MyExtraHeaders);
            myheaders.Add(new Tuple<string, string>("Cookie", Cookies));

            var options = new PureWebSocketOptions();
            options.Headers = myheaders;

            ws = new PureWebSocket(Uri(), options);

            ws.OnOpened += Ws_OnOpened;
            ws.OnClosed += Ws_OnClosed;
            ws.OnData += Ws_OnData;
            ws.OnMessage += Ws_OnMessage;
            ws.OnError += Ws_OnError;

            ws.Connect();
        }

        private void Ws_OnError(Exception ex)
        {
            OnError("websocket error", ex);
        }

        private void Ws_OnMessage(string message)
        {
            //TODO: logging
            Console.WriteLine("ws_MessageReceived e.Message= " + message);
            OnData(message);
        }

        private void Ws_OnData(byte[] data)
        {
            // only really needed for binary
            //TODO: logging
            Console.WriteLine("ws_DataReceived " + System.Text.Encoding.UTF8.GetString(data));
            OnData(data);
        }

        private void Ws_OnClosed(System.Net.WebSockets.WebSocketCloseStatus reason)
        {
            //TODO: logging
            Console.WriteLine("ws_Closed");
            ws.OnOpened -= Ws_OnOpened;
            ws.OnClosed -= Ws_OnClosed;
            ws.OnData -= Ws_OnData;
            ws.OnMessage -= Ws_OnMessage;
            ws.OnError -= Ws_OnError;
            OnClose();
        }

        private void Ws_OnOpened()
        {
            //TODO: logging
            Console.WriteLine("ws_Opened ");
            OnOpen();
        }

        internal protected override void Write(ImmutableList<Packet> packets)
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
            if (ws != null)
            {
                try
                {
                    ws.Disconnect();
                }
                catch (Exception e)
                {
                    //TODO: logging
                    Console.WriteLine("DoClose ws.Close() Exception= " + e.Message);
                }
            }
        }

        public string Uri()
        {
            Dictionary<string, string> query = null;
            query = this.Query == null ? new Dictionary<string, string>() : new Dictionary<string, string>(this.Query);
            var schema = this.Secure ? "wss" : "ws";
            var portString = "";

            if (this.TimestampRequests)
            {
                query.Add(this.TimestampParam, DateTime.Now.Ticks.ToString() + "-" + Transport.Timestamps++);
            }

            var _query = Helpers.EncodeQuerystring(query);

            if (Port > 0 && (("wss" == schema && Port != 443)
                    || ("ws" == schema && Port != 80)))
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
