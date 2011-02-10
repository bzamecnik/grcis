using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Text;
using System.IO;
using System.IO.Compression;
using Support;
using System.Runtime.Serialization;
using System.Drawing.Imaging;

namespace _016videoslow
{
    class VideoCodec
    {
        #region protected data

        protected const uint DEFLATE_BUFFER_SIZE = 16384;

        protected const uint MAGIC = 0xfe4c128a;

        protected const uint MAGIC_FRAME = 0x1212fba1;

        protected int frameWidth = 0;

        protected int frameHeight = 0;

        public Size FrameSize {
            get {
                return new Size(frameWidth, frameHeight);
            }
        }

        protected float framesPerSecond = 0.0f;

        protected Bitmap currentFrame = null;
        protected Bitmap previousFrame = null;

        // TODO: make a checkbox for this
        protected bool visualizeMCBlockTypes = false;
        protected Bitmap debugFrame = null;
        protected BitmapData debugFrameData = null;

        protected bool useDeflateCompression = true;

        // both X and Y size of a motion compensation block
        protected int mcBlockSize = 8;
        protected MotionVector[] mcPossibleOffsets;

        StreamWriter log;

        #endregion

        #region constructor

        public VideoCodec(StreamWriter log)
        {
            mcPossibleOffsets = PreparePossibleMotionVectors();
            this.log = log;
        }

        #endregion

        #region Codec API

        public Stream EncodeHeader(Stream outStream, int width, int height, float fps, PixelFormat pixelFormat)
        {
            if (outStream == null) return null;

            Stream ds = (useDeflateCompression)
                ? new BufferedDeflateStream((int)DEFLATE_BUFFER_SIZE, outStream,
                    CompressionMode.Compress, true)
                : outStream;

            frameWidth = width;
            frameHeight = height;
            framesPerSecond = fps;
            previousFrame = new Bitmap(width, height, pixelFormat);

            // video header: [ MAGIC, width, height, fps, pixel format ]
            ds.WriteUInt(MAGIC);
            ds.WriteSShort((short)width);
            ds.WriteSShort((short)height);
            ds.WriteSShort((short)(100.0f * fps));
            ds.WriteUInt((uint)pixelFormat);

            return ds;
        }

        public void EncodeFrame(int frameIndex, Bitmap inputFrame, Stream outStream)
        {
            if ((inputFrame == null) || (outStream == null)) return;

            int width = inputFrame.Width;
            int height = inputFrame.Height;
            if ((width != frameWidth) || (height != frameHeight)) return;

            // frame header: [ MAGIC_FRAME, frameIndex ]
            outStream.WriteUInt(MAGIC_FRAME);
            outStream.WriteSShort((short)frameIndex);

            BitmapData inputData = inputFrame.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly, inputFrame.PixelFormat);
            BitmapData previousData = previousFrame.LockBits(
                new Rectangle(0, 0, width, height),
                ImageLockMode.ReadOnly, previousFrame.PixelFormat);


            int pixelBytes = GetBytesPerPixel(inputFrame.PixelFormat);
            if (frameIndex == 0)
            {
                EncodeIntraFrame(outStream, inputData, pixelBytes);
            }
            else
            {
                EncodePredictedFrame(outStream, inputData, previousData, pixelBytes);
            }

            inputFrame.UnlockBits(inputData);
            previousFrame.UnlockBits(previousData);

            previousFrame = inputFrame;
        }

        public Stream DecodeHeader(Stream inStream)
        {
            if (inStream == null) return null;

            Stream ds = (useDeflateCompression)
                ? new DeflateStream(inStream, CompressionMode.Decompress, true)
                : inStream;

            PixelFormat pixelFormat;

            try
            {
                // Check the global header:
                if (ds.ReadUInt() != MAGIC) return null;

                frameWidth = ds.ReadSShort();
                frameHeight = ds.ReadSShort();
                framesPerSecond = ds.ReadSShort() * 0.01f;
                pixelFormat = (PixelFormat)ds.ReadUInt();
            }
            catch (EndOfStreamException)
            {
                return null;
            }

            previousFrame = new Bitmap(frameWidth, frameHeight, pixelFormat);
            currentFrame = new Bitmap(frameWidth, frameHeight, pixelFormat);

            return ds;
        }

