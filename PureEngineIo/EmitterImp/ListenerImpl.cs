using PureEngineIo.Interfaces;
using System;

namespace PureEngineIo.EmitterImp
{
    public class ListenerImpl : IListener
    {
        private static int _idCounter;
        private readonly int _id;
        private readonly Action _fn1;
        private readonly Action<object> _fn;

        public ListenerImpl(Action<object> fn)
        {

            _fn = fn;
            _id = _idCounter++;
        }

        public ListenerImpl(Action fn)
        {
            _fn1 = fn;
            _id = _idCounter++;
        }

        public void Call(params object[] args)
        {
            if (_fn != null)
            {
                var arg = args.Length > 0 ? args[0] : null;
                _fn(arg);
            }
            else
            {
                _fn1();
            }
        }

        public int CompareTo(IListener other) => GetId().CompareTo(other.GetId());

	    public int GetId() => _id;
    }
}
