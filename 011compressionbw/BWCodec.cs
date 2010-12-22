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

        Neighborhood neighborhood = new EightPixelNeighborhood();

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

            int backgroundColor = computeDominantColor(inputImage);

            Bitmap copyImage = new Bitmap(inputImage);

            // !!!{{ TODO: add the encoding code here

            DeflateStream ds = new BufferedDeflateStream(16384, outputStream, CompressionMode.Compress, true);

            try
            {
                // file header: [ MAGIC, width, height, dominant color ]
                ds.WriteByte((byte)((MAGIC >> 24) & 0xff));
                ds.WriteByte((byte)((MAGIC >> 16) & 0xff));
                ds.WriteByte((byte)((MAGIC >> 8) & 0xff));
                ds.WriteByte((byte)(MAGIC & 0xff));

                ds.WriteByte((byte)((width >> 8) & 0xff));
                ds.WriteByte((byte)(width & 0xff));

                ds.WriteByte((byte)((height >> 8) & 0xff));
                ds.WriteByte((byte)(height & 0xff));

                // dominant color
                ds.WriteByte((byte)(backgroundColor & 0x01));

                // debug counters:
                int totalPixels = width * height;
                Console.WriteLine("Total pixels: {0}, [{1}, {2}]", totalPixels, width, height);

                int totalLinePixels = getTotalLinePixels(inputImage, backgroundColor);
                Console.WriteLine("Total line pixels: {0} ({1} %)", totalLinePixels, 100.0 * totalLinePixels / totalPixels);
                int firstLinePixels = 0;
                int neighborhoodLinePixels = 0;

                Point previousStartingPoint = new Point();

                int longestLine = 0;

                List<int> lineDirections = new List<int>();

                //Console.WriteLine("Encoding.");

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int bwIntensity = BWImageHelper.GetBWPixel(copyImage, x, y);
                        if (IsBackground(backgroundColor, bwIntensity))
                        {
                            continue;
                        }

                        // now we have found the first unprocessed non-background pixel
                        //Console.WriteLine("First pixel: [{0}, {1}]", x, y);
                        BWImageHelper.SetBWPixel(copyImage, x, y, backgroundColor);
                        // write position of line's starting pixel
                        // X and Y as two-byte numbers
                        int diffX = x - previousStartingPoint.X;
                        int diffY = y - previousStartingPoint.Y;

                        firstLinePixels++;

                        int currentX = x;
                        int currentY = y;
                        previousStartingPoint.X = x;
                        previousStartingPoint.Y = y;
                        int previousAbsDirectionIndex = 0;
                        bool lineContinues = true;
                        int lineLength = 1;
                        while (lineContinues && (lineLength < 256))
                        //while (lineContinues)
                        {
                            lineContinues = false;
                            for (int relativeDirectionIndex = 0; relativeDirectionIndex < neighborhood.Directions.Count - 1; relativeDirectionIndex++)
                            {
                                // absolute
                                int newAbsDirectionIndex = (previousAbsDirectionIndex + relativeDirectionIndex + neighborhood.Directions.Count - 2) % neighborhood.Directions.Count;
                                Point direction = neighborhood.Directions[newAbsDirectionIndex];
                                int nextX = currentX + direction.X;
                                int nextY = currentY + direction.Y;
                                if (!BWImageHelper.IsInside(copyImage, nextX, nextY))
                                {
                                    continue;
                                }
                                int nextIntensity = BWImageHelper.GetBWPixel(copyImage, nextX, nextY);
                                if (!IsBackground(backgroundColor, nextIntensity))
                                {
                                    currentX = nextX;
                                    currentY = nextY;
                                    //Console.WriteLine("Neighbor pixel: [{0}, {1}]", nextX, nextY);
                                    //Console.WriteLine("  Direction: {0}, rel: {1}, abs: {2}", direction, relativeDirectionIndex, newAbsDirectionIndex);
                                    lineDirections.Add(relativeDirectionIndex);
                                    previousAbsDirectionIndex = newAbsDirectionIndex;
                                    lineContinues = true;
                                    neighborhoodLinePixels++;
                                    lineLength++;
                                    BWImageHelper.SetBWPixel(copyImage, nextX, nextY, backgroundColor);
                                    break;
                                }
                            }
                        }

                        ds.WriteByte((byte)((diffX >> 8) & 0xff));
                        ds.WriteByte((byte)(diffX & 0xff));
                        ds.WriteByte((byte)((diffY >> 8) & 0xff));
                        ds.WriteByte((byte)(diffY & 0xff));

                        // write the number of following directions
                        ds.WriteByte((byte)((lineLength - 1) & 0xff));

                        // write the list of following directions
                        int buffer = 0;
                        int bufferLength = 0;
                        foreach (int relDirectionIndex in lineDirections)
                        {
                            buffer = (buffer << neighborhood.SignificantBits) + relDirectionIndex;
                            bufferLength += neighborhood.SignificantBits;
                            int remainingBits = bufferLength - 8; // free space in the byte
                            if ((bufferLength >= 8) && (remainingBits < neighborhood.SignificantBits))
                            {
                                int writtenByte = (buffer >> remainingBits) & 0xff;
                                ds.WriteByte((byte)(writtenByte & 0xff));
                                buffer -= writtenByte << remainingBits;
                                bufferLength -= 8;
                            }
                        }
                        if (bufferLength > 0)
                        {
                            // shift the remaining bits completely to the left
                            // (so that the rest of the byte is filled with zeros only)
                            buffer <<= 8 - bufferLength;
                            ds.WriteByte((byte)(buffer & 0xff));
                            buffer = 0;
                            bufferLength = 0;
                        }

                        lineDirections.Clear();

                        longestLine = Math.Max(longestLine, lineLength);
                    }
                }

                Console.WriteLine("Total first pixels: {0} ({1} %) ({2} %)", firstLinePixels, 100.0 * firstLinePixels / totalPixels, 100.0 * firstLinePixels / totalLinePixels);
                Console.WriteLine("Total neighborhood pixels: {0} ({1} %) ({2} %)", neighborhoodLinePixels, 100.0 * neighborhoodLinePixels / totalPixels, 100.0 * neighborhoodLinePixels / totalLinePixels);
                Console.WriteLine("First + neighborhood: {0}", firstLinePixels + neighborhoodLinePixels);
                //Console.WriteLine("OK: {0}", totalLinePixels == (firstLinePixels + neighborhoodLinePixels));
                Console.WriteLine("Longest line: {0}", longestLine);
                Console.WriteLine();
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

        private static bool IsBackground(int backgroundColor, int bwIntensity)
        {
            return (bwIntensity ^ backgroundColor) == 0;
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

                // read magic number
                buffer = ds.ReadByte();
                if (buffer < 0 || buffer != ((MAGIC >> 24) & 0xff)) return null;
                buffer = ds.ReadByte();
                if (buffer < 0 || buffer != ((MAGIC >> 16) & 0xff)) return null;
                buffer = ds.ReadByte();
                if (buffer < 0 || buffer != ((MAGIC >> 8) & 0xff)) return null;
                buffer = ds.ReadByte();
                if (buffer < 0 || buffer != (MAGIC & 0xff)) return null;

                // read image width
                int width = ds.ReadByte();
                if (width < 0) return null;
                buffer = ds.ReadByte();
                if (buffer < 0) return null;
                width = (width << 8) + buffer;

                // read image height
                int height = ds.ReadByte();
                if (height < 0) return null;
                buffer = ds.ReadByte();
                if (buffer < 0) return null;
                height = (height << 8) + buffer;

                if (width < 1 || height < 1)
                    return null;

                decodedImage = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                // read dominant color
                int backgroundColor = ds.ReadByte();
                if (backgroundColor < 0) return null;
                int foregroundColor = 1 - backgroundColor;
                // fill the image with the background color
                Color bgDrawingColor = (backgroundColor == 0) ? Color.Black : Color.White;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        decodedImage.SetPixel(x, y, bgDrawingColor);
                    }
                }

                //Console.WriteLine("Decoding.");

                // compute bit mask for getting relativeDirectionIndex bits
                int directionBitMask = 0;
                for (int i = 0; i < neighborhood.SignificantBits; i++)
                {
                    directionBitMask += 1 << i;
                }

                Point previousStartingPoint = new Point();

                bool canProcessLines = true;
                while (canProcessLines) {
                    //read starting pixel position - X, Y
                    int diffX = ds.ReadByte();
                    if (diffX < 0)
                    {
                        canProcessLines = false;
                        break;
                    }

                    buffer = ds.ReadByte();
                    if (buffer < 0) return null;
                    diffX = (diffX << 8) + buffer;

                    int diffY = ds.ReadByte();
                    if (diffY < 0) return null;
                    buffer = ds.ReadByte();
                    if (buffer < 0) return null;
                    diffY = (diffY << 8) + buffer;

                    int startX = previousStartingPoint.X + (short) diffX;
                    int startY = previousStartingPoint.Y + (short) diffY;

                    //draw the pixel
                    //Console.WriteLine("First pixel: [{0}, {1}]", startX, startY);
                    //BWImageHelper.SetBWPixel(decodedImage, startX, startY, foregroundColor);
                    decodedImage.SetPixel(startX, startY, Color.Red);
                    previousStartingPoint.X = startX;
                    previousStartingPoint.Y = startY;

                    //read the count of following directions
                    int directionsCount = ds.ReadByte();
                    if (directionsCount < 0) return null;
                    int directionsRead = 0;
                    int bufferLength = 0;
                    int nextX = startX;
                    int nextY = startY;
                    int previousAbsDirectionIndex = 0;
                    while (directionsRead < directionsCount)
                    {
                        if (bufferLength < neighborhood.SignificantBits)
                        {
                            buffer &= 0xffff >> (16 - bufferLength);
                            buffer <<= 8;
                            buffer += ds.ReadByte();
                            if (buffer < 0) return null;
                            bufferLength += 8;
                        }

                        //read the relativeDirectionIndex relativeDirectionIndex
                        int maskOffset = bufferLength - neighborhood.SignificantBits;
                        int relativeDirectionIndex = (buffer & (directionBitMask << maskOffset)) >> maskOffset;
                        int newAbsDirectionIndex = (relativeDirectionIndex + previousAbsDirectionIndex + neighborhood.Directions.Count - 2) % neighborhood.Directions.Count;
                        bufferLength -= neighborhood.SignificantBits;
                        //convert the directions relativeDirectionIndex to a Point
                        Point direction = neighborhood.Directions[newAbsDirectionIndex];
                        //compute the next pixel and draw it
                        nextX += direction.X;
                        nextY += direction.Y;
                        //Console.WriteLine("Neighbor pixel: [{0}, {1}]", nextX, nextY);
                        //Console.WriteLine("  Direction: {0}, rel: {1}, abs: {2}", direction, relativeDirectionIndex, newAbsDirectionIndex);
                        //BWImageHelper.SetBWPixel(decodedImage, nextX, nextY, foregroundColor);
                        //decodedImage.SetPixel(nextX, nextY, Color.Green);
                        int linePointIntensity = (int)(127.0 * (1.0 - directionsRead / (double)directionsCount));
                        decodedImage.SetPixel(nextX, nextY, Color.FromArgb(0, linePointIntensity, linePointIntensity));
                        directionsRead++;
                        previousAbsDirectionIndex = newAbsDirectionIndex;
                    }
                }
                Console.WriteLine("Successfully decoded.");
                Console.WriteLine();
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

        /// <summary>
        /// Computes dominant color in a given black & white image, that is
        /// the color of the majority of pixels.
        /// 
        /// Assume that black = 0 and white = 1.
        /// </summary>
        /// <param name="image"></param>
        /// <returns>0 if black is dominant, 1 if white is dominant</returns>
        public int computeDominantColor(Bitmap image)
        {
            // the least half of pixels must be white for white to be the
            // dominant color
            int whiteThreshold = image.Width * image.Height / 2;
            int totalIntensity = 0;
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    totalIntensity += BWImageHelper.GetBWPixel(image, x, y);
                    if (totalIntensity > whiteThreshold)
                    {
                        return 1;
                    }
                }
            }
            return 0;
        }

        public int getTotalLinePixels(Bitmap image, int dominantColor)
        {
            int totalLinePixels = 0;
            for (int y = 0; y < image.Height; y++)
            {
                for (int x = 0; x < image.Width; x++)
                {
                    int intensity = BWImageHelper.GetBWPixel(image, x, y);
                    if (intensity != dominantColor)
                    {
                        totalLinePixels++;
                    }
                }
            }
            return totalLinePixels;
        }

        #endregion

    }

    abstract class Neighborhood
    {
        public List<Point> Directions { get; protected set; }
        public int SignificantBits
        {
            get
            {
                return ComputeSignificantBits(Directions.Count);
            }
        }

        public static int ComputeSignificantBits(int number)
        {
            if (number > 0)
            {
                return (int)Math.Ceiling(Math.Log(number, 2.0));
            }
            else
            {
                return 0;
            }
        }
    }

    class EightPixelNeighborhood : Neighborhood
    {
        public static readonly Point RIGHT = new Point(1, 0);
        public static readonly Point RIGHT_DOWN = new Point(1, 1);
        public static readonly Point DOWN = new Point(0, 1);
        public static readonly Point LEFT_DOWN = new Point(-1, 1);
        public static readonly Point LEFT = new Point(-1, 0);
        public static readonly Point LEFT_UP = new Point(-1, -1);
        public static readonly Point UP = new Point(0, -1);
        public static readonly Point RIGHT_UP = new Point(1, -1);

        public EightPixelNeighborhood()
        {
            Directions = new List<Point>() {
                // default clockwise order:
                RIGHT, RIGHT_DOWN, DOWN, LEFT_DOWN,
                LEFT, LEFT_UP, UP, RIGHT_UP
            };
        }
    }

    class BWImageHelper
    {
        public static int GetBWPixel(Bitmap image, int x, int y)
        {
            if (IsInside(image, x, y))
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
            if (IsInside(image, x, y))
            {
                int intensity = bwIntensity > 0 ? 255 : 0;
                image.SetPixel(x, y, Color.FromArgb(intensity, intensity, intensity));
            }
        }

        public static bool IsInside(Bitmap image, int x, int y)
        {
            return ((x >= 0) && (x < image.Width) && (y >= 0) && (y < image.Height));
        }

    }

}