using PureEngineIo.Interfaces;

namespace PureEngineIo.Transports.WebSocketImp
{
    public class WriteEncodeCallback : IEncodeCallback
    {
        private WebSocket webSocket;

        public WriteEncodeCallback(WebSocket webSocket) => this.webSocket = webSocket;

        public void Call(object data)
        {
            if (data is string)
            {
                webSocket.ws.Send((string)data);
            }
            else if (data is byte[])
            {
                webSocket.ws.Send((byte[])data);
            }
        }
    }
}
