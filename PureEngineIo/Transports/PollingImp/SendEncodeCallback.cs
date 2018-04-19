using PureEngineIo.Interfaces;

namespace PureEngineIo.Transports.PollingImp
{
    public class SendEncodeCallback : IEncodeCallback
    {
        private Polling polling;

        public SendEncodeCallback(Polling polling)
        {
            this.polling = polling;
        }

        public void Call(object data)
        {
            var byteData = (byte[])data;
            polling.DoWrite(byteData, () =>
            {
                polling.Writable = true;
                polling.Emit(Transport.EVENT_DRAIN);
            });
        }
    }

}
