using PureEngineIo.Interfaces;
using PureEngineIo.Transports.PollingImp;
using System;

namespace PureEngineIo.Transports.PollingXHRImp
{
    public class PollingXHR : Polling
    {
        private XHRRequest _sendXhr;

        public PollingXHR(PureEngineIoTransportOptions options) : base(options)
        {
        }

        protected XHRRequest Request() => Request(null);

        protected XHRRequest Request(RequestOptions opts)
        {
            if (opts == null)
            {
                opts = new RequestOptions();
            }
            opts.Uri = Uri();

            var req = new XHRRequest(opts);

            req.On(EVENT_REQUEST_HEADERS, new EventRequestHeadersListener(this)).
                On(EVENT_RESPONSE_HEADERS, new EventResponseHeadersListener(this));

            return req;
        }

        private class EventRequestHeadersListener : IListener
        {
            private readonly PollingXHR pollingXHR;

			public EventRequestHeadersListener(PollingXHR pollingXHR) => this.pollingXHR = pollingXHR;

			public void Call(params object[] args)
            {
                // Never execute asynchronously for support to modify headers.
                pollingXHR.Emit(EVENT_RESPONSE_HEADERS, args[0]);
            }

			public int CompareTo(IListener other) => GetId().CompareTo(other.GetId());

			public int GetId() => 0;
		}

        private class EventResponseHeadersListener : IListener
        {
            private readonly PollingXHR _pollingXhr;

			public EventResponseHeadersListener(PollingXHR pollingXHR) => _pollingXhr = pollingXHR;

			public void Call(params object[] args) => _pollingXhr.Emit(EVENT_REQUEST_HEADERS, args[0]);

			public int CompareTo(IListener other) => GetId().CompareTo(other.GetId());

			public int GetId() => 0;
		}

        protected internal override void DoWrite(byte[] data, Action action)
        {
            var opts = new RequestOptions { Method = "POST", Data = data, CookieHeaderValue = Cookie };
			Logger.Log("DoWrite data = " + data);
            //try
            //{
            //    var dataString = BitConverter.ToString(data);
            //    log.Info(string.Format("DoWrite data {0}", dataString));
            //}
            //catch (Exception e)
            //{
            //    log.Error(e);
            //}

            _sendXhr = Request(opts);
            _sendXhr.On(EVENT_SUCCESS, new SendEventSuccessListener(action));
            _sendXhr.On(EVENT_ERROR, new SendEventErrorListener(this));
            _sendXhr.Create();
        }

        protected override void DoPoll()
        {
			Logger.Log("xhr poll");
            var opts = new RequestOptions { CookieHeaderValue = Cookie };
            _sendXhr = Request(opts);
            _sendXhr.On(EVENT_DATA, new DoPollEventDataListener(this));
            _sendXhr.On(EVENT_ERROR, new DoPollEventErrorListener(this));
            _sendXhr.Create();
        }

        private class DoPollEventDataListener : IListener
        {
            private readonly PollingXHR pollingXHR;

			public DoPollEventDataListener(PollingXHR pollingXHR) => this.pollingXHR = pollingXHR;

			public void Call(params object[] args)
            {
                var arg = args.Length > 0 ? args[0] : null;
                if (arg is string s)
                {
                    pollingXHR.OnData(s);
                }
                else if (arg is byte[] bytes)
                {
                    pollingXHR.OnData(bytes);
                }
            }

			public int CompareTo(IListener other) => GetId().CompareTo(other.GetId());

			public int GetId() => 0;
		}

        private class DoPollEventErrorListener : IListener
        {
            private readonly PollingXHR pollingXHR;

			public DoPollEventErrorListener(PollingXHR pollingXHR) => this.pollingXHR = pollingXHR;

			public void Call(params object[] args)
            {
                var err = args.Length > 0 && args[0] is Exception ? (Exception)args[0] : null;
                pollingXHR.OnError("xhr poll error", err);
            }

			public int CompareTo(IListener other) => GetId().CompareTo(other.GetId());

			public int GetId() => 0;
		}
    }
}
