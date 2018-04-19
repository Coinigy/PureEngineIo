using PureEngineIo.Interfaces;
using System;

namespace PureEngineIo.EmitterImp
{
    public class ListenerImpl : IListener
    {
        private static int id_counter = 0;
        private int Id;
        private readonly Action fn1;
        private readonly Action<object> fn;

        public ListenerImpl(Action<object> fn)
        {

            this.fn = fn;
            Id = id_counter++;
        }

        public ListenerImpl(Action fn)
        {

            fn1 = fn;
            Id = id_counter++;
        }

        public void Call(params object[] args)
        {
            if (fn != null)
            {
                var arg = args.Length > 0 ? args[0] : null;
                fn(arg);
            }
            else
            {
                fn1();
            }
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
