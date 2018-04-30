using System;
using System.Collections.Immutable;
using PureEngineIo.Interfaces;

namespace PureEngineIo
{
    public class PureEngineIoOptions : PureEngineIoTransportOptions
    {
        public ImmutableList<string> Transports;
        public bool Upgrade = true;
        public bool RememberUpgrade;
        public string Host;
        public string QueryString;
	    public ISerializer Serializer { get; set; }

		public static PureEngineIoOptions FromURI(Uri uri, PureEngineIoOptions opts)
        {
            if (opts == null)
            {
                opts = new PureEngineIoOptions();
            }

            opts.Host = uri.Host;
            opts.Secure = uri.Scheme == "https" || uri.Scheme == "wss";
            opts.Port = uri.Port;

			opts.Serializer = new Utf8JsonSerializer(); 

            if (!string.IsNullOrEmpty(uri.Query))
            {
                opts.QueryString = uri.Query;
            }

            return opts;
        }
    }
}