        public Bitmap DecodeFrame(int frameIndex, Stream inStream)
        {
            if (inStream == null) return null;

            try
            {
                // Check the frame header:
                if (inStream.ReadUInt() != MAGIC_FRAME) return null;

                int encodedFrameIndex = inStream.ReadSShort();
                if (encodedFrameIndex != frameIndex) return null;

                BitmapData currentData = currentFrame.LockBits(
                    new Rectangle(0, 0, frameWidth, frameHeight),
                    ImageLockMode.ReadOnly, currentFrame.PixelFormat);
                BitmapData previousData = previousFrame.LockBits(
                    new Rectangle(0, 0, frameWidth, frameHeight),
                    ImageLockMode.ReadOnly, previousFrame.PixelFormat);

                if (visualizeMCBlockTypes)
                {
                    debugFrame = new Bitmap(frameWidth, frameHeight, PixelFormat.Format24bppRgb);
                    debugFrameData = debugFrame.LockBits(
                        new Rectangle(0, 0, frameWidth, frameHeight),
                        ImageLockMode.ReadOnly, debugFrame.PixelFormat);
                }

                int pixelBytes = GetBytesPerPixel(currentFrame.PixelFormat);

                if (frameIndex == 0)
                {
                    DecodeIntraFrame(inStream, currentData, pixelBytes);
                }
                else
                {
                    DecodePredictedFrame(inStream, currentData, previousData, pixelBytes);
                }

                currentFrame.UnlockBits(currentData);
                previousFrame.UnlockBits(previousData);

                if (visualizeMCBlockTypes)
                {
                    debugFrame.UnlockBits(debugFrameData);
                    debugFrame.Save(String.Format("debug{0:000000}.png", frameIndex), ImageFormat.Png);
                }
            }
            catch (EndOfStreamException)
            {
                return null;
            }

            Bitmap result = currentFrame;
            // double buffering
            // save the current bitmap to act as previous one when decoding the next frame
            SwapBitmaps(ref previousFrame, ref currentFrame);

            return result;
        }

        #endregion

        private void EncodeIntraFrame(Stream outStream, BitmapData inputData, int pixelBytes)
        {
            // TODO:
            // use spatial prediction (similar to PNG)
            unsafe
            {
                for (int y = 0; y < frameHeight; y++)
                {
                    byte* inputRow = (byte*)inputData.Scan0 + (y * inputData.Stride);
                    for (int x = 0; x < frameWidth; x++)
                    {
                        // assume BGRA pixel format
                        for (int band = 2; band >= 0; band--)
                        {
                            outStream.WriteByte(inputRow[x * pixelBytes + band]);
                        }
                    }
                }
            }
        }

