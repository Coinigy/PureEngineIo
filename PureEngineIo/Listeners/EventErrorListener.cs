using PureEngineIo.Interfaces;
using System;

namespace PureEngineIo.Listeners
{
    internal class EventErrorListener : IListener
    {
        private PureEngineIoSocket socket;

        public EventErrorListener(PureEngineIoSocket socket) => this.socket = socket;

        public void Call(params object[] args) => socket.OnError(args.Length > 0 ? (Exception)args[0] : null);

        public int CompareTo(IListener other) => GetId().CompareTo(other.GetId());

        public int GetId() => 0;
    }
}
