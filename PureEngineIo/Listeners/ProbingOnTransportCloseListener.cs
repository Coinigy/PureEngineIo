using PureEngineIo.Interfaces;

namespace PureEngineIo.Listeners
{
    internal class ProbingOnTransportCloseListener : IListener
    {
        internal readonly IListener _onError;

        public ProbingOnTransportCloseListener(ProbingOnErrorListener onError) => _onError = onError;

        void IListener.Call(params object[] args) => _onError.Call("transport closed");

        public int CompareTo(IListener other) => GetId().CompareTo(other.GetId());

        public int GetId() => 0;
    }
}
