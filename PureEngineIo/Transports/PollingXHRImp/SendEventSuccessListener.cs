using PureEngineIo.Interfaces;
using System;

namespace PureEngineIo.Transports.PollingXHRImp
{
    internal class SendEventSuccessListener : IListener
    {
        private readonly Action _action;

        public SendEventSuccessListener(Action action) => _action = action;

        public void Call(params object[] args) => _action?.Invoke();

        public int CompareTo(IListener other) => GetId().CompareTo(other.GetId());

        public int GetId() => 0;
    }
}
