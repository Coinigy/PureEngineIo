using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Text.RegularExpressions;

namespace PureEngineIo
{
    class Helpers
    {
        public static string EncodeURIComponent(string str) => Uri.EscapeDataString(str);

        public static string DecodeURIComponent(string str) => Uri.UnescapeDataString(str);

        public static string CallerName([CallerMemberName]string caller = "", [CallerLineNumber]int number = 0, [CallerFilePath]string path = "") => string.Format("{0}-{1}:{2}#{3}", path, path.Split('\\').LastOrDefault(), caller, number);

        public static string StripInvalidUnicodeCharacters(string str) => Regex.Replace(str, "([\ud800-\udbff](?![\udc00-\udfff]))|((?<![\ud800-\udbff])[\udc00-\udfff])", "");

        public static string EncodeQuerystring(ImmutableDictionary<string, string> obj)
        {
            var sb = new StringBuilder();
            foreach (var key in obj.Keys.OrderBy(x => x))
            {
                if (sb.Length > 0)
                {
                    sb.Append("&");
                }
                sb.Append(EncodeURIComponent(key));
                sb.Append("=");
                sb.Append(EncodeURIComponent(obj[key]));
            }
            return sb.ToString();
        }

        internal static string EncodeQuerystring(Dictionary<string, string> obj)
        {
            var sb = new StringBuilder();
            foreach (var key in obj.Keys)
            {
                if (sb.Length > 0)
                {
                    sb.Append("&");
                }
                sb.Append(EncodeURIComponent(key));
                sb.Append("=");
                sb.Append(EncodeURIComponent(obj[key]));
            }
            return sb.ToString();
        }

        public static Dictionary<string, string> DecodeQuerystring(string qs)
        {
            var qry = new Dictionary<string, string>();
            var pairs = qs.Split('&');
            for (int i = 0; i < pairs.Length; i++)
            {
                var pair = pairs[i].Split('=');

                qry.Add(DecodeURIComponent(pair[0]), DecodeURIComponent(pair[1]));
            }
            return qry;
        }

        internal static byte[] StringToByteArray(string str)
        {
            int len = str.Length;
            var bytes = new byte[len];
            for (int i = 0; i < len; i++)
            {
                bytes[i] = (byte)str[i];
            }
            return bytes;
        }

        internal static string ByteArrayToString(byte[] bytes) => string.Concat(bytes.Select(b => b <= 0x7f ? (char)b : '?'));
    }
}
