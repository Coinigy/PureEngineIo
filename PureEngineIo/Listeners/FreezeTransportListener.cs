using PureEngineIo.Interfaces;
using System.Collections.Immutable;

namespace PureEngineIo.Listeners
{
    internal class FreezeTransportListener : IListener
    {
        internal ProbeParameters Parameters;

        public FreezeTransportListener(ProbeParameters parameters) => Parameters = parameters;

        void IListener.Call(params object[] args)
        {
            if (Parameters.Failed[0])
            {
                return;
            }

            Parameters.Failed = Parameters.Failed.SetItem(0, true);

            Parameters.Cleanup[0]();

            if (Parameters.Transport.Count < 1)
            {
                return;
            }

            Parameters.Transport[0].Close();
            Parameters.Transport = ImmutableList<Transport>.Empty;
        }

        public int CompareTo(IListener other) => GetId().CompareTo(other.GetId());

        public int GetId() => 0;
    }
}
