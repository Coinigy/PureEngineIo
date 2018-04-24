using PureEngineIo;
using PureEngineIo.Parser;
using PureEngineIo.Transports.WebSocketImp;
using System;
using System.Threading;

namespace PureEngineIoTest
{
    class Program
    {

        static void AttachHandlers(PureEngineIoSocket socket)
        {
            socket.On(PureEngineIoSocket.EVENT_OPEN, () =>
            {
                socket.On(PureEngineIoSocket.EVENT_MESSAGE, (data) =>
                {
                    var receivedMessage = (string)data;

                    Console.WriteLine($"Event message: {receivedMessage}");
                });
                socket.On(PureEngineIoSocket.EVENT_DATA, (data) =>
                {
                    var receivedMessage = (string)data;

                    Console.WriteLine($"Event data: {receivedMessage}");
                });
                socket.On(PureEngineIoSocket.EVENT_HANDSHAKE, (data) =>
                {

                    Console.WriteLine("Event handshake");
                });
                socket.On(PureEngineIoSocket.EVENT_HEARTBEAT, (data) =>
                {
                    var receivedMessage = (string)data;

                    Console.WriteLine($"Event heartbeat: {receivedMessage}");
                });
                socket.On(PureEngineIoSocket.EVENT_PACKET, (data) =>
                {
                    var receivedMessage = (Packet)data;

                    Console.WriteLine($"Event packet: {receivedMessage.Data ?? string.Empty}");
                });
                socket.On(PureEngineIoSocket.EVENT_PACKET_CREATE, (data) =>
                {
                    var packet = (Packet)data;

                    Console.WriteLine($"Event packet create: {packet.Data ?? string.Empty} type: {packet.Type}");
                });
                socket.On(PureEngineIoSocket.EVENT_TRANSPORT, (data) =>
                {

                    Console.WriteLine($"Event transport");
                });
                socket.On(PureEngineIoSocket.EVENT_UPGRADE, (data) =>
                {
                    Console.WriteLine($"Event upgrade");
                    for (var i = 0; i < 10; i++)
                    {
                        socket.Write($"Message # {i}");
                        Thread.Sleep(500);
                    }
                    socket.Close();
                });
                socket.On(PureEngineIoSocket.EVENT_UPGRADE_ERROR, (data) =>
                {
                    var receivedMessage = (string)data;

                    Console.WriteLine($"Event upgrade error: {receivedMessage}");
                });
                socket.On(PureEngineIoSocket.EVENT_UPGRADING, (data) =>
                {
                    var sock = (WebSocket)data;
                    Console.WriteLine($"Event upgrading");
                });
                socket.On(PureEngineIoSocket.EVENT_CLOSE, (data) =>
                {
                    Console.WriteLine($"Event close");
                    DetachHandlers(socket);
                    TestSocket();

                });
                
                for (var i = 0; i < 10; i++)
                {
                    socket.Send($"Message # {i}");
                    Thread.Sleep(500);
                }
                
            });
        }

        static void DetachHandlers(PureEngineIoSocket socket)
        {
            socket.Off();
        }

        static void TestSocket()
        {
            var socket = new PureEngineIoSocket("localhost:3000", new PureEngineIoOptions(){DebugMode = true});
            AttachHandlers(socket);

            socket.Open();
        }

        static void Main(string[] args)
        {
            TestSocket();
           
            Console.ReadLine();
        }
    }
}