        private void EncodePredictedFrame(Stream outStream, BitmapData inputData, BitmapData previousData, int pixelBytes)
        {
            int xBlocksCount = DivideRoundUp(frameWidth, mcBlockSize);
            int yBlocksCount = DivideRoundUp(frameHeight, mcBlockSize);

            int identicalBlocksCount = 0;
            int translatedBlocksCount = 0;
            int fullBlocksCount = 0;

            for (int yBlock = 0; yBlock < yBlocksCount; yBlock++)
            {
                for (int xBlock = 0; xBlock < xBlocksCount; xBlock++)
                {
                    // absolute position of the block start
                    int yStart = yBlock * mcBlockSize;
                    int xStart = xBlock * mcBlockSize;

                    MotionVector motionVector;
                    bool motionVectorFound = SearchMotionVector(inputData,
                        previousData, pixelBytes, yStart, xStart, out motionVector);

                    if (motionVectorFound)
                    {
                        if ((motionVector.x == 0) && (motionVector.y == 0))
                        {
                            outStream.WriteByte((byte)MCRecordType.Identical);
                            identicalBlocksCount++;
                        }
                        else
                        {
                            // store a record type being a motion vector
                            outStream.WriteByte((byte)MCRecordType.MotionVector);
                            // store the motion vector itself
                            // TODO: store only a difference to the previous motion vector
                            // TODO: it could be possible to store only an index to the vector of
                            // possible motion vectors
                            outStream.WriteSShort(motionVector.x);
                            outStream.WriteSShort(motionVector.y);
                            translatedBlocksCount++;
                        }
                    }
                    else
                    {
                        // store a record type being a full block
                        outStream.WriteByte((byte)MCRecordType.FullBlock);
                        // store the full block contents
                        EncodeFullBlock(outStream, inputData, previousData, pixelBytes, yStart, xStart);
                        fullBlocksCount++;
                    }
                }
            }
            Log("Block counts - identical: {0}, translated: {1}, full: {2}", identicalBlocksCount, translatedBlocksCount, fullBlocksCount);
        }

        unsafe private bool SearchMotionVector(BitmapData inputData, BitmapData previousData, int pixelBytes, int yStart, int xStart, out MotionVector motionVector)
        {
            byte* inputPtr = (byte*)inputData.Scan0;
            byte* previousPtr = (byte*)previousData.Scan0;

            // check possible offsets to find an equal shifted
            // block in the previous frame
            for (int i = 0; i < mcPossibleOffsets.Length; i++)
            {
                motionVector = mcPossibleOffsets[i];
                if (TestMotionVector(inputData, previousData, pixelBytes, yStart, xStart, inputPtr, previousPtr, motionVector))
                {
                    return true;
                }
            }
            motionVector = MotionVector.ZERO;
            return false;
        }

        unsafe private bool TestMotionVector(BitmapData inputData, BitmapData previousData, int pixelBytes, int yStart, int xStart, byte* inputPtr, byte* previousPtr, MotionVector motionVector)
        {
            for (int y = yStart; y < yStart + mcBlockSize; y++)
            {
                byte* inputRow = inputPtr + (y * inputData.Stride);
                byte* previousRow = previousPtr + (y * previousData.Stride);
                for (int x = xStart; x < xStart + mcBlockSize; x++)
                {
                    int xSource = x + motionVector.x;
                    int ySource = y + motionVector.y;
                    if ((x < 0) || (x >= frameWidth) ||
                        (y < 0) || (y >= frameHeight) ||
                        (xSource < 0) || (xSource >= frameWidth) ||
                        (ySource < 0) || (ySource >= frameHeight))
                    {
                        return false;
                    }
                    // assume BGRA pixel format
                    for (int band = 0; band < 3; band++)
                    {
                        int inputIndex = x * pixelBytes + band;
                        int previousIndex = motionVector.y * previousData.Stride + xSource * pixelBytes + band;
                        if (inputRow[inputIndex] != previousRow[previousIndex])
                        {
                            // means: inputFrame[x, y] != previousFrame[xSource, ySource])
                            return false;
                        }
                    }
                }
            }
            return true;
        }

        unsafe private void EncodeFullBlock(Stream outStream, BitmapData inputData, BitmapData previousData, int pixelBytes, int yStart, int xStart)
        {
            byte* inputPtr = (byte*)inputData.Scan0;
            byte* previousPtr = (byte*)previousData.Scan0;
            int yMax = Math.Min(yStart + mcBlockSize, frameHeight);
            int xMax = Math.Min(xStart + mcBlockSize, frameWidth);
            for (int y = yStart; y < yMax; y++)
            {
                byte* inputRow = inputPtr + (y * inputData.Stride);
                byte* previousRow = previousPtr + (y * previousData.Stride);
                for (int x = xStart; x < xMax; x++)
                {
                    // store (inputFrame[x, y] - previousFrame[x, y])
                    // assume BGRA input pixel format, store as RGB
                    for (int band = 2; band >= 0; band--)
                    {
                        // temporal prediction
                        int index = x * pixelBytes + band;
                        byte diff = (byte)(inputRow[index] - previousRow[index]);
                        outStream.WriteByte(diff);
                    }
                }
            }
        }

