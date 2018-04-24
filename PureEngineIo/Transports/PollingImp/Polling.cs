using PureEngineIo.Parser;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace PureEngineIo.Transports.PollingImp
{
    public class Polling : Transport
    {
        public const string NAME = "polling";
        public const string EVENT_POLL = "poll";
        public const string EVENT_POLL_COMPLETE = "pollComplete";

        private bool _isPolling;

		public Polling(PureEngineIoTransportOptions opts) : base(opts) => Name = NAME;

		protected override void DoOpen() => Poll();

		public void Pause(Action onPause)
        {
            ReadyState = ReadyStateEnum.PAUSED;

	        void Action()
	        {
		        Logger.Log("paused");
		        ReadyState = ReadyStateEnum.PAUSED;
		        onPause();
	        }

	        if (_isPolling || !Writable)
            {
                var total = new[] { 0 };

                if (_isPolling)
                {
                    Logger.Log("we are currently polling - waiting to pause");
                    total[0]++;
                    Once(EVENT_POLL_COMPLETE, new PauseEventPollCompleteListener(total, Action));

                }

                if (!Writable)
                {
                    Logger.Log("we are currently writing - waiting to pause");
                    total[0]++;
                    Once(EVENT_DRAIN, new PauseEventDrainListener(total, Action));
                }
            }
            else
            {
                Action();
            }
        }

        public void Resume()
        {
            if (ReadyState == ReadyStateEnum.PAUSED)
                ReadyState = ReadyStateEnum.OPEN;
        }

        private void Poll()
        {
            _isPolling = true;
            DoPoll();
            Emit(EVENT_POLL);
        }

		protected override void OnData(string data) => _onData(data);

		protected override void OnData(byte[] data) => _onData(data);

		private void _onData(object data)
        {
			Logger.Log($"polling got data {data}");
            var callback = new DecodePayloadCallback(this);
            if (data is string s)
            {
                Parser.Parser.DecodePayload(s, callback);
            }
            else if (data is byte[] bytes)
            {
                Parser.Parser.DecodePayload(bytes, callback);
            }

            if (ReadyState != ReadyStateEnum.CLOSED)
            {
                _isPolling = false;
				Logger.Log("ReadyState != ReadyStateEnum.CLOSED");
                Emit(EVENT_POLL_COMPLETE);

                if (ReadyState == ReadyStateEnum.OPEN)
                {
                    Poll();
                }
                else
                {
					Logger.Log($"ignoring poll - transport state {ReadyState}");
                }
            }
        }

        protected override void DoClose()
        {
            var closeListener = new CloseListener(this);

            if (ReadyState == ReadyStateEnum.OPEN)
            {
				Logger.Log("transport open - closing");
                closeListener.Call();
            }
            else
            {
				// in case we're trying to close while
				// handshaking is in progress (engine.io-client GH-164)
				Logger.Log("transport not open - deferring close");
                Once(EVENT_OPEN, closeListener);
            }
        }

        protected internal override void Write(ImmutableList<Packet> packets)
        {
			Logger.Log("Write packets.Count = " + packets.Count);

            Writable = false;

            var callback = new SendEncodeCallback(this);
            Parser.Parser.EncodePayload(packets.ToArray(), callback);
        }

        public string Uri()
        {
            var query = new Dictionary<string, string>(Query);
            var schema = Secure ? "https" : "http";
            var portString = "";

            if (TimestampRequests)
            {
                query.Add(TimestampParam, DateTime.Now.Ticks + "-" + Timestamps++);
            }

            query.Add("b64", "1");

            var encodedQuery = Helpers.EncodeQuerystring(query);

            if (Port > 0 && (("https" == schema && Port != 443)  || ("http" == schema && Port != 80)))
            {
                portString = ":" + Port;
            }

            if (encodedQuery.Length > 0)
            {
                encodedQuery = "?" + encodedQuery;
            }

            return schema + "://" + Hostname + portString + Path + encodedQuery;
        }

        protected internal virtual void DoWrite(byte[] data, Action action)
        {
        }

        protected virtual void DoPoll()
        {
        }
    }
}
