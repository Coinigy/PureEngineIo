using PureEngineIo.Interfaces;
using System;

namespace PureEngineIo.Transports.PollingXHRImp
{
    internal class SendEventErrorListener : IListener
    {
        private readonly PollingXHR _pollingXhr;

        public SendEventErrorListener(PollingXHR pollingXHR) => _pollingXhr = pollingXHR;

        public void Call(params object[] args)
        {
            var err = args.Length > 0 && args[0] is Exception ? (Exception)args[0] : null;
            _pollingXhr.OnError("xhr post error", err);
        }

        public int CompareTo(IListener other) => GetId().CompareTo(other.GetId());

        public int GetId() => 0;
    }

}
