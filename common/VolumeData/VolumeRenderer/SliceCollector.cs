using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using VolumeData;

namespace VolumeRenderer
{
    public class SliceCollector
    {
        /// <summary>
        /// Make a orthogonal projection of volume slices in z direction.
        /// Accumulate voxel values into output bitmap pixels.
        /// </summary>
        /// <param name="dataSet">Volume data set.</param>
        /// <param name="outputImage">Output image (optionally preallocated).
        /// </param>
        /// <returns></returns>
        public static Bitmap RenderAverage(VolumeDataSet dataSet, Bitmap outputImage)
        {
            int width = dataSet.Size[0];
            int height = dataSet.Size[1];
            int depth = dataSet.Size[2];
            int channels = dataSet.ChannelCount;
            if ((outputImage == null) || (outputImage.Width != width)
                || (outputImage.Height != height))
            {
                outputImage = new Bitmap(width, height);
            }

            double[] buffer = new double[width * height * channels];

            var values = dataSet.TimeSlices[0].Values;
            // accumulate depth slices from back to front
            {
                int depthStep = width * height * channels;
                int depthIndex = (depth - 1) * depthStep;
                for (int z = depth - 1; z >= 0; z--)
                {
                    int index = 0;
                    for (int y = 0; y < height; y++)
                    {
                        for (int x = 0; x < width; x++)
                        {
                            for (int channel = 0; channel < channels; channel++)
                            {
                                buffer[index] += values[depthIndex + index];
                                index++;
                            }
                        }
                    }
                    depthIndex -= depthStep;
                }
            }

            // compute average voxel value
            double depthSlicesInv = 1 / (double)depth;
            {
                int i = 0;
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                        for (int channel = 0; channel < channels; channel++)
                        {
                            buffer[i] *= depthSlicesInv;
                            i++;
                        }
            }

            {
                int i = 0;
                double scaleFactor = 255.0 / 4095.0;
                for (int y = 0; y < height; y++)
                    for (int x = 0; x < width; x++)
                    {
                        double intensity = buffer[i];
                        byte value = (byte)(intensity * scaleFactor);
                        outputImage.SetPixel(x, y, Color.FromArgb(value, value, value));
                        i += channels;
                    }
            }

            return outputImage;
        }

        public static Bitmap RenderMaxIntensityProjection(VolumeDataSet dataSet, Bitmap outputImage)
        {
            int width = dataSet.Size[0];
            int height = dataSet.Size[1];
            int depth = dataSet.Size[2];
            int channels = dataSet.ChannelCount;
            if ((outputImage == null) || (outputImage.Width != width)
                || (outputImage.Height != height))
            {
                outputImage = new Bitmap(width, height);
            }

            var values = dataSet.TimeSlices[0].Values;

            // accumulate depth slices one xy pixel at time

            int xyIndex = 0;
            int zStep = width * height * channels;
            double scaleFactor = 255.0 / 4095.0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int zIndex = 0;
                    double[] zBuffer = new double[depth];
                    for (int z = 0; z < depth; z++)
                    {
                        zBuffer[z] = values[zIndex + xyIndex];
                        zIndex += zStep;
                    }
                    // max intensity projection
                    double intensity = zBuffer.Max();
                    byte value = (byte)(intensity * scaleFactor);
                    outputImage.SetPixel(x, y, Color.FromArgb(value, value, value));
                    xyIndex += channels;
                }
            }

            return outputImage;
        }

        /// <summary>
        /// Sabella method.
        /// </summary>
        /// <param name="dataSet"></param>
        /// <param name="outputImage"></param>
        /// <returns></returns>
        public static Bitmap RenderGlowingFog(VolumeDataSet dataSet, Bitmap outputImage)
        {
            int width = dataSet.Size[0];
            int height = dataSet.Size[1];
            int depth = dataSet.Size[2];
            int channels = dataSet.ChannelCount;
            if ((outputImage == null) || (outputImage.Width != width)
                || (outputImage.Height != height))
            {
                outputImage = new Bitmap(width, height);
            }

            var values = dataSet.TimeSlices[0].Values;

            // accumulate depth slices one xy pixel at time

            // attenuation exponent
            double tau = 3.0;

            int xyIndex = 0;
            int zStep = width * height * channels;
            double thickness = dataSet.VoxelSize[2];
            double scaleFactor = 1 / 4095.0;
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    int zIndex = 0;
                    double maxDensity = double.NegativeInfinity;
                    int maxPosition = 0; // position of max density
                    double attenuation = 1; // accumulator
                    double prevIntensity = 0;
                    double intensity = 0;
                    for (int z = 0; z < depth; z++)
                    {
                        // voxel value scale scaled to [0; 1]
                        double density = values[zIndex + xyIndex] * scaleFactor;
                        if (density >= maxDensity)
                        {
                            maxDensity = density;
                            maxPosition = z;
                        }
                        double sliceIntensity = density * thickness;
                        double sliceAttenuation = Math.Exp(-tau * prevIntensity);
                        attenuation *= sliceAttenuation;
                        intensity += sliceIntensity * attenuation;
                        prevIntensity = sliceIntensity;
                        zIndex += zStep;
                        if (attenuation < 0.01)
                        {
                            break;
                        }
                    }

                    // [depth - 1; 0] -> [0; 1]
                    double maxPositionNormalized = 1 - (maxPosition / (double)depth);
                    intensity = Math.Min(intensity, 1);

                    Color color = HsvToRgbColor(1 - maxDensity, maxPositionNormalized, intensity);
                    outputImage.SetPixel(x, y, color);
                    xyIndex += channels;
                }
            }

            return outputImage;
        }

        /// <summary>
        /// Convert HSV to RGB.
        /// Formua source: Wikipedia.
        /// http://en.wikipedia.org/wiki/HSV_color_space
        /// </summary>
        /// <param name="hue">[0;1]</param>
        /// <param name="saturation">[0;1]</param>
        /// <param name="value">[0;1]</param>
        /// <returns></returns>
        private static Color HsvToRgbColor(double hue, double saturation, double value)
        {
            Debug.Assert(hue >= 0);
            Debug.Assert(hue <= 1);
            Debug.Assert(saturation >= 0);
            Debug.Assert(saturation <= 1);
            Debug.Assert(value >= 0);
            Debug.Assert(value <= 1);

            double chroma = value * saturation;
            double h = 6 * hue;
            double x = chroma * (1 - Math.Abs(h % 2 - 1));
            double r1 = 0;
            double g1 = 0;
            double b1 = 0;
            switch ((int)Math.Floor(h))
            {
                case 0: r1 = chroma; g1 = x; break;
                case 1: r1 = x; g1 = chroma; break;
                case 2: g1 = chroma; b1 = x; break;
                case 3: g1 = x; b1 = chroma; break;
                case 4: r1 = chroma; b1 = x; break;
                case 5: r1 = x; b1 = chroma; break;
                default: break;
            }
            double m = value - chroma;
            double r = r1 + m;
            double g = g1 + m;
            double b = b1 + m;
            return Color.FromArgb((int)(255 * r), (int)(255 * g), (int)(255 * b));
        }
    }
}
