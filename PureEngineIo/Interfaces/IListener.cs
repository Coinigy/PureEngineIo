using System;

namespace PureEngineIo.Interfaces
{
    public interface IListener : IComparable<IListener>
    {
        int GetId();
        void Call(params object[] args);
    }
}
