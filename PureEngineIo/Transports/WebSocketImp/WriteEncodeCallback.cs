using PureEngineIo.Interfaces;

namespace PureEngineIo.Transports.WebSocketImp
{
    public class WriteEncodeCallback : IEncodeCallback
    {
        private readonly WebSocket _webSocket;

        public WriteEncodeCallback(WebSocket webSocket) => _webSocket = webSocket;

        public void Call(object data)
        {
            if (data is string s)
            {
                _webSocket.Ws.Send(s);
            }
            else if (data is byte[] bytes)
            {
                _webSocket.Ws.Send(bytes);
            }
        }
    }
}
