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
        Neighborhood neighborhood = new EightPixelNeighborhood();
        //Neighborhood neighborhood = new FourNextPixelsNeighborhood();

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

            int dominantColor = computeDominantColor(inputImage);

            Bitmap copyImage = new Bitmap(inputImage);

            // !!!{{ TODO: add the encoding code here

            DeflateStream ds = new BufferedDeflateStream(16384, outputStream, CompressionMode.Compress, true);

            PredictionNeighborhood predictionNeighborhood = new PredictionNeighborhood(neighborhood);

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
                ds.WriteByte((byte)(dominantColor & 0x01));

                // debug counters:
                int totalPixels = width * height;
                Console.WriteLine("Total pixels: {0}, [{1}, {2}]", totalPixels, width, height);

                int totalLinePixels = getTotalLinePixels(inputImage, dominantColor);
                Console.WriteLine("Total line pixels: {0} ({1} %)", totalLinePixels, 100.0 * totalLinePixels / totalPixels);
                int firstLinePixels = 0;
                int neighborhoodLinePixels = 0;

                int longestLine = 0;

                List<int> lineDirections = new List<int>();

                Console.WriteLine("Encoding.");

                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        int bwIntensity = BWImageHelper.GetBWPixel(copyImage, x, y);
                        if (IsBackground(dominantColor, bwIntensity))
                        {
                            continue;
                        }

                        // now we have found the first unprocessed non-background pixel
                        //Console.WriteLine("First pixel: [{0}, {1}]", x, y);
                        BWImageHelper.SetBWPixel(copyImage, x, y, dominantColor);
                        // write position of line's starting pixel
                        // X and Y as two-byte numbers
                        ds.WriteByte((byte)((x >> 8) & 0xff));
                        ds.WriteByte((byte)(x & 0xff));
                        ds.WriteByte((byte)((y >> 8) & 0xff));
                        ds.WriteByte((byte)(y & 0xff));

                        firstLinePixels++;

                        int currentX = x;
                        int currentY = y;
                        bool lineContinues = true;
                        int lineLength = 1;
                        int maxLineDirections = 1 << 16;
                        while (lineContinues && (lineLength < maxLineDirections))
                        //while (lineContinues)
                        {
                            lineContinues = false;
                            int directionNumber = 0;
                            foreach (Point direction in predictionNeighborhood.Directions)
                            {
                                int nextX = currentX + direction.X;
                                int nextY = currentY + direction.Y;
                                if (!BWImageHelper.IsInside(copyImage, nextX, nextY))
                                {
                                    break;
                                }
                                int nextIntensity = BWImageHelper.GetBWPixel(copyImage, nextX, nextY);
                                if (!IsBackground(dominantColor, nextIntensity))
                                {
                                    currentX = nextX;
                                    currentY = nextY;
                                    //Console.WriteLine("Neighbor pixel: [{0}, {1}]", nextX, nextY);
                                    //Console.WriteLine("  Direction: {0} ({1})", direction.Offset, directionNumber);
                                    lineDirections.Add(directionNumber);
                                    lineContinues = true;
                                    neighborhoodLinePixels++;
                                    lineLength++;
                                    predictionNeighborhood.SetLastDirection(direction);
                                    BWImageHelper.SetBWPixel(copyImage, nextX, nextY, dominantColor);
                                    break;
                                }
                                directionNumber++;
                            }
                        }

                        // write the number of following directions
                        int lineDirectionsCount = lineLength - 1;
                        ds.WriteByte((byte)((lineDirectionsCount >> 8) & 0xff));
                        ds.WriteByte((byte)(lineDirectionsCount & 0xff));

                        // write the list of following directions
                        int buffer = 0;
                        int bufferLength = 0;
                        foreach (int direction in lineDirections)
                        {
                            buffer = (buffer << neighborhood.SignificantBits) + direction;
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
                Console.WriteLine("OK: {0}", totalLinePixels == (firstLinePixels + neighborhoodLinePixels));
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

        private static bool IsBackground(int dominantColor, int bwIntensity)
        {
            return (bwIntensity ^ dominantColor) == 0;
        }

        public Bitmap DecodeImage(Stream inps)
        {
            if (inps == null) return null;

            // !!!{{ TODO: add the decoding code here

            PredictionNeighborhood predictionNeighborhood = new PredictionNeighborhood(neighborhood);

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

                // read dominant color
                int dominantColor = ds.ReadByte();
                if (dominantColor < 0) return null;
                int lineColor = 1 - dominantColor;

                decodedImage = new Bitmap(width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb);

                // fill the image with the background color
                Color backgroundColor = (dominantColor == 0) ? Color.Black : Color.White;
                for (int y = 0; y < height; y++)
                {
                    for (int x = 0; x < width; x++)
                    {
                        decodedImage.SetPixel(x, y, backgroundColor);
                    }
                }

                Console.WriteLine("Decoding.");

                // compute bit mask for getting direction bits
                int directionBitMask = 0;
                for (int i = 0; i < neighborhood.SignificantBits; i++)
                {
                    directionBitMask += 1 << i;
                }

                bool canProcessLines = true;
                while (canProcessLines) {
                    //read starting pixel position - X, Y
                    int startX = ds.ReadByte();
                    if (startX < 0)
                    {
                        canProcessLines = false;
                        break;
                    }

                    buffer = ds.ReadByte();
                    if (buffer < 0) return null;
                    startX = (startX << 8) + buffer;

                    int startY = ds.ReadByte();
                    if (startY < 0) return null;
                    buffer = ds.ReadByte();
                    if (buffer < 0) return null;
                    startY = (startY << 8) + buffer;

                    //draw the pixel
                    //Console.WriteLine("First pixel: [{0}, {1}]", startX, startY);
                    //BWImageHelper.SetBWPixel(decodedImage, startX, startY, lineColor);
                    decodedImage.SetPixel(startX, startY, Color.Red);

                    //read the count of following directions
                    int directionsCount = ds.ReadByte();
                    if (directionsCount < 0) return null;
                    buffer = ds.ReadByte();
                    if (buffer < 0) return null;
                    directionsCount = (directionsCount << 8) + buffer;

                    int directionsRead = 0;
                    int bufferLength = 0;
                    int nextX = startX;
                    int nextY = startY;
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

                        //read the direction ordinal
                        int maskOffset = bufferLength - neighborhood.SignificantBits;
                        int directionIndex = (buffer & (directionBitMask << maskOffset)) >> maskOffset;
                        bufferLength -= neighborhood.SignificantBits;
                        //convert the directions ordinal to a Point
                        Point direction = predictionNeighborhood.Directions[directionIndex];
                        predictionNeighborhood.SetLastDirection(direction);
                        //compute the next pixel and draw it
                        nextX += direction.X;
                        nextY += direction.Y;
                        //Console.WriteLine("Neighbor pixel: [{0}, {1}]", nextX, nextY);
                        //Console.WriteLine("  Direction: {0} ({1})", direction, ordinal);
                        //BWImageHelper.SetBWPixel(decodedImage, nextX, nextY, lineColor);
                        decodedImage.SetPixel(nextX, nextY, Color.Green);
                        directionsRead++;
                    }
                }

                //int bufLen = 0;
                //for (int y = 0; y < height; y++)
                //{
                //    for (int x = 0; x < width; x++)
                //    {
                //        if (bufLen == 0)
                //        {
                //            buffer = ds.ReadByte();
                //            if (buffer < 0) return null;
                //            bufLen = 8;
                //        }
                //        // get the leftmost bit from the byte - the predictor error value
                //        int readValue = (buffer & 0x80) >> 7;
                //        //int predictedIntensity = Predictor.Predict(decodedImage, x, y);
                //        //int errorValue = readValue;
                //        //int decodedBWIntensity = predictedIntensity ^ readValue;
                //        int decodedBWIntensity = readValue;
                //        BWImageHelper.SetBWPixel(decodedImage, x, y, decodedBWIntensity);
                //        // shift the buffer to the right by 1 bit
                //        buffer += buffer;
                //        bufLen--;
                //    }
                //    bufLen = 0;
                //}
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

    abstract class Neighborhood
    {
        public abstract IEnumerable<Direction> Values { get; }
        public abstract int SignificantBits { get; }

        public abstract Direction DirectionFromOrdinal(int ordinal);
    }

    class PredictionNeighborhood {
        public List<Point> Directions { get; private set; }

        public PredictionNeighborhood(Neighborhood neighborhood) {
            Directions = new List<Point>();
            // initialize the list of directions with the default order
            foreach (Direction direction in neighborhood.Values)
            {
                Directions.Add(direction.Offset);
            }
        }

        public Point GetLastDirection()
        {
            return Directions[0];
        }

        public void SetLastDirection(Point direction)
        {
            // move the given direction to the beginning of the list
            // assume the given direction is in the list
            int index = Directions.IndexOf(direction);
            Directions.RemoveAt(index);
            Directions.Insert(0, direction);
        }
    }

    class Direction
    {
        public Point Offset { get; set; }
        public int Ordinal { get; set; }

        public Direction(int ordinal, Point offset)
        {
            Offset = offset;
            Ordinal = ordinal;
        }
    }

    class FourNextPixelsNeighborhood : Neighborhood
    {
        public override int SignificantBits { get { return 2; } }

        public static readonly Direction RIGHT = new Direction(0, new Point(1, 0));
        public static readonly Direction RIGHT_DOWN = new Direction(1, new Point(1, 1));
        public static readonly Direction DOWN = new Direction(2, new Point(0, 1));
        public static readonly Direction LEFT_DOWN = new Direction(3, new Point(-1, 1));

        public override IEnumerable<Direction> Values
        {
            get
            {
                yield return RIGHT;
                yield return RIGHT_DOWN;
                yield return DOWN;
                yield return LEFT_DOWN;
            }
        }

        public override Direction DirectionFromOrdinal(int ordinal)
        {
            switch (ordinal)
            {
                case 0: return RIGHT;
                case 1: return RIGHT_DOWN;
                case 2: return DOWN;
                case 3: return LEFT_DOWN;
                default: throw new ArgumentException();
            }
        }
    }

    class EightPixelNeighborhood : Neighborhood
    {
        public override int SignificantBits { get { return 3; } }

        public static readonly Direction RIGHT = new Direction(0, new Point(1, 0));
        public static readonly Direction RIGHT_DOWN = new Direction(1, new Point(1, 1));
        public static readonly Direction DOWN = new Direction(2, new Point(0, 1));
        public static readonly Direction LEFT_DOWN = new Direction(3, new Point(-1, 1));
        public static readonly Direction LEFT = new Direction(4, new Point(-1, 0));
        public static readonly Direction LEFT_UP = new Direction(5, new Point(-1, -1));
        public static readonly Direction UP = new Direction(6, new Point(0, -1));
        public static readonly Direction RIGHT_UP = new Direction(7, new Point(1, -1));

        public override IEnumerable<Direction> Values
        {
            get
            {
                yield return RIGHT;
                yield return RIGHT_DOWN;
                yield return DOWN;
                yield return LEFT_DOWN;
                yield return LEFT;
                yield return LEFT_UP;
                yield return UP;
                yield return RIGHT_UP;
            }
        }

        public override Direction DirectionFromOrdinal(int ordinal)
        {
            switch (ordinal)
            {
                case 0: return RIGHT;
                case 1: return RIGHT_DOWN;
                case 2: return DOWN;
                case 3: return LEFT_DOWN;
                case 4: return LEFT;
                case 5: return LEFT_UP;
                case 6: return UP;
                case 7: return RIGHT_UP;
                default: throw new ArgumentException();
            }
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

        public static void BWPixel(Bitmap image, int x, int y, int bwIntensity)
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