        private int DivideRoundUp(int numerator, int denominator)
        {
            return (numerator / denominator) + ((numerator % denominator > 0) ? 1 : 0);
        }

        private void DecodeIntraFrame(Stream inStream, BitmapData currentData, int pixelBytes)
        {
            // TODO:
            // use spatial prediction (similar to PNG)
            unsafe
            {
                for (int y = 0; y < frameHeight; y++)
                {
                    byte* currentRow = (byte*)currentData.Scan0 + (y * currentData.Stride);
                    for (int x = 0; x < frameWidth; x++)
                    {
                        // assume BGRA pixel format
                        for (int band = 2; band >= 0; band--)
                        {
                            currentRow[x * pixelBytes + band] = inStream.ReadUByte();
                        }
                        if (pixelBytes == 4)
                        {
                            currentRow[x * pixelBytes + 3] = 255; // assume full alpha 
                        }
                    }
                }
            }
        }

        private void DecodePredictedFrame(Stream inStream, BitmapData currentData, BitmapData previousData, int pixelBytes)
        {
            int xBlocksCount = DivideRoundUp(frameWidth, mcBlockSize);
            int yBlocksCount = DivideRoundUp(frameHeight, mcBlockSize);

            MotionVector motionVector = MotionVector.ZERO;

            for (int yBlock = 0; yBlock < yBlocksCount; yBlock++)
            {
                for (int xBlock = 0; xBlock < xBlocksCount; xBlock++)
                {
                    int yStart = yBlock * mcBlockSize;
                    int xStart = xBlock * mcBlockSize;
                    MCRecordType mcType = (MCRecordType)inStream.ReadUByte();
                    switch (mcType)
                    {
                        case MCRecordType.Identical:
                            DecodeTranslatedBlock(inStream, currentData, previousData, pixelBytes, yStart, xStart, MotionVector.ZERO);
                            break;
                        case MCRecordType.MotionVector:
                            motionVector.x = inStream.ReadSShort();
                            motionVector.y = inStream.ReadSShort();

                            DecodeTranslatedBlock(inStream, currentData, previousData, pixelBytes, yStart, xStart, motionVector);
                            break;
                        case MCRecordType.FullBlock:
                            DecodeFullBlock(inStream, currentData, previousData, pixelBytes, yStart, xStart);
                            break;
                    }
                }
            }
        }

        unsafe private void DecodeFullBlock(Stream inStream, BitmapData currentData, BitmapData previousData, int pixelBytes, int yStart, int xStart)
        {
            byte* currentPtr = (byte*)currentData.Scan0;
            byte* previousPtr = (byte*)previousData.Scan0;
            int debugPixelBytes = pixelBytes;
            if (visualizeMCBlockTypes)
            {
                debugPixelBytes = GetBytesPerPixel(debugFrame.PixelFormat);
            }
            int yMax = Math.Min(yStart + mcBlockSize, frameHeight);
            int xMax = Math.Min(xStart + mcBlockSize, frameWidth);

            for (int y = yStart; y < yMax; y++)
            {
                byte* currentRow = currentPtr + (y * currentData.Stride);
                byte* previousRow = previousPtr + (y * previousData.Stride);
                byte* debugRow = (byte*)0;
                if (visualizeMCBlockTypes)
                {
                    debugRow = (byte*)debugFrameData.Scan0 + (y * debugFrameData.Stride);
                }
                for (int x = xStart; x < xMax; x++)
                {
                    // assume BGRA input pixel format, store as RGB
                    for (int band = 2; band >= 0; band--)
                    {
                        // temporal prediction
                        byte diff = inStream.ReadUByte();
                        int index = x * pixelBytes + band;
                        currentRow[index] = (byte)(previousRow[index] + diff);
                    }

                    if (visualizeMCBlockTypes)
                    {
                        int index = x * debugPixelBytes;
                        debugRow[index] = 0;
                        debugRow[index + 1] = 0;
                        debugRow[index + 2] = 255; // red
                    }
                }
            }
        }

