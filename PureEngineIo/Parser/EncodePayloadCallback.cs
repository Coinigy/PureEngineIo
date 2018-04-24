using PureEngineIo.Interfaces;
using System.Collections.Generic;

namespace PureEngineIo.Parser
{
    internal class EncodePayloadCallback : IEncodeCallback
    {
        private readonly List<byte[]> _results;

        public EncodePayloadCallback(List<byte[]> results)
        {
            _results = results;
        }

        public void Call(object data)
        {
            if (data is string packet)
            {
	            var encodingLength = packet.Length.ToString();
                var sizeBuffer = new byte[encodingLength.Length + 2];
                sizeBuffer[0] = 0; // is a string
                for (var i = 0; i < encodingLength.Length; i++)
                {
                    sizeBuffer[i + 1] = byte.Parse(encodingLength.Substring(i, 1));
                }
                sizeBuffer[sizeBuffer.Length - 1] = 255;
                _results.Add(Buffer.Concat(new[] { sizeBuffer, Helpers.StringToByteArray(packet) }));
                return;
            }

            var packet1 = (byte[])data;
            var encodingLength1 = packet1.Length.ToString();
            var sizeBuffer1 = new byte[encodingLength1.Length + 2];
            sizeBuffer1[0] = 1; // is binary
            for (var i = 0; i < encodingLength1.Length; i++)
            {
                sizeBuffer1[i + 1] = byte.Parse(encodingLength1.Substring(i, 1));
            }
            sizeBuffer1[sizeBuffer1.Length - 1] = 255;
            _results.Add(Buffer.Concat(new[] { sizeBuffer1, packet1 }));
        }
    }
}
