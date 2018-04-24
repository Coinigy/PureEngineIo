using System.Linq;

namespace PureEngineIo.Parser
{
    internal class Buffer
    {

        private Buffer()
        {
        }

        public static byte[] Concat(byte[][] list)
        {
            var length = list.Sum(buf => buf.Length);

	        return Concat(list, length);
        }

        public static byte[] Concat(byte[][] list, int length)
        {
            if (list.Length == 0)
            {
                return new byte[0];
            }
            if (list.Length == 1)
            {
                return list[0];
            }

            var buffer = ByteBuffer.Allocate(length);
            foreach (var buf in list)
            {
                buffer.Put(buf);
            }

            return buffer.Array();
        }
    }
}
