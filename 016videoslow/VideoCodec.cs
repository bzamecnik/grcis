using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Text;
using System.IO;
using System.IO.Compression;
using Support;
using System.Runtime.Serialization;

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

        protected Bitmap previousFrame = null;

        #endregion

        #region constructor

        public VideoCodec()
        {
        }

        #endregion

        #region Codec API

        public Stream EncodeHeader(int width, int height, float fps, Stream outStream)
        {
            if (outStream == null) return null;

            DeflateStream ds = new BufferedDeflateStream(16384, outStream, CompressionMode.Compress, true);

            frameWidth = width;
            frameHeight = height;
            framesPerSecond = fps;
            //previousFrame = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            // video header: [ MAGIC, width, height, fps ]
            ds.WriteUInt(MAGIC);
            ds.WriteShort((short)width);
            ds.WriteShort((short)height);

            ds.WriteShort((short)(100.0f * fps));

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

            for (int y = 0; y < frameHeight; y++)
            {
                for (int x = 0; x < frameWidth; x++)
                {
                    Color actualColor = inputFrame.GetPixel(x, y);
                    //Color predictedColor = previousFrame.GetPixel(x, y);
                    //outStream.WriteShort((short)(pixelColor.R - predictedColor.R));
                    //outStream.WriteShort((short)(pixelColor.G - predictedColor.G));
                    //outStream.WriteShort((short)(pixelColor.B - predictedColor.B));
                    outStream.WriteByte(actualColor.R);
                    outStream.WriteByte(actualColor.G);
                    outStream.WriteByte(actualColor.B);
                }
            }

            //previousFrame = inputFrame;
        }

        public Stream DecodeHeader(Stream inStream)
        {
            if (inStream == null) return null;

            DeflateStream ds = new DeflateStream(inStream, CompressionMode.Decompress, true);

            try
            {
                // Check the global header:
                if (ds.ReadUInt() != MAGIC) return null;

                frameWidth = ds.ReadShort();
                frameHeight = ds.ReadShort();
                framesPerSecond = ds.ReadShort() * 0.01f;
            }
            catch (EndOfStreamException ex)
            {
                return null;
            }

            //previousFrame = new Bitmap(frameWidth, frameHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

            return ds;
        }

        public Bitmap DecodeFrame(int frameIndex, Stream inStream)
        {
            if (inStream == null) return null;

            Bitmap result = null;
            try
            {
                // Check the frame header:
                if (inStream.ReadUInt() != MAGIC_FRAME) return null;

                int encodedFrameIndex = inStream.ReadShort();
                if (encodedFrameIndex != frameIndex) return null;

                result = new Bitmap(frameWidth, frameHeight, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                //short[] diff = new short[3];
                byte[] pixelColor = new byte[3];
                for (int y = 0; y < frameHeight; y++)
                {
                    for (int x = 0; x < frameWidth; x++)
                    {
                        //for (int band = 0; band < diff.Length; band++)
                        //{
                        //    diff[band] = inStream.ReadShort();
                        //}
                        //Color predicted = previousFrame.GetPixel(x, y);
                        //Color actual = Color.FromArgb(predicted.R + diff[0], predicted.G + diff[1], predicted.B + diff[2]);
                        //result.SetPixel(x, y, actual);
                        for (int band = 0; band < pixelColor.Length; band++)
                        {
                            pixelColor[band] = Extensions.ReadByte(inStream);
                        }
                        result.SetPixel(x, y, Color.FromArgb(pixelColor[0], pixelColor[1], pixelColor[2]));
                    }
                }
            }
            catch (EndOfStreamException ex)
            {
                return null;
            }

            previousFrame = result;

            return result;
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
