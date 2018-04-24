using PureEngineIo.Interfaces;

namespace PureEngineIo.Transports.PollingImp
{
    public class SendEncodeCallback : IEncodeCallback
    {
        private readonly Polling _polling;

        public SendEncodeCallback(Polling polling)
        {
            _polling = polling;
        }

        public void Call(object data)
        {
            var byteData = (byte[])data;
            _polling.DoWrite(byteData, () =>
            {
                _polling.Writable = true;
                _polling.Emit(Transport.EVENT_DRAIN);
            });
        }
    }

}
