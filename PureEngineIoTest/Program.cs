using PureEngineIo;
using System;

namespace PureEngineIoTest
{
    class Program
    {
        static void Main(string[] args)
        {
            var socket = new PureEngineIoSocket("ws://localhost");
            socket.On(PureEngineIoSocket.EVENT_OPEN, () =>
            {
                socket.On(PureEngineIoSocket.EVENT_MESSAGE, (data) =>
                {
                    Console.WriteLine((string)data);
                });
            });
            socket.Open();
        }
    }
}
