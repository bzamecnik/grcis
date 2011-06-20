using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;

namespace VolumeData
{
    class VolumeTest
    {
        private void TestCreate()
        {
            var dataSet = new VolumeDataSet(3, new[] { 10, 20, 30 }, 1, 1, 8, typeof(byte),
                new float[] { 1, 1, 1 }, 1.0f);
            PrintVolumeDataSetInfo(dataSet);

            dataSet = new VolumeDataSet(2, new[] { 10, 20 }, 1, 1, 64, typeof(double),
                new float[] { 2.5f, 3.14f }, 1.0f);
            PrintVolumeDataSetInfo(dataSet);
        }

        private void TestLoadInfo()
        {
            var dataSet = VolumeDataSet.LoadFromFile(@"..\..\..\fullHeadCT.head", @"..\..\..\fullHeadCT.raw.gz", true);
            PrintVolumeDataSetInfo(dataSet);

            //dataSet = null;
            //GC.Collect();

            dataSet = VolumeDataSet.LoadFromFile(@"..\..\..\headCT.head", @"..\..\..\headCT.raw.gz", true);
            PrintVolumeDataSetInfo(dataSet);

            //dataSet = null;
            //GC.Collect();

            dataSet = VolumeDataSet.LoadFromFile(@"..\..\..\vectorField2D.head", @"..\..\..\vectorField2D.raw.gz", true);
            PrintVolumeDataSetInfo(dataSet);
        }

        private void TestLoad2dVectorField()
        {
            var dataSet = VolumeDataSet.LoadFromFile(@"..\..\..\vectorField2D.head", @"..\..\..\vectorField2D.raw.gz");
            PrintVolumeDataSetInfo(dataSet);
            int channels = dataSet.ChannelCount;
            double[] min = new double[channels];
            double[] max = new double[channels];
            for (int i = 0; i < dataSet.ChannelCount; i++)
            {
                FindChannelMinAndMax(dataSet.TimeSlices[0], i, out min[i], out max[i]);
            }
            double[] range = new double[channels];
            for (int i = 0; i < dataSet.ChannelCount; i++)
            {
                range[i] = max[i] - min[i];
            }

            Bitmap image = dataSet.TimeSlices[0].DepthSliceToBitmap(0,
                (value) => Color.FromArgb(
                    (int)(255 * ((value[0] - min[0]) / range[0])),
                    (int)(255 * ((value[1] - min[1]) / range[1])),
                    0));
            image.Save("vectorField2D.png");
        }

        private void TestLoadHead()
        {
            var dataSet = VolumeDataSet.LoadFromFile(@"..\..\..\headCT.head", @"..\..\..\headCT.raw.gz");
            PrintVolumeDataSetInfo(dataSet);

            int z = dataSet.Size[2] / 2; //half z
            Bitmap image = dataSet.TimeSlices[0].DepthSliceToBitmap(z,
                (value) => Color.FromArgb(
                    // 16-bit -> 8-bit
                    (int)(value[0] / 255.0),
                    (int)(value[0] / 255.0),
                    (int)(value[0] / 255.0)));
            image.Save(string.Format("headCT_{0}.png", z));
        }

        private void FindChannelMinAndMax(VolumeDataSet.VolumeGrid grid, int channel,
            out double min, out double max)
        {
            min = double.PositiveInfinity;
            max = double.NegativeInfinity;
            var values = grid.Values;
            for (int z = 0; z < grid.Depth; z++)
                for (int y = 0; y < grid.Height; y++)
                    for (int x = 0; x < grid.Width; x++)
                    {
                        double value = values[x, y, z, channel];
                        min = Math.Min(min, value);
                        max = Math.Max(max, value);
                    }
        }

        private void PrintVolumeDataSetInfo(VolumeDataSet dataSet)
        {
            Console.WriteLine("Dimension: {0}", dataSet.Dimension);
            Console.WriteLine("Size: {0}", string.Join(", ",
                (from size in dataSet.Size select size.ToString()).ToArray()));
            Console.WriteLine("Numer of time slices: {0}", dataSet.TimeSize);
            Console.WriteLine("Number of channels: {0}", dataSet.ChannelCount);
            Console.WriteLine("Bits per channel: {0}", dataSet.BitsPerChannel);
            Console.WriteLine("Voxel value type: {0}", dataSet.VoxelValueType.Name);
            Console.WriteLine("Voxel size: {0}", string.Join(", ",
                (from size in dataSet.VoxelSize select size.ToString()).ToArray()));
            Console.WriteLine("Voxel time size: {0}", dataSet.VoxelTimeSize);
            Console.WriteLine();
        }
    }
}
