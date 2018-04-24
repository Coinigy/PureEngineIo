using PureEngineIo.Interfaces;
using PureEngineIo.Parser;

namespace PureEngineIo.Transports.PollingImp
{
    internal class DecodePayloadCallback : IDecodePayloadCallback
    {
        private readonly Polling _polling;

        public DecodePayloadCallback(Polling polling)
        {
            _polling = polling;
        }
        public bool Call(Packet packet, int index, int total)
        {
            if (_polling.ReadyState == ReadyStateEnum.OPENING)
            {
                _polling.OnOpen();
            }

            if (packet.Type == Packet.CLOSE)
            {
                _polling.OnClose();
                return false;
            }

            _polling.OnPacket(packet);
            return true;
        }
    }
}
