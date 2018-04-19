using PureEngineIo.Exceptions;
using PureEngineIo.Interfaces;
using System;
using System.Collections.Immutable;

namespace PureEngineIo.Listeners
{
    internal class ProbingOnErrorListener : IListener
    {
        private readonly PureEngineIoSocket _socket;
        internal readonly ImmutableList<Transport> _transport;
        internal readonly IListener _freezeTransport;

        public ProbingOnErrorListener(PureEngineIoSocket socket, ImmutableList<Transport> transport, IListener freezeTransport)
        {
            _socket = socket;
            _transport = transport;
            _freezeTransport = freezeTransport;
        }

        void IListener.Call(params object[] args)
        {
            var err = args[0];
            PureEngineIOException error;
            if (err is Exception)
            {
                error = new PureEngineIOException("probe error", (Exception)err);
            }
            else if (err is string)
            {
                error = new PureEngineIOException("probe error: " + (string)err);
            }
            else
            {
                error = new PureEngineIOException("probe error");
            }
            error.Transport = _transport[0].Name;

            _freezeTransport.Call();

            //TODO: logging
            Console.WriteLine(string.Format("probe transport \"{0}\" failed because of error: {1}", error.Transport, err));
            _socket.Emit(PureEngineIoSocket.EVENT_UPGRADE_ERROR, error);
        }

        public int CompareTo(IListener other) => GetId().CompareTo(other.GetId());

        public int GetId() => 0;
    }
}
