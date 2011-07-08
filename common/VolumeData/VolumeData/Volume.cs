using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace VolumeData
{
    /// <summary>
    /// Represents a 3D volumetric data or 4D data set in time.
    /// </summary>
    /// <typeparam name="T">
    /// Type of the voxel values. It can be (signed or unsinged 8- to 64-bit
    /// integer or a floating point number): Byte, UInt16, UInt32, UInt64,
    /// SByte, Int16, Int32, Int64, Single, (32-bit float), Double
    /// (64-bit float).
    /// </typeparam>
    public class VolumeDataSet
    {
        /// <summary>
        /// Dimension of the data (excluding time).
        /// </summary>
        public int Dimension { get; private set; }

        /// <summary>
        /// Size of the data.
        /// [xSize, ySize, zSize].
        /// </summary>
        public int[] Size { get; private set; }

        /// <summary>
        /// Number of time slices.
        /// </summary>
        public int TimeSize { get; private set; }

        /// <summary>
        /// Number of channels of a voxel value.
        /// Allowed values: >= 1.
        /// </summary>
        public int ChannelCount { get; private set; }

        /// <summary>
        /// Bits per single voxel value channel.
        /// Allowed: [8, 16, 32, 64].
        /// </summary>
        public int BitsPerChannel { get; private set; }

        /// <summary>
        /// Type of a signle channel voxel value.
        /// </summary>
        public Type VoxelValueType { get; private set; }

        /// <summary>
        /// Physical dimensions of a single voxel.
        /// [x, y, z] size (in mm).
        /// </summary>
        public float[] VoxelSize { get; private set; }

        /// <summary>
        /// Physical size between time slices (in seconds).
        /// </summary>
        public float VoxelTimeSize { get; private set; }

        /// <summary>
        /// Volume values. [t].
        /// </summary>
        public VolumeGrid[] TimeSlices { get; private set; }

        private static readonly int[] AllowedChannelBits = new[] { 8, 16, 32, 64 };

        private static readonly TypeSpec[] AllowedTypeSpecs = new[] {
            new TypeSpec(typeof(Byte), 8, NumberType.UnsingedInteger ),
            new TypeSpec(typeof(UInt16), 16, NumberType.UnsingedInteger ),
            new TypeSpec(typeof(UInt32), 32, NumberType.UnsingedInteger),
            new TypeSpec(typeof(UInt64), 64, NumberType.UnsingedInteger),
            new TypeSpec(typeof(SByte), 8, NumberType.SignedInteger),
            new TypeSpec(typeof(Int16), 16, NumberType.SignedInteger),
            new TypeSpec(typeof(Int32), 32, NumberType.SignedInteger),
            new TypeSpec(typeof(Int64), 64, NumberType.SignedInteger),
            new TypeSpec(typeof(Single), 32, NumberType.FloatingPoint),
            new TypeSpec(typeof(Double), 64, NumberType.FloatingPoint),
        };

        public VolumeDataSet(
           int dimension,
           int[] size,
           int timeSize,
           int channelCount,
           int bitsPerChannel,
           Type voxelValueType,
           float[] voxelSize,
           float voxelTimeSize)
            : this(dimension, size, timeSize, channelCount, bitsPerChannel,
            voxelValueType, voxelSize, voxelTimeSize, true)
        {
        }

        public VolumeDataSet(
            int dimension,
            int[] size,
            int timeSize,
            int channelCount,
            int bitsPerChannel,
            Type voxelValueType,
            float[] voxelSize,
            float voxelTimeSize,
            bool allocateData)
        {
            #region Input parameters check
            if ((dimension < 1) || (dimension > 4))
            {
                throw new ArgumentException("dimension");
            }
            if (size == null)
            {
                throw new ArgumentNullException("size");
            }
            if (size.Length != dimension)
            {
                throw new ArgumentException("size");
            }
            if (channelCount < 1)
            {
                throw new ArgumentException("channelCount");
            }
            if (!(AllowedChannelBits.Contains(bitsPerChannel)))
            {
                throw new ArgumentException("bitsPerChannel");
            }
            var AllowedTypes = from spec in AllowedTypeSpecs
                               select spec.Type;
            if (!(AllowedTypes.Contains(voxelValueType)))
            {
                throw new ArgumentException("voxelValueType");
            }
            if (voxelSize == null)
            {
                throw new ArgumentNullException("voxelSize");
            }
            if (voxelSize.Length != dimension)
            {
                throw new ArgumentException("size");
            }
            #endregion

            Dimension = dimension;
            Size = new[] { 1, 1, 1 }; // at least one slice in each dimension
            Array.Copy(size, Size, size.Length); // size can be smaller (eg. 1D or 2D)
            TimeSize = timeSize;
            ChannelCount = channelCount;
            BitsPerChannel = bitsPerChannel;
            VoxelValueType = voxelValueType;
            VoxelSize = new float[] { 1, 1, 1 };
            Array.Copy(voxelSize, VoxelSize, voxelSize.Length);
            VoxelTimeSize = voxelTimeSize;

            TimeSlices = new VolumeGrid[TimeSize];

            if (allocateData)
            {
                for (int i = 0; i < TimeSize; i++)
                {
                    TimeSlices[i] = new VolumeGrid(new[] { Size[0], Size[1], Size[2] }, channelCount);
                }
            }
        }

        public static VolumeDataSet LoadFromFile(string headFile, string rawFile)
        {
            return LoadFromFile(headFile, rawFile, false);
        }

        public static VolumeDataSet LoadFromFile(string headFile, string rawFile, bool readOnlyHeader)
        {
            VolumeDataSet dataSet = LoadDataSetHeader(headFile, readOnlyHeader);

            if (!readOnlyHeader)
            {
                bool gzipped = rawFile.EndsWith(".gz");
                using (FileStream fs = new FileStream(rawFile, FileMode.Open))
                {
                    Stream stream = new BufferedStream(fs, 32 * 1024);
                    if (gzipped)
                    {
                        stream = new GZipStream(stream, CompressionMode.Decompress);
                    }
                    using (BinaryReader reader = new BinaryReader(stream))
                    {
                        var valueReadFunc = GetReaderFuncToFloat(dataSet.VoxelValueType, reader);
                        for (int time = 0; time < dataSet.TimeSize; time++)
                        {
                            VolumeGrid.LoadFromReader(reader, valueReadFunc,
                                ref dataSet.TimeSlices[time]);
                        }
                    }
                }
            }

            return dataSet;
        }

        ///// <summary>
        ///// Load the volue data set metadata and load volume data into a texture buffer.
        ///// </summary>
        ///// <remarks>
        ///// Load first first time slice.
        ///// </remarks>
        ///// <param name="headFile"></param>
        ///// <param name="rawFile"></param>
        ///// <param name="readOnlyHeader"></param>
        ///// <param name="textureData"></param>
        ///// <returns></returns>
        //public static VolumeDataSet LoadFromFileToTexture3d(
        //    string headFile, string rawFile, out IntPtr textureData, float scale)
        //{
        //    VolumeDataSet dataSet = LoadDataSetHeader(headFile, true);

        //    if (dataSet.VoxelValueType != typeof(UInt16))
        //    {
        //        throw new NotImplementedException();
        //    }

        //    int width = dataSet.Size[0];
        //    int height = dataSet.Size[1];
        //    int depth = dataSet.Size[2];
        //    int channels = dataSet.ChannelCount;
        //    textureData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(float)) * width * height * depth * channels);

        //    bool gzipped = rawFile.EndsWith(".gz");
        //    using (FileStream fs = new FileStream(rawFile, FileMode.Open))
        //    {
        //        Stream stream = new BufferedStream(fs, 32 * 1024);
        //        if (gzipped)
        //        {
        //            stream = new GZipStream(stream, CompressionMode.Decompress);
        //        }
        //        using (BinaryReader reader = new BinaryReader(stream))
        //        {
        //            unsafe
        //            {
        //                float* volumeDataPtr = (float*)textureData;
        //                int index = 0;
        //                for (int z = 0; z < depth; z++)
        //                    for (int y = 0; y < height; y++)
        //                        for (int x = 0; x < width; x++)
        //                            for (int channel = 0; channel < channels; channel++)
        //                            {
        //                                volumeDataPtr[index] = (float)reader.ReadUInt16() * scale;
        //                                index++;
        //                            }
        //            }
        //        }
        //    }
        //    return dataSet;
        //}

        private static VolumeDataSet LoadDataSetHeader(string headFile, bool readOnlyHeader)
        {
            VolumeDataSet dataSet;
            using (StreamReader sr = new StreamReader(headFile))
            {
                if (sr.ReadLine() != "VHEADER")
                {
                    throw new OutOfMemoryException("header");
                }

                string line = sr.ReadLine();
                string[] lineParts = line.Split(new[] { ' ' });
                int dimension = Int32.Parse(lineParts[0]);

                if (lineParts.Length < dimension + 1)
                {
                    throw new OutOfMemoryException("size");
                }
                int[] size = (from dimStr in lineParts.Skip(1).Take(dimension)
                              select Int32.Parse(dimStr)).ToArray();
                int timeSize = 1;
                if (dimension == 4)
                {
                    // fourth item is time
                    timeSize = size[3];
                    size = size.Take(3).ToArray();
                }

                line = sr.ReadLine();
                lineParts = line.Split(new[] { ' ' }, dimension);
                if (lineParts.Length != dimension)
                {
                    throw new OutOfMemoryException("size");
                }
                float[] voxelSize = (from dimStr in lineParts.Take(dimension)
                                     select Single.Parse(dimStr,
                                     CultureInfo.InvariantCulture.NumberFormat)).ToArray();
                float voxelTimeSize = 1.0f;
                if (dimension == 4)
                {
                    // fourth item is time
                    voxelTimeSize = voxelSize[3];
                    voxelSize = voxelSize.Take(3).ToArray();
                }

                line = sr.ReadLine();
                lineParts = line.Split(new[] { ' ' }, 3);
                if (lineParts.Length != 3)
                {
                    throw new OutOfMemoryException("channels, type or bitsPerChannel");
                }

                int channelCount = Int32.Parse(lineParts[0]);
                string typeSpec = lineParts[1];
                int bitsPerChannel = Int32.Parse(lineParts[2]);
                Type voxelValueType = GetVoxelType(typeSpec, bitsPerChannel);
                dataSet = new VolumeDataSet(
                dimension, size, timeSize, channelCount, bitsPerChannel,
                voxelValueType, voxelSize, voxelTimeSize, !readOnlyHeader);
            }
            return dataSet;
        }

        private static Type GetVoxelType(string typeSpec, int bitsPerChannel)
        {
            NumberType numberType;
            switch (typeSpec)
            {
                case "i": numberType = NumberType.UnsingedInteger; break;
                case "s": numberType = NumberType.SignedInteger; break;
                case "f": numberType = NumberType.FloatingPoint; break;
                default:
                    throw new OutOfMemoryException("type");
            }
            var possibleTypes = from type in AllowedTypeSpecs
                                where (type.Bits == bitsPerChannel)
                                && (type.NumberType == numberType)
                                select type.Type;
            if (possibleTypes.Count() != 1)
            {
                throw new OutOfMemoryException("type");
            }
            return possibleTypes.ElementAt(0);
        }

        public static Func<BinaryReader, float> GetReaderFuncToFloat(Type inputType, BinaryReader reader)
        {
            switch (inputType.Name)
            {
                case "Byte": return (r) => (float)r.ReadByte();
                case "SByte": return (r) => (float)r.ReadSByte();
                case "UInt16": return (r) => (float)r.ReadUInt16();
                case "Int16": return (r) => (float)r.ReadInt16();
                case "UInt32": return (r) => (float)r.ReadUInt32();
                case "Int32": return (r) => (float)r.ReadInt32();
                case "UInt64": return (r) => (float)r.ReadUInt64();
                case "Int64": return (r) => (float)r.ReadInt64();
                case "Single": return (r) => (float)r.ReadSingle();
                case "Double": return (r) => (float)r.ReadDouble();
                default: throw new NotImplementedException();
            }
        }

        public class VolumeGrid
        {
            /// <summary>
            /// Volume 3D table, possibly with vector values.
            /// </summary>
            public float[] Values { get; set; }
            public int Width { get; private set; }
            public int Height { get; private set; }
            public int Depth { get; private set; }
            public int ChannelCount { get; private set; }

            public float this[int x, int y, int z, int channel]
            {
                get
                {
                    return Values[GetLinearIndex(x, y, z, channel)];
                }
                set
                {
                    Values[GetLinearIndex(x, y, z, channel)] = value;
                }
            }

            public int GetLinearIndex(int x, int y, int z, int channel)
            {
                //return channel + x * ChannelCount + y * ChannelCount * Width
                //    + z * ChannelCount * Width * Height;
                return ((z * Height + y) * Width + x) * ChannelCount + channel;
            }

            public VolumeGrid(int[] size, int channelCount)
            {
                Width = size[0];
                Height = size[1];
                Depth = size[2];
                ChannelCount = channelCount;
                //Stopwatch sw = Stopwatch.StartNew();
                Values = new float[Width * Height * Depth * channelCount];
                //sw.Stop();
                //Console.WriteLine("allocated {0}x{1}x{2}x{3} doubles in {4} ms",
                //    Width, Height, Depth, channelCount, sw.ElapsedMilliseconds);
            }

            public static VolumeGrid LoadFromReader(BinaryReader reader, Func<BinaryReader, float> readFunc, ref VolumeGrid volumeGrid)
            {
                var values = volumeGrid.Values;
                int depth = volumeGrid.Depth;
                int height = volumeGrid.Height;
                int width = volumeGrid.Width;
                int channelCount = volumeGrid.ChannelCount;
                int index = 0;
                for (int z = 0; z < depth; z++)
                    for (int y = 0; y < height; y++)
                        for (int x = 0; x < width; x++)
                            for (int channel = 0; channel < channelCount; channel++)
                            {
                                values[index] = readFunc(reader);
                                index++;
                            }
                return volumeGrid;
            }

            public Bitmap DepthSliceToBitmap(int z, Func<float[], Color> colorFunc)
            {
                Bitmap bitmap = new Bitmap(Width, Height);
                int index = z * Width * Height * ChannelCount;
                for (int y = 0; y < Height; y++)
                {
                    for (int x = 0; x < Width; x++)
                    {
                        float[] value = new float[ChannelCount];
                        for (int channel = 0; channel < ChannelCount; channel++)
                        {
                            value[channel] = Values[index];
                            index++;
                        }
                        bitmap.SetPixel(x, y, colorFunc(value));
                    }
                }
                return bitmap;
            }
        }

        private class TypeSpec
        {
            public Type Type { get; set; }
            public int Bits { get; set; }
            public NumberType NumberType { get; set; }

            public TypeSpec(Type type, int bits, NumberType numberType)
            {
                Type = type;
                Bits = bits;
                NumberType = numberType;
            }
        }

        private enum NumberType
        {
            SignedInteger,
            UnsingedInteger,
            FloatingPoint,
        }
    }
}
