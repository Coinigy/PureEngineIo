using System;
using System.Collections.Generic;
using System.Net;
using System.Text;
using PureWebSockets;

namespace PureEngineIo
{
    public class PureEngineIoTransportOptions : IPureWebSocketOptions
    {
        public bool Agent;
        public bool ForceBase64;
        public bool ForceJsonp;
        public string Hostname;
        public string Path;
        public string TimestampParam;
        public bool Secure;
        public bool TimestampRequests = true;
        public int Port;
        public int PolicyPort;
        public Dictionary<string, string> Query;
        public bool IgnoreServerCertificateValidation;
        internal PureEngineIoSocket Socket;
        public Dictionary<string, string> Cookies = new Dictionary<string, string>();
        public Dictionary<string, string> ExtraHeaders = new Dictionary<string, string>();

        public IEnumerable<Tuple<string, string>> Headers { get; set; }
        public IWebProxy Proxy { get; set; }
        public int SendQueueLimit { get; set; }
        public TimeSpan SendCacheItemTimeout { get; set; }
        public ushort SendDelay { get; set; }
        public ReconnectStrategy MyReconnectStrategy { get; set; }
        public bool DebugMode { get; set; }
        public int DisconnectWait { get; set; }

        public string GetCookiesAsString()
        {
            var result = new StringBuilder();
            foreach (var item in Cookies)
            {
                result.Append(item.Key);
                result.Append('=');
                result.Append(item.Value);
                result.Append(';');
            }
            return result.ToString();
        }
    }
}
