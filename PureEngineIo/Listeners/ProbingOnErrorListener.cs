using PureEngineIo.Exceptions;
using PureEngineIo.Interfaces;
using System;
using System.Collections.Immutable;

namespace PureEngineIo.Listeners
{
    internal class ProbingOnErrorListener : IListener
    {
        private readonly PureEngineIoSocket _socket;
        internal readonly ImmutableList<Transport> Transport;
        internal readonly IListener FreezeTransport;

        public ProbingOnErrorListener(PureEngineIoSocket socket, ImmutableList<Transport> transport, IListener freezeTransport)
        {
            _socket = socket;
            Transport = transport;
            FreezeTransport = freezeTransport;
        }

        void IListener.Call(params object[] args)
        {
            var err = args[0];
            PureEngineIOException error;
            if (err is Exception exception)
            {
                error = new PureEngineIOException("probe error", exception);
            }
            else if (err is string s)
            {
                error = new PureEngineIOException("probe error: " + s);
            }
            else
            {
                error = new PureEngineIOException("probe error");
            }
            error.Transport = Transport[0].Name;

            FreezeTransport.Call();

			Logger.Log(new Exception($"probe transport \"{error.Transport}\" failed because of error: {err}"));
            _socket.Emit(PureEngineIoSocket.EVENT_UPGRADE_ERROR, error);
        }

        public int CompareTo(IListener other) => GetId().CompareTo(other.GetId());

        public int GetId() => 0;
    }
}