        unsafe private void DecodeTranslatedBlock(Stream inStream, BitmapData currentData, BitmapData previousData, int pixelBytes, int yStart, int xStart, MotionVector motionVector)
        {
            byte* currentPtr = (byte*)currentData.Scan0;
            byte* previousPtr = (byte*)previousData.Scan0;
            int debugPixelBytes = pixelBytes;
            if (visualizeMCBlockTypes)
            {
                debugPixelBytes = GetBytesPerPixel(debugFrame.PixelFormat);
            }

            bool isIdenticalBlock = (motionVector.x == 0) && (motionVector.y == 0);

            for (int y = yStart; y < yStart + mcBlockSize; y++)
            {
                byte* currentRow = currentPtr + (y * currentData.Stride);
                byte* previousRow = previousPtr + ((y + motionVector.y) * previousData.Stride);
                byte* debugRow = (byte*)0;
                if (visualizeMCBlockTypes)
                {
                    debugRow = (byte*)debugFrameData.Scan0 + (y * debugFrameData.Stride);
                }
                for (int x = xStart; x < xStart + mcBlockSize; x++)
                {
                    // assume BGRA input pixel format, store as RGB
                    for (int band = 2; band >= 0; band--)
                    {
                        // temporal prediction
                        int currentIndex = x * pixelBytes + band;
                        int previousIndex = currentIndex + motionVector.x * pixelBytes;
                        currentRow[currentIndex] = previousRow[previousIndex];
                    }
                    if (pixelBytes == 4)
                    {
                        currentRow[x * pixelBytes + 3] = 255; // assume full alpha 
                    }
                    if (visualizeMCBlockTypes)
                    {
                        // identical - green
                        // translated - blue
                        int index = x * debugPixelBytes;
                        debugRow[index] = (byte)((isIdenticalBlock) ? 0 : 255);
                        debugRow[index + 1] = (byte)((isIdenticalBlock) ? 255 : 0);
                        debugRow[index + 2] = 0;
                    }
                }
            }
        }

        private void SwapBitmaps(ref Bitmap previousFrame, ref Bitmap currentFrame)
        {
            Bitmap swapped = previousFrame;
            previousFrame = currentFrame;
            currentFrame = swapped;
        }

        private int GetBytesPerPixel(PixelFormat pixelFormat)
        {
            switch (pixelFormat)
            {
                case PixelFormat.Format24bppRgb: return 3;
                case PixelFormat.Format32bppArgb: return 4;
                default:
                    throw new ArgumentException("Unsupported pixel format");
            }
        }

        private MotionVector[] PreparePossibleMotionVectors()
        {
            List<MotionVector> vectors = new List<MotionVector>();
            // Origin - no translation.
            // This is most probable.
            vectors.Add(MotionVector.ZERO);
            // Add offsets for vertical and horizontal translation.
            // This is very probable.
            int maxDistance = 64;
            for (short i = 1; i < maxDistance; i++)
            {
                vectors.Add(new MotionVector(0, i));
                vectors.Add(new MotionVector(0, (short)-i));
            }
            for (short i = 1; i < maxDistance; i++)
            {
                vectors.Add(new MotionVector(i, 0));
                vectors.Add(new MotionVector((short)-i, 0));
            }
            // Add offsets for exhaustive search in remaining positions
            // within a defined square (possible smaller than the V and H directions).
            // This is less probable.
            int squareSize = 16;
            for (short i = 1; i < squareSize; i++)
            {
                for (short j = 1; j < squareSize; j++)
                {
                    // right down quadrant
                    vectors.Add(new MotionVector(i, j));
                    // right up quadrant
                    vectors.Add(new MotionVector(i, (short)-j));
                    // left up quadrant
                    vectors.Add(new MotionVector((short)-i, j));
                    // left down quadrant
                    vectors.Add(new MotionVector((short)-i, (short)-j));
                }
            }
            return vectors.ToArray();
        }

