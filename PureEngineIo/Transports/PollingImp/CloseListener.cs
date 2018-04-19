using PureEngineIo.Interfaces;
using PureEngineIo.Parser;
using System.Collections.Immutable;

namespace PureEngineIo.Transports.PollingImp
{
    internal class CloseListener : IListener
    {
        private Polling polling;

        public CloseListener(Polling polling) => this.polling = polling;

        public void Call(params object[] args)
        {
            ImmutableList<Packet> packets = ImmutableList<Packet>.Empty;
            packets = packets.Add(new Packet(Packet.CLOSE));
            polling.Write(packets);
        }

        public int CompareTo(IListener other) => GetId().CompareTo(other.GetId());

        public int GetId() => 0;
    }
}
