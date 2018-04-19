using PureEngineIo.Interfaces;

namespace PureEngineIo.EmitterImp
{
    public class OnceListener : IListener
    {
        private static int id_counter = 0;
        private int Id;
        private readonly string _eventString;
        private readonly IListener _fn;
        private readonly Emitter _emitter;

        public OnceListener(string eventString, IListener fn, Emitter emitter)
        {
            _eventString = eventString;
            _fn = fn;
            _emitter = emitter;
            Id = id_counter++;
        }

        void IListener.Call(params object[] args)
        {
            _emitter.Off(_eventString, this);
            _fn.Call(args);
        }

        public int CompareTo(IListener other)
        {
            return GetId().CompareTo(other.GetId());
        }

        public int GetId()
        {
            return Id;
        }
    }
}
