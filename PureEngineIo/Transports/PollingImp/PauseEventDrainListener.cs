using PureEngineIo.Interfaces;
using System;

namespace PureEngineIo.Transports.PollingImp
{
    internal class PauseEventDrainListener : IListener
    {
        private int[] total;
        private Action pause;

        public PauseEventDrainListener(int[] total, Action pause)
        {
            this.total = total;
            this.pause = pause;
        }

        public void Call(params object[] args)
        {
            if (--total[0] == 0)
            {
                pause();
            }
        }

        public int CompareTo(IListener other) => GetId().CompareTo(other.GetId());

        public int GetId() => 0;
    }
}
