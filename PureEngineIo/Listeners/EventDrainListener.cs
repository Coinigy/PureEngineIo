using PureEngineIo.Interfaces;

namespace PureEngineIo.Listeners
{
    internal class EventDrainListener : IListener
    {
        private readonly PureEngineIoSocket _socket;

        public EventDrainListener(PureEngineIoSocket socket) => _socket = socket;

        void IListener.Call(params object[] args) => _socket.OnDrain();

        public int CompareTo(IListener other) => GetId().CompareTo(other.GetId());

        public int GetId() => 0;
    }
}
