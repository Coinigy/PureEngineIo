using PureEngineIo.Interfaces;
using System;

namespace PureEngineIo.Transports.PollingXHRImp
{
    internal class SendEventSuccessListener : IListener
    {
        private Action action;

        public SendEventSuccessListener(Action action) => this.action = action;

        public void Call(params object[] args) => action?.Invoke();

        public int CompareTo(IListener other) => GetId().CompareTo(other.GetId());

        public int GetId() => 0;
    }
}
