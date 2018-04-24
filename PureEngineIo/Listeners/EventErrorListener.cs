using PureEngineIo.Interfaces;
using System;

namespace PureEngineIo.Listeners
{
    internal class EventErrorListener : IListener
    {
        private readonly PureEngineIoSocket _socket;

        public EventErrorListener(PureEngineIoSocket socket) => _socket = socket;

        public void Call(params object[] args) => _socket.OnError(args.Length > 0 ? (Exception)args[0] : null);

        public int CompareTo(IListener other) => GetId().CompareTo(other.GetId());

        public int GetId() => 0;
    }
}
