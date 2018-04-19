using System;
using System.Collections.Immutable;

namespace PureEngineIo
{
    internal class ProbeParameters
    {
        public ImmutableList<Transport> Transport { get; set; }
        public ImmutableList<bool> Failed { get; set; }
        public ImmutableList<Action> Cleanup { get; set; }
        public PureEngineIoSocket Socket { get; set; }
    }
}
