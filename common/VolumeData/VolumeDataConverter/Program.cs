using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using VolumeData;
using System.Diagnostics;

namespace VolumeDataConverter
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch sw = Stopwatch.StartNew();

            var dataSet = VolumeDataSet.LoadFromFile(@"..\..\..\headCT.head", @"..\..\..\headCT.raw");

            sw.Stop();
            Console.WriteLine("Loaded volume in {0} ms", sw.ElapsedMilliseconds);

            //RenderDepthSlicesToBitmaps(dataSet);
        }

        private static void RenderDepthSlicesToBitmaps(VolumeDataSet dataSet)
        {
            Stopwatch sw = Stopwatch.StartNew();

            double min;
            double max;
            FindChannelMinAndMax(dataSet.TimeSlices[0], 0, out min, out max);
            double range = max - min;

            sw.Start();
            int depth = dataSet.Size[2];
            for (int z = 0; z < depth; z++)
            {
                Bitmap image = dataSet.TimeSlices[0].DepthSliceToBitmap(z,
                (value) => Color.FromArgb(
                    // 16-bit -> 8-bit
                    (int)(255 * ((value[0] - min) / range)),
                    (int)(255 * ((value[0] - min) / range)),
                    (int)(255 * ((value[0] - min) / range))));
                image.Save(string.Format("headCT_{0:D4}.png", z));
            }
            sw.Stop();
            Console.WriteLine("Save {0} slices in {1} ms, {2} ms on average",
                depth, sw.ElapsedMilliseconds, sw.ElapsedMilliseconds / (float)depth);
        }

        private static void FindChannelMinAndMax(VolumeDataSet.VolumeGrid grid, int channel,
            out double min, out double max)
        {
            min = double.PositiveInfinity;
            max = double.NegativeInfinity;
            var values = grid.Values;
            int index = channel;
            for (int z = 0; z < grid.Depth; z++)
                for (int y = 0; y < grid.Height; y++)
                    for (int x = 0; x < grid.Width; x++)
                    {
                        double value = values[index];
                        min = Math.Min(min, value);
                        max = Math.Max(max, value);
                        index += grid.ChannelCount;
                    }
        }
    }
}
