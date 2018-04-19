using PureEngineIo.Interfaces;

namespace PureEngineIo.Listeners
{
    internal class ProbingOnCloseListener : IListener
    {
        internal IListener _onError;

        public ProbingOnCloseListener(ProbingOnErrorListener onError) => _onError = onError;

        void IListener.Call(params object[] args) => _onError.Call("socket closed");

        public int CompareTo(IListener other) => GetId().CompareTo(other.GetId());

        public int GetId() => 0;
    }
}
