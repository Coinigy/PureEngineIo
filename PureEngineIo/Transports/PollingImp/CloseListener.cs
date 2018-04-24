using PureEngineIo.Interfaces;
using PureEngineIo.Parser;
using System.Collections.Immutable;

namespace PureEngineIo.Transports.PollingImp
{
    internal class CloseListener : IListener
    {
        private readonly Polling _polling;

        public CloseListener(Polling polling) => _polling = polling;

        public void Call(params object[] args)
        {
            var packets = ImmutableList<Packet>.Empty;
            packets = packets.Add(new Packet(Packet.CLOSE));
            _polling.Write(packets);
        }

        public int CompareTo(IListener other) => GetId().CompareTo(other.GetId());

        public int GetId() => 0;
    }
}
