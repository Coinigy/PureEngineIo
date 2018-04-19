using PureEngineIo.Interfaces;

namespace PureEngineIo.Listeners
{
    internal class EventCloseListener : IListener
    {
        private PureEngineIoSocket socket;

        public EventCloseListener(PureEngineIoSocket socket) => this.socket = socket;

        public void Call(params object[] args) => socket.OnClose("transport close");

        public int CompareTo(IListener other) => GetId().CompareTo(other.GetId());

        public int GetId() => 0;
    }
}
