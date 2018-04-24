using PureEngineIo.Interfaces;

namespace PureEngineIo.Listeners
{
    internal class OnHeartbeatAsListener : IListener
    {
        private readonly PureEngineIoSocket _socket;

        public OnHeartbeatAsListener(PureEngineIoSocket socket) => _socket = socket;

        void IListener.Call(params object[] args) => _socket.OnHeartbeat(args.Length > 0 ? (long)args[0] : 0);

        public int CompareTo(IListener other) => GetId().CompareTo(other.GetId());

        public int GetId() => 0;
    }
}
