using PureEngineIo.Parser;

namespace PureEngineIo.Interfaces
{
    public interface IDecodePayloadCallback
    {
        bool Call(Packet packet, int index, int total);
    }
}
