using PureEngineIo.Interfaces;
using System;
using System.Collections.Immutable;

namespace PureEngineIo.Listeners
{
    internal class ProbingOnUpgradeListener : IListener
    {
        internal readonly IListener _freezeTransport;
        internal readonly ImmutableList<Transport> _transport;

        public ProbingOnUpgradeListener(FreezeTransportListener freezeTransport, ImmutableList<Transport> transport)
        {
            _freezeTransport = freezeTransport;
            _transport = transport;
        }

        void IListener.Call(params object[] args)
        {
            var to = (Transport)args[0];
            if (_transport[0] != null && to.Name != _transport[0].Name)
            {
                //TODO: logging
                Console.WriteLine(string.Format("'{0}' works - aborting '{1}'", to.Name, _transport[0].Name));
                _freezeTransport.Call();
            }
        }

        public int CompareTo(IListener other) => GetId().CompareTo(other.GetId());

        public int GetId() => 0;
    }
}
