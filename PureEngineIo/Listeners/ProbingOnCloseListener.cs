using PureEngineIo.Interfaces;

namespace PureEngineIo.Listeners
{
    internal class ProbingOnCloseListener : IListener
    {
	    private readonly IListener _onError;

        public ProbingOnCloseListener(IListener onError) => _onError = onError;

        void IListener.Call(params object[] args) => _onError.Call("socket closed");

        public int CompareTo(IListener other) => GetId().CompareTo(other.GetId());

        public int GetId() => 0;
    }
}
