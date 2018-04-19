using PureEngineIo.Interfaces;

namespace PureEngineIo.Listeners
{
    internal class EventDrainListener : IListener
    {
        private PureEngineIoSocket socket;

        public EventDrainListener(PureEngineIoSocket socket) => this.socket = socket;

        void IListener.Call(params object[] args) => socket.OnDrain();

        public int CompareTo(IListener other) => GetId().CompareTo(other.GetId());

        public int GetId() => 0;
    }
}
