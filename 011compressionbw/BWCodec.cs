using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Text;
using System.IO;
using System.IO.Compression;
using Support;

namespace _011compressionbw
{
    class BWCodec
    {
        #region protected data

        protected const uint MAGIC = 0xff12fe45;

        protected Predictor Predictor = new PreviousLeftPixelPredictor();

        #endregion

        #region constructor

        public BWCodec()
        {
        }

        #endregion

        #region Codec API

        public void EncodeImage(Bitmap inputImage, Stream outputStream)
        {
            if (inputImage == null ||
                 outputStream == null) return;

            int width = inputImage.Width;
            int height = inputImage.Height;

            if (width < 1 || height < 1) return;

            // !!!{{ TODO: add the encoding code here

            DeflateStream ds = new BufferedDeflateStream(16384, outputStream, CompressionMode.Compress, true);

            try
            {
                // file header: [ MAGIC, width, height ]
                ds.WriteByte((byte)((MAGIC >> 24) & 0xff));
                ds.WriteByte((byte)((MAGIC >> 16) & 0xff));
                ds.WriteByte((byte)((MAGIC >> 8) & 0xff));
                ds.WriteByte((byte)(MAGIC & 0xff));

                ds.WriteByte((byte)((width >> 8) & 0xff));
                ds.WriteByte((byte)(width & 0xff));

                ds.WriteByte((byte)((height >> 8) & 0xff));
                ds.WriteByte((byte)(height & 0xff));

                int buffer = 0;
                int bufLen = 0;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        // threshold the intensity in case the image is not already black/white
                        // and convert is from [0.0; 1.0] to {0,1}
                        int bwIntensity = BWImageHelper.GetBWPixel(inputImage, x, y);

                        int predictedIntensity = Predictor.Predict(inputImage, x, y);
                        int errorValue = bwIntensity ^ predictedIntensity;
                        //int errorValue = bwIntensity;
                        
                        // shift the buffer to the left and add the next bit value
                        // equivalent to:
                        //   buffer = 2 * buffer + bitValue
                        // or:
                        //   buffer = (buffer << 1) + bitValue
                        buffer += buffer + errorValue;
                        // store up to 8 bits (= 8 pixels) in a single byte
                        if (++bufLen == 8)
                        {
                            ds.WriteByte((byte)(buffer & 0xff));
                            buffer = 0;
                            bufLen = 0;
                        }
                    }

                    // end of scanline
                    if (bufLen > 0)
                    {
                        // shift the remaining bits completely to the left
                        // (so that the rest of the byte is filled with zeros only)
                        buffer <<= 8 - bufLen;
                        ds.WriteByte((byte)(buffer & 0xff));
                        buffer = 0;
                        bufLen = 0;
                    }
                }

            }
            finally
            {
                if (ds != null)
                {
                    ds.Close();
                }
            }

            // !!!}}
        }

        public Bitmap DecodeImage(Stream inps)
        {
            if (inps == null) return null;

            // !!!{{ TODO: add the decoding code here

            DeflateStream ds = new DeflateStream(inps, CompressionMode.Decompress, true);
            Bitmap decodedImage = null;

            try
            {
                int buffer;
                buffer = ds.ReadByte();
                if (buffer < 0 || buffer != ((MAGIC >> 24) & 0xff)) return null;
                buffer = ds.ReadByte();
                if (buffer < 0 || buffer != ((MAGIC >> 16) & 0xff)) return null;
                buffer = ds.ReadByte();
                if (buffer < 0 || buffer != ((MAGIC >> 8) & 0xff)) return null;
                buffer = ds.ReadByte();
                if (buffer < 0 || buffer != (MAGIC & 0xff)) return null;

                int width, height;
                width = ds.ReadByte();
                if (width < 0) return null;
                buffer = ds.ReadByte();
                if (buffer < 0) return null;
                width = (width << 8) + buffer;
                height = ds.ReadByte();
                if (height < 0) return null;
                buffer = ds.ReadByte();
                if (buffer < 0) return null;
                height = (height << 8) + buffer;

                if (width < 1 || height < 1)
                    return null;

                decodedImage = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                int bufLen = 0;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        if (bufLen == 0)
                        {
                            buffer = ds.ReadByte();
                            if (buffer < 0) return null;
                            bufLen = 8;
                        }
                        // get the leftmost bit from the byte - the predictor error value
                        int errorValue = (buffer & 0x80) >> 7;
                        int predictedIntensity = Predictor.Predict(decodedImage, x, y);
                        int decodedBWIntensity = predictedIntensity ^ errorValue;
                        //int decodedBWIntensity = errorValue;
                        BWImageHelper.SetBWPixel(decodedImage, x, y, decodedBWIntensity);
                        // shift the buffer to the right by 1 bit
                        buffer += buffer;
                        bufLen--;
                    }
                    bufLen = 0;
                }
            }
            finally
            {
                if (ds != null)
                {
                    ds.Close();
                }
            }
            return decodedImage;

            // !!!}}
        }

        #endregion

    }

    interface Predictor
    {
        int Predict(Bitmap image, int x, int y);
    }

    class PreviousLeftPixelPredictor : Predictor
    {
        public int Predict(Bitmap image, int x, int y)
        {
            int currentPixel = BWImageHelper.GetBWPixel(image, x, y);
            int leftDiff = BWImageHelper.GetBWPixel(image, x - 1, y);
            return leftDiff;
        }
    }

    class PreviousFourPixelsPredictor : Predictor
    {
        public int Predict(Bitmap image, int x, int y)
        {
            int currentPixel = BWImageHelper.GetBWPixel(image, x, y);
            int leftDiff = BWImageHelper.GetBWPixel(image, x - 1, y);
            int upperDiff = BWImageHelper.GetBWPixel(image, x, y - 1);
            int leftUpperDiff = BWImageHelper.GetBWPixel(image, x - 1, y - 1);
            int predicted = leftDiff + upperDiff - leftUpperDiff;
            return predicted;
        }
    }

    class BWImageHelper
    {
        public static int GetBWPixel(Bitmap image, int x, int y)
        {
            if ((x >= 0) && (x < image.Width) && (y >= 0) && (y < image.Height))
            {
                return (image.GetPixel(x, y).GetBrightness() < 0.5f) ? 0 : 1;
            }
            else
            {
                return 0;
            }
        }

        public static void SetBWPixel(Bitmap image, int x, int y, int bwIntensity)
        {
            if ((x >= 0) && (x < image.Width) && (y >= 0) && (y < image.Height))
            {
                int intensity = bwIntensity > 0 ? 255 : 0;
                image.SetPixel(x, y, Color.FromArgb(intensity, intensity, intensity));
            }
        }

        public static void BWPixel(Bitmap image, int x, int y, int bwIntensity)
        {
            if ((x >= 0) && (x < image.Width) && (y >= 0) && (y < image.Height))
            {
                int intensity = bwIntensity > 0 ? 255 : 0;
                image.SetPixel(x, y, Color.FromArgb(intensity, intensity, intensity));
            }
        }

    }

}
