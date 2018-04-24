using PureEngineIo.Interfaces;
using System.Collections.Immutable;

namespace PureEngineIo.Listeners
{
    internal class ProbingOnUpgradeListener : IListener
    {
        internal readonly IListener FreezeTransport;
        internal readonly ImmutableList<Transport> Transport;

        public ProbingOnUpgradeListener(IListener freezeTransport, ImmutableList<Transport> transport)
        {
            FreezeTransport = freezeTransport;
            Transport = transport;
        }

        void IListener.Call(params object[] args)
        {
            var to = (Transport)args[0];
            if (Transport[0] != null && to.Name != Transport[0].Name)
            {
				Logger.Log($"'{to.Name}' works - aborting '{Transport[0].Name}'");
                FreezeTransport.Call();
            }
        }

        public int CompareTo(IListener other) => GetId().CompareTo(other.GetId());

        public int GetId() => 0;
    }
}
