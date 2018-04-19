using System.Collections.Generic;

namespace PureEngineIo.Transports.PollingXHRImp
{
    public class RequestOptions
    {
        public string Uri;
        public string Method;
        public byte[] Data;
        public string CookieHeaderValue;
        public Dictionary<string, string> ExtraHeaders;
    }
}
