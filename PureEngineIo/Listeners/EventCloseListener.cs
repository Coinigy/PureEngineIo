using PureEngineIo.Interfaces;

namespace PureEngineIo.Listeners
{
    internal class EventCloseListener : IListener
    {
        private readonly PureEngineIoSocket _socket;

        public EventCloseListener(PureEngineIoSocket socket) => _socket = socket;

        public void Call(params object[] args) => _socket.OnClose("transport close");

        public int CompareTo(IListener other) => GetId().CompareTo(other.GetId());

        public int GetId() => 0;
    }
}
