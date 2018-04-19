using PureEngineIo.Interfaces;
using PureEngineIo.Parser;
using System.Collections.Immutable;

namespace PureEngineIo.Listeners
{
    internal class OnTransportOpenListener : IListener
    {
        internal ProbeParameters Parameters;

        public OnTransportOpenListener(ProbeParameters parameters) => Parameters = parameters;

        void IListener.Call(params object[] args)
        {
            if (Parameters.Failed[0])
            {
                return;
            }

            var packet = new Packet(Packet.PING, "probe");
            Parameters.Transport[0].Once(Transport.EVENT_PACKET, new ProbeEventPacketListener(this));
            Parameters.Transport[0].Send(ImmutableList<Packet>.Empty.Add(packet));
        }        

        public int CompareTo(IListener other) => GetId().CompareTo(other.GetId());

        public int GetId() => 0;
    }
}
