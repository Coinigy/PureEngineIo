using PureEngineIo.EmitterImp;
using PureEngineIo.Exceptions;
using PureEngineIo.Parser;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;

namespace PureEngineIo
{
    public abstract class Transport : Emitter
    {
        public static readonly string EVENT_OPEN = "open";
        public static readonly string EVENT_CLOSE = "close";
        public static readonly string EVENT_PACKET = "packet";
        public static readonly string EVENT_DRAIN = "drain";
        public static readonly string EVENT_ERROR = "error";
        public static readonly string EVENT_SUCCESS = "success";
        public static readonly string EVENT_DATA = "data";
        public static readonly string EVENT_REQUEST_HEADERS = "requestHeaders";
        public static readonly string EVENT_RESPONSE_HEADERS = "responseHeaders";

        protected static int Timestamps = 0;

        private bool _writeable;
        public bool Writable
        {
            get { return _writeable; }
            set
            {
                //TODO: hook up to logger
                Console.WriteLine(string.Format("Writable: {0} sid={1}", value, Socket.Id));
                _writeable = value;
            }
        }

        public int MyProperty { get; set; }

        public string Name;
        public Dictionary<string, string> Query;

        protected bool Secure;
        protected bool TimestampRequests;
        protected int Port;
        protected string Path;
        protected string Hostname;
        protected string TimestampParam;
        protected PureEngineIoSocket Socket;
        protected bool Agent = false;
        protected bool ForceBase64 = false;
        protected bool ForceJsonp = false;
        protected string Cookie;

        protected Dictionary<string, string> ExtraHeaders;

        internal protected ReadyStateEnum ReadyState = ReadyStateEnum.CLOSED;

        protected Transport(PureEngineIoTransportOptions options)
        {
            Path = options.Path;
            Hostname = options.Hostname;
            Port = options.Port;
            Secure = options.Secure;
            Query = options.Query;
            TimestampParam = options.TimestampParam;
            TimestampRequests = options.TimestampRequests;
            Socket = options.Socket;
            Agent = options.Agent;
            ForceBase64 = options.ForceBase64;
            ForceJsonp = options.ForceJsonp;
            Cookie = options.GetCookiesAsString();
            ExtraHeaders = options.ExtraHeaders;
        }

        internal protected Transport OnError(string message, Exception exception)
        {
            Exception err = new PureEngineIOException(message, exception);
            Emit(EVENT_ERROR, err);
            return this;
        }

        internal protected void OnOpen()
        {
            ReadyState = ReadyStateEnum.OPEN;
            Writable = true;
            Emit(EVENT_OPEN);
        }

        internal protected void OnClose()
        {
            ReadyState = ReadyStateEnum.CLOSED;
            Emit(EVENT_CLOSE);
        }

        protected virtual void OnData(string data) => OnPacket(Parser.Parser.DecodePacket(data));

        protected virtual void OnData(byte[] data) => OnPacket(Parser.Parser.DecodePacket(data));

        internal protected void OnPacket(Packet packet) => Emit(EVENT_PACKET, packet);

        public Transport Open()
        {
            if (ReadyState == ReadyStateEnum.CLOSED)
            {
                ReadyState = ReadyStateEnum.OPENING;
                DoOpen();
            }
            return this;
        }

        public Transport Close()
        {
            if (ReadyState == ReadyStateEnum.OPENING || ReadyState == ReadyStateEnum.OPEN)
            {
                DoClose();
                OnClose();
            }
            return this;
        }

        public Transport Send(ImmutableList<Packet> packets)
        {
            //TODO: hook up to logger
            Console.WriteLine("Send called with packets.Count: " + packets.Count);
            var count = packets.Count;
            if (ReadyState == ReadyStateEnum.OPEN)
            {
                //PollTasks.Exec((n) =>
                //{
                Write(packets);
                //});
            }
            else
            {
                throw new PureEngineIOException("Transport not open");
                //log.Info("Transport not open");
            }
            return this;
        }

        protected abstract void DoOpen();

        protected abstract void DoClose();

        internal protected abstract void Write(ImmutableList<Packet> packets);
    }
}
