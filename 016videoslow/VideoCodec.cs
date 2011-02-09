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

        #endregion

        #region constructor

        public VideoCodec()
        {
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

            unsafe
            {
                for (int y = 0; y < frameHeight; y++)
                {
                    byte* inputRow = (byte*)inputData.Scan0 + (y * inputData.Stride);
                    byte* previousRow = (byte*)previousData.Scan0 + (y * previousData.Stride);
                    for (int x = 0; x < frameWidth; x++)
                    {
                        // assume BGRA pixel format
                        for (int band = 2; band >= 0; band--)
                        {
                            int index = x * pixelBytes + band;
                            outStream.WriteShort((short)(inputRow[index] - previousRow[index]));
                        }
                    }
                }
            }

            inputFrame.UnlockBits(inputData);
            previousFrame.UnlockBits(previousData);

            previousFrame = inputFrame;
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

                int pixelBytes = GetBytesPerPixel(currentFrame.PixelFormat);

                unsafe
                {
                    for (int y = 0; y < frameHeight; y++)
                    {
                        byte* currentRow = (byte*)currentData.Scan0 + (y * currentData.Stride);
                        byte* previousRow = (byte*)previousData.Scan0 + (y * previousData.Stride);
                        for (int x = 0; x < frameWidth; x++)
                        {
                            // BGRA
                            for (int band = 2; band >= 0; band--)
                            {
                                short diff = inStream.ReadShort();
                                int index = x * pixelBytes + band;
                                currentRow[index] = (byte)(previousRow[index] + diff);
                            }
                            if (pixelBytes == 4)
                            {
                                currentRow[x * pixelBytes + 3] = 255; // assume full alpha
                            }
                        }
                    }
                }
                currentFrame.UnlockBits(currentData);
                previousFrame.UnlockBits(previousData);
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
            :  base(message, inner)
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

        public static short ReadShort(this Stream inputStream)
        {
            int number = 0;
            for (int i = 0; i < 2; i++)
            {
                int buffer = inputStream.ReadByte();
                if (buffer < 0) throw new EndOfStreamException();
                number = (number << 8) + buffer;
            }
            return (short) number;
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
