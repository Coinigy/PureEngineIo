using System;

namespace PureEngineIo.Exceptions
{
    class PureEngineIOException : Exception
    {
        public string Transport;
        public object Code;

        public PureEngineIOException(string message) : base(message) { }

        public PureEngineIOException(Exception cause) : base("", cause) { }

        public PureEngineIOException(string message, Exception cause) : base(message, cause) { }
    }
}
