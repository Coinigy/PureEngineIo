using PureEngineIo.Interfaces;
using PureEngineIo.Parser;

namespace PureEngineIo.Transports.PollingImp
{
    internal class DecodePayloadCallback : IDecodePayloadCallback
    {
        private Polling polling;

        public DecodePayloadCallback(Polling polling)
        {
            this.polling = polling;
        }
        public bool Call(Packet packet, int index, int total)
        {
            if (polling.ReadyState == ReadyStateEnum.OPENING)
            {
                polling.OnOpen();
            }

            if (packet.Type == Packet.CLOSE)
            {
                polling.OnClose();
                return false;
            }

            polling.OnPacket(packet);
            return true;
        }
    }
}
