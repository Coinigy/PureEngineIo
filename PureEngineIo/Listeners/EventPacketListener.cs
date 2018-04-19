using PureEngineIo.Interfaces;
using PureEngineIo.Parser;

namespace PureEngineIo.Listeners
{
    internal class EventPacketListener : IListener
    {
        private PureEngineIoSocket socket;

        public EventPacketListener(PureEngineIoSocket socket) => this.socket = socket;

        void IListener.Call(params object[] args) => socket.OnPacket(args.Length > 0 ? (Packet)args[0] : null);

        public int CompareTo(IListener other) => GetId().CompareTo(other.GetId());

        public int GetId() => 0;
    }
}
