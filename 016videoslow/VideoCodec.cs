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

        protected const uint MAGIC = 0xfe4c128a;

        protected const uint MAGIC_FRAME = 0x1212fba1;

        protected int frameWidth = 0;

        protected int frameHeight = 0;

        protected float framesPerSecond = 0.0f;

        protected Bitmap currentFrame = null;
        protected Bitmap previousFrame = null;

        protected bool visualizeMCBlockTypes = true;
        private Bitmap debugFrame = null;
        private BitmapData debugFrameData = null;

        // both X and Y size of a motion compensation block
        protected int mcBlockSize = 8;
        protected int[] mcPossibleOffsets;

        #endregion

        #region constructor

        public VideoCodec()
        {
            mcPossibleOffsets = PreparePossibleMotionVectors();
        }

        #endregion

        #region Codec API

        public Stream EncodeHeader(Stream outStream, int width, int height, float fps, PixelFormat pixelFormat)
        {
            if (outStream == null) return null;

            DeflateStream ds = new BufferedDeflateStream(16384, outStream, CompressionMode.Compress, true);
            //Stream ds = outStream;

            frameWidth = width;
            frameHeight = height;
            framesPerSecond = fps;
            previousFrame = new Bitmap(width, height, pixelFormat);

            // video header: [ MAGIC, width, height, fps, pixel format ]
            ds.WriteUInt(MAGIC);
            ds.WriteShort((short)width);
            ds.WriteShort((short)height);
            ds.WriteShort((short)(100.0f * fps));
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
            outStream.WriteShort((short)frameIndex);

            BitmapData inputData = inputFrame.LockBits(new Rectangle(0, 0, width, height), System.Drawing.Imaging.ImageLockMode.ReadOnly, inputFrame.PixelFormat);
            BitmapData previousData = previousFrame.LockBits(new Rectangle(0, 0, width, height), System.Drawing.Imaging.ImageLockMode.ReadOnly, previousFrame.PixelFormat);
            

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

            for (int yBlock = 0; yBlock < yBlocksCount; yBlock++)
            {
                for (int xBlock = 0; xBlock < xBlocksCount; xBlock++)
                {
                    // absolute position of the block start
                    int yStart = yBlock * mcBlockSize;
                    int xStart = xBlock * mcBlockSize;

                    int motionVectorOffset;
                    bool motionVectorFound = SearchMotionVector(inputData, previousData, pixelBytes, yStart, xStart, out motionVectorOffset);

                    if (motionVectorFound)
                    {
                        // store a record type being a motion vector
                        outStream.WriteByte((byte)MCRecordType.MotionVector);
                        // store the motion vector itself
                        // TODO: store only a difference to the previous motion vector
                        // TODO: it could be possible to store only an index to the vector of
                        // possible motion vectors
                        outStream.WriteShort((short)mcPossibleOffsets[motionVectorOffset]);
                        outStream.WriteShort((short)mcPossibleOffsets[motionVectorOffset + 1]);
                    }
                    else
                    {
                        // store a record type being a full block
                        outStream.WriteByte((byte)MCRecordType.FullBlock);
                        // store the full block contents
                        EncodeFullBlock(outStream, inputData, previousData, pixelBytes, yStart, xStart);
                    }
                }
            }
        }

        unsafe private bool SearchMotionVector(BitmapData inputData, BitmapData previousData, int pixelBytes, int yStart, int xStart, out int motionVectorOffset)
        {
            byte* inputPtr = (byte*)inputData.Scan0;
            byte* previousPtr = (byte*)previousData.Scan0;

            bool motionVectorFound = false;
            motionVectorOffset = 0;

            // check possible offsets to find an equal shifted
            // block in the previous frame
            for (int i = 0; !motionVectorFound && (i < mcPossibleOffsets.Length); i += 2)
            {
                bool isValidOffset = true;
                for (int y = yStart; isValidOffset && (y < yStart + mcBlockSize); y++)
                {
                    byte* inputRow = inputPtr + (y * inputData.Stride);
                    byte* previousRow = previousPtr + (y * previousData.Stride);
                    for (int x = xStart; isValidOffset && (x < xStart + mcBlockSize); x++)
                    {
                        int xSource = x + mcPossibleOffsets[i];
                        int ySource = y + mcPossibleOffsets[i + 1];
                        if ((x < 0) || (x >= frameWidth) ||
                            (y < 0) || (y >= frameHeight) ||
                            (xSource < 0) || (xSource >= frameWidth) ||
                            (ySource < 0) || (ySource >= frameHeight))
                        {
                            isValidOffset = false;
                            break;
                        }
                        // assume BGRA pixel format
                        for (int band = 0; band < 2; band++)
                        {
                            int inputIndex = x * pixelBytes + band;
                            int previousIndex = mcPossibleOffsets[i + 1] * previousData.Stride + xSource * pixelBytes + band;
                            if (inputRow[inputIndex] != previousRow[previousIndex])
                            {
                                // means: inputFrame[x, y] != previousFrame[xSource, ySource])
                                isValidOffset = false;
                                break;
                            }
                        }
                    }
                }
                motionVectorFound = isValidOffset;
                motionVectorOffset = i;
            }
            return motionVectorFound;
        }

        unsafe private void EncodeFullBlock(Stream outStream, BitmapData inputData, BitmapData previousData, int pixelBytes, int yStart, int xStart)
        {
            byte* inputPtr = (byte*)inputData.Scan0;
            byte* previousPtr = (byte*)previousData.Scan0;
            for (int y = yStart; y < yStart + mcBlockSize; y++)
            {
                byte* inputRow = inputPtr + (y * inputData.Stride);
                byte* previousRow = previousPtr + (y * previousData.Stride);
                for (int x = xStart; x < xStart + mcBlockSize; x++)
                {
                    // store (inputFrame[x, y] - previousFrame[x, y])
                    // assume BGRA input pixel format, store as RGB
                    for (int band = 2; band >= 0; band--)
                    {
                        // temporal prediction
                        int index = x * pixelBytes + band;
                        SByte diff = (SByte)(inputRow[index] - previousRow[index]);
                        outStream.WriteSByte(diff);
                    }
                }
            }
        }

        private int DivideRoundUp(int numerator, int denominator)
        {
            return (numerator / denominator) + ((numerator % denominator > 0) ? 1 : 0);
        }

        public Stream DecodeHeader(Stream inStream)
        {
            if (inStream == null) return null;

            DeflateStream ds = new DeflateStream(inStream, CompressionMode.Decompress, true);
            //Stream ds = inStream;

            PixelFormat pixelFormat;

            try
            {
                // Check the global header:
                if (ds.ReadUInt() != MAGIC) return null;

                frameWidth = ds.ReadShort();
                frameHeight = ds.ReadShort();
                framesPerSecond = ds.ReadShort() * 0.01f;
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

                int encodedFrameIndex = inStream.ReadShort();
                if (encodedFrameIndex != frameIndex) return null;

                BitmapData currentData = currentFrame.LockBits(new Rectangle(0, 0, frameWidth, frameHeight), System.Drawing.Imaging.ImageLockMode.ReadOnly, currentFrame.PixelFormat);
                BitmapData previousData = previousFrame.LockBits(new Rectangle(0, 0, frameWidth, frameHeight), System.Drawing.Imaging.ImageLockMode.ReadOnly, previousFrame.PixelFormat);

                if (visualizeMCBlockTypes)
                {
                    debugFrame = new Bitmap(frameWidth, frameHeight, currentData.PixelFormat);
                    debugFrameData = debugFrame.LockBits(new Rectangle(0, 0, frameWidth, frameHeight), System.Drawing.Imaging.ImageLockMode.ReadOnly, previousFrame.PixelFormat);
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
                    debugFrame.Save(String.Format("debug{0:000000}.png", frameIndex + 1), ImageFormat.Png);
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
                            currentRow[x * pixelBytes + band] = Extensions.ReadByte(inStream);
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

            for (int yBlock = 0; yBlock < yBlocksCount; yBlock++)
            {
                for (int xBlock = 0; xBlock < xBlocksCount; xBlock++)
                {
                    int yStart = yBlock * mcBlockSize;
                    int xStart = xBlock * mcBlockSize;
                    MCRecordType mcType = (MCRecordType)Extensions.ReadByte(inStream);
                    switch (mcType)
                    {
                        case MCRecordType.FullBlock:
                            DecodeFullBlock(inStream, currentData, previousData, pixelBytes, yStart, xStart);
                            break;
                        case MCRecordType.MotionVector:
                            DecodeTranslatedBlock(inStream, currentData, previousData, pixelBytes, yStart, xStart);
                            break;
                    }
                }
            }
        }

        unsafe private void DecodeFullBlock(Stream inStream, BitmapData currentData, BitmapData previousData, int pixelBytes, int yStart, int xStart)
        {
            byte* currentPtr = (byte*)currentData.Scan0;
            byte* previousPtr = (byte*)previousData.Scan0;
            for (int y = yStart; y < yStart + mcBlockSize; y++)
            {
                byte* currentRow = currentPtr + (y * currentData.Stride);
                byte* previousRow = previousPtr + (y * previousData.Stride);
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
                        SByte diff = inStream.ReadSByte();
                        int index = x * pixelBytes + band;
                        currentRow[index] = (byte)(previousRow[index] + diff);
                    }

                    if (visualizeMCBlockTypes)
                    {
                        int index = x * pixelBytes;
                        debugRow[index] = 0;
                        debugRow[index + 1] = 255;
                        debugRow[index + 2] = 0;
                    }
                }
            }
        }

        unsafe private void DecodeTranslatedBlock(Stream inStream, BitmapData currentData, BitmapData previousData, int pixelBytes, int yStart, int xStart)
        {
            int xOffset = inStream.ReadShort();
            int yOffset = inStream.ReadShort();

            byte* currentPtr = (byte*)currentData.Scan0;
            byte* previousPtr = (byte*)previousData.Scan0;
            for (int y = yStart; y < yStart + mcBlockSize; y++)
            {
                byte* inputRow = currentPtr + (y * currentData.Stride);
                byte* previousRow = previousPtr + ((y + yOffset) * previousData.Stride);
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
                        int inputIndex = x * pixelBytes + band;
                        int previousIndex = inputIndex + xOffset * pixelBytes;
                        inputRow[inputIndex] = previousRow[previousIndex];
                    }
                    if (visualizeMCBlockTypes)
                    {
                        int index = x * pixelBytes;
                        debugRow[index] = 0;
                        debugRow[index + 1] = 0;
                        debugRow[index + 2] = 255;
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

        #endregion

        private int[] PreparePossibleMotionVectors()
        {
            List<int> vectors = new List<int>();
            // origin
            vectors.Add(0);
            vectors.Add(0);
            // vertical and horizontal translation
            int maxDistance = 64;
            for (int i = 0; i < maxDistance; i++)
            {
                vectors.Add(0);
                vectors.Add(i);
                vectors.Add(0);
                vectors.Add(-i);
            }
            for (int i = 0; i < maxDistance; i++)
            {
                vectors.Add(i);
                vectors.Add(0);
                vectors.Add(-i);
                vectors.Add(0);
            }
            return vectors.ToArray();
        }

    }

    /// <summary>
    /// Type of a motion compensation block record.
    /// </summary>
    public enum MCRecordType
    {
        /// <summary>
        /// The record contains only a motion vector, ie. offset of an equal
        /// block found in a previous frame.
        /// </summary>
        MotionVector,
        /// <summary>
        /// The records contains the full block, ie. values for all pixels.
        /// </summary>
        FullBlock,
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
        public static void WriteShort(this Stream outs, short number)
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

        public static byte ReadByte(this Stream inputStream)
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

        public static short ReadShort(this Stream inputStream)
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

        public static Color plus(this Color color, Color another)
        {
            return Color.FromArgb(color.R + another.R, color.G + another.G, color.B + another.B);
        }

        public static Color minus(this Color color, Color another)
        {
            return Color.FromArgb(color.R - another.R, color.G - another.G, color.B - another.B);
        }
    }

}
