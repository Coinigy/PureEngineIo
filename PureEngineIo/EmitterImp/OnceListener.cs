using PureEngineIo.Interfaces;

namespace PureEngineIo.EmitterImp
{
    public class OnceListener : IListener
    {
        private static int _idCounter;
        private readonly int _id;
        private readonly string _eventString;
        private readonly IListener _fn;
        private readonly Emitter _emitter;

        public OnceListener(string eventString, IListener fn, Emitter emitter)
        {
            _eventString = eventString;
            _fn = fn;
            _emitter = emitter;
            _id = _idCounter++;
        }

        void IListener.Call(params object[] args)
        {
            _emitter.Off(_eventString, this);
            _fn.Call(args);
        }

        public int CompareTo(IListener other) => GetId().CompareTo(other.GetId());

	    public int GetId() => _id;
    }
}
