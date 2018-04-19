using System.Collections.Immutable;
using System.Runtime.Serialization;
using Utf8Json;

namespace PureEngineIo
{
    public class HandshakeData
    {
        [DataMember(Name = "sid")]
        public string Sid;

        [DataMember(Name = "upgrades")]
        public ImmutableList<string> Upgrades = ImmutableList<string>.Empty;

        [DataMember(Name = "pingInterval")]
        public long PingInterval;

        [DataMember(Name = "pingTimeout")]
        public long PingTimeout;

        public HandshakeData()
        {
        }

        internal static HandshakeData FromString(string data)
        {
            return JsonSerializer.Deserialize<HandshakeData>(data);
        }
    }
}
