using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace _011compressionbw
{
    class BufferedBitStream
    {
        public System.IO.Stream Stream { get; private set; }

        private int buffer;
        private int bufferLength;

        public BufferedBitStream(System.IO.Stream wrappedStream)
        {
            Stream = wrappedStream;
        }

        public int GetBits(int bitsCount)
        {
            // TODO
            return 0;
        }

        public void PutBits(int value, int bitsCount)
        {
            // TODO
        }
    }
}