        private void Log(String message, params object[] parameters)
        {
            if (log != null)
            {
                log.WriteLine(String.Format(message, parameters));
            }
        }
    }

    /// <summary>
    /// Type of a motion compensation block record.
    /// </summary>
    public enum MCRecordType
    {
        /// <summary>
        /// The motion vector is [0, 0], ie. the block is identical to the
        /// block in the previous frame and there is no offset needed to be
        /// stored.
        /// </summary>
        Identical,
        /// <summary>
        /// The record contains only a motion vector, ie. offset of an identical
        /// block found in a previous frame.
        /// </summary>
        MotionVector,
        /// <summary>
        /// The records contains the full block, ie. values for all pixels.
        /// </summary>
        FullBlock,
    }

    public struct MotionVector {
        public short x;
        public short y;

        public static readonly MotionVector ZERO = new MotionVector(0, 0);

        public MotionVector(short x, short y)
        {
            this.x = x;
            this.y = y;
        }

        public override string ToString()
        {
            return string.Format("[{0}, {1}]", x, y);
        }
    }

    public class EndOfStreamException : System.Exception, ISerializable
    {
        public EndOfStreamException()
        {
        }

        public EndOfStreamException(string message)
            : base(message)
        {
        }

        public EndOfStreamException(string message, Exception inner)
            : base(message, inner)
        {
        }

        // This constructor is needed for serialization.
        protected EndOfStreamException(SerializationInfo info, StreamingContext context)
            : base(info, context)
        {
        }
    }

    public static class Extensions
    {
        public static void WriteSShort(this Stream outs, short number)
        {
            outs.WriteByte((byte)((number >> 8) & 0xff));
            outs.WriteByte((byte)(number & 0xff));
        }

        // [-255;255]
        public static void WriteSByte(this Stream outs, SByte number)
        {
            // sign:
            // 255 = minus, 0 = zero, 1 = plus
            outs.WriteByte((byte)(Math.Sign(number) & 0xff));
            outs.WriteByte((byte)(Math.Abs((int)number) & 0xff));
        }

        public static void WriteUInt(this Stream outs, uint number)
        {
            outs.WriteByte((byte)((number >> 24) & 0xff));
            outs.WriteByte((byte)((number >> 16) & 0xff));
            outs.WriteByte((byte)((number >> 8) & 0xff));
            outs.WriteByte((byte)(number & 0xff));
        }

        public static byte ReadUByte(this Stream inputStream)
        {
            int number = inputStream.ReadByte();
            if (number < 0) throw new EndOfStreamException();
            return (byte)number;
        }

        public static SByte ReadSByte(this Stream inputStream)
        {
            int sign = inputStream.ReadByte();
            if (sign < 0) throw new EndOfStreamException();
            int number = inputStream.ReadByte();
            if (number < 0) throw new EndOfStreamException();
            return (SByte)(((SByte)sign) * ((byte)number));
        }

        public static short ReadSShort(this Stream inputStream)
        {
            int number = 0;
            for (int i = 0; i < 2; i++)
            {
                int buffer = inputStream.ReadByte();
                if (buffer < 0) throw new EndOfStreamException();
                number = (number << 8) + buffer;
            }
            return (short)number;
        }

        public static uint ReadUInt(this Stream inputStream)
        {
            uint number = 0;
            for (int i = 0; i < 4; i++)
            {
                int buffer = inputStream.ReadByte();
                if (buffer < 0) throw new EndOfStreamException();
                number = (uint)((number << 8) + buffer);
            }
            return number;
        }
    }
}
