using PureEngineIo.Interfaces;
using PureEngineIo.Parser;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;

namespace PureEngineIo.Transports.PollingImp
{
    public class Polling : Transport
    {
        public static readonly string NAME = "polling";
        public static readonly string EVENT_POLL = "poll";
        public static readonly string EVENT_POLL_COMPLETE = "pollComplete";

        private bool IsPolling = false;

        public Polling(PureEngineIoTransportOptions opts) : base(opts)
        {
            Name = NAME;
        }

        protected override void DoOpen()
        {
            Poll();
        }

        public void Pause(Action onPause)
        {
            ReadyState = ReadyStateEnum.PAUSED;
            Action pause = () =>
            {
                //log.Info("paused");
                ReadyState = ReadyStateEnum.PAUSED;
                onPause();
            };

            if (IsPolling || !Writable)
            {
                var total = new[] { 0 };

                if (IsPolling)
                {
                    //log.Info("we are currently polling - waiting to pause");
                    total[0]++;
                    Once(EVENT_POLL_COMPLETE, new PauseEventPollCompleteListener(total, pause));

                }

                if (!Writable)
                {
                    //log.Info("we are currently writing - waiting to pause");
                    total[0]++;
                    Once(EVENT_DRAIN, new PauseEventDrainListener(total, pause));
                }
            }
            else
            {
                pause();
            }
        }

        public void Resume()
        {
            if (ReadyState == ReadyStateEnum.PAUSED)
                ReadyState = ReadyStateEnum.OPEN;
        }

        private void Poll()
        {
            IsPolling = true;
            DoPoll();
            Emit(EVENT_POLL);
        }

        protected override void OnData(string data)
        {
            _onData(data);
        }

        protected override void OnData(byte[] data)
        {
            _onData(data);
        }

        private void _onData(object data)
        {
            //TODO: logging
            Console.WriteLine(string.Format("polling got data {0}", data));
            var callback = new DecodePayloadCallback(this);
            if (data is string)
            {
                Parser.Parser.DecodePayload((string)data, callback);
            }
            else if (data is byte[])
            {
                Parser.Parser.DecodePayload((byte[])data, callback);
            }

            if (ReadyState != ReadyStateEnum.CLOSED)
            {
                IsPolling = false;
                //TODO: logging
                Console.WriteLine("ReadyState != ReadyStateEnum.CLOSED");
                Emit(EVENT_POLL_COMPLETE);

                if (ReadyState == ReadyStateEnum.OPEN)
                {
                    Poll();
                }
                else
                {
                    //TODO: logging
                    Console.WriteLine(string.Format("ignoring poll - transport state {0}", ReadyState));
                }
            }
        }

        protected override void DoClose()
        {
            var closeListener = new CloseListener(this);

            if (ReadyState == ReadyStateEnum.OPEN)
            {
                //TODO: logging
                Console.WriteLine("transport open - closing");
                closeListener.Call();
            }
            else
            {
                // in case we're trying to close while
                // handshaking is in progress (engine.io-client GH-164)
                //TODO: logging
                Console.WriteLine("transport not open - deferring close");
                Once(EVENT_OPEN, closeListener);
            }
        }

        internal protected override void Write(ImmutableList<Packet> packets)
        {
            // TODO: logging
            Console.WriteLine("Write packets.Count = " + packets.Count);

            Writable = false;

            var callback = new SendEncodeCallback(this);
            Parser.Parser.EncodePayload(packets.ToArray(), callback);
        }

        public string Uri()
        {
            var query = new Dictionary<string, string>(Query);
            string schema = Secure ? "https" : "http";
            string portString = "";

            if (TimestampRequests)
            {
                query.Add(TimestampParam, DateTime.Now.Ticks + "-" + Transport.Timestamps++);
            }

            query.Add("b64", "1");

            string _query = Helpers.EncodeQuerystring(query);

            if (Port > 0 && (("https" == schema && Port != 443)  || ("http" == schema && Port != 80)))
            {
                portString = ":" + Port;
            }

            if (_query.Length > 0)
            {
                _query = "?" + _query;
            }

            return schema + "://" + Hostname + portString + Path + _query;
        }

        internal protected virtual void DoWrite(byte[] data, Action action)
        {
        }

        protected virtual void DoPoll()
        {
        }
    }
}
