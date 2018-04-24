using PureEngineIo.Interfaces;
using System;

namespace PureEngineIo.Transports.PollingImp
{
    internal class PauseEventPollCompleteListener : IListener
    {
        private readonly int[] _total;
        private readonly Action _pause;

        public PauseEventPollCompleteListener(int[] total, Action pause)
        {
            _total = total;
            _pause = pause;
        }

        public void Call(params object[] args)
        {
            if (--_total[0] == 0)
            {
                _pause();
            }
        }

        public int CompareTo(IListener other) => GetId().CompareTo(other.GetId());

        public int GetId() => 0;
    }
}
