using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.Linq;
using System.Windows.Forms;
using Raster;

namespace _025contours
{
    public partial class IsoContoursCpuForm : Form
    {
        /// <summary>
        /// Initialize the function-selection combo-box.
        /// </summary>
        protected void InitializeFunctions()
        {
            functions = new List<Func<double, double, double>>();

            // 0:
            functions.Add((double x, double y) => Math.Sin(0.1 * x) + Math.Cos(0.1 * y));
            comboFunction.Items.Add("Waves 0");
            // 1:
            functions.Add((double x, double y) =>
            {
                double r = 0.1 * Math.Sqrt(x * x + y * y);
                return (r <= Double.Epsilon) ? 10.0 : (10.0 * Math.Sin(r) / r);
            });
            comboFunction.Items.Add("Drop 0");

            comboFunction.SelectedIndex = 0;
            f = functions[0];

            // threshold set
            for (double d = -10.0; d <= 10.0; d += 0.2)
                thr.Add(d);
        }

        /// <summary>
        /// Draw contour field to the given Bitmap
        /// using class members: origin, scale, valueDrift, thr
        /// </summary>
        protected void ComputeContours(Bitmap image)
        {
            if (f == null) return;

            int width = image.Width;
            int height = image.Height;

            // size of one pixel at the target scale
            double pixelSize = scale;

            BitmapData data = image.LockBits(new Rectangle(0, 0, width, height),
                System.Drawing.Imaging.ImageLockMode.WriteOnly,
                PixelFormat.Format24bppRgb);

            double[] values = new double[4];
            double[] thresholds = thr.ToArray();

            unsafe
            {
                for (int y = 0; y < height; y++)
                {
                    byte* row = (byte*)data.Scan0 + y * data.Stride;
                    double dy = (y - origin.Y) * scale;
                    for (int x = 0; x < width; x++)
                    {
                        double dx = (x - origin.X) * scale;

                        values[0] = f(dx + 0.5 * pixelSize, dy) + valueDrift;
                        values[1] = f(dx, dy + 0.5 * pixelSize) + valueDrift;
                        values[2] = f(dx + pixelSize, dy + 0.5 * pixelSize) + valueDrift;
                        values[3] = f(dx + 0.5 * pixelSize, dy + pixelSize) + valueDrift;
                        double minValue = values.Min();
                        double maxValue = values.Max();

                        bool isIsoLine = false;
                        //foreach (double threshold in thresholds)
                        //{
                        double threshold = 0.0;
                        isIsoLine = (minValue < threshold) && (maxValue >= threshold);
                        //    if (isIsoLine)
                        //    {
                        //        break;
                        //    }
                        //}

                        Color color;
                        if (isIsoLine)
                        {
                            color = Color.Black;
                        }
                        else
                        {
                            double value = f(dx, dy) + valueDrift;
                            color = Draw.ColorRamp(value * 0.1 + 0.5);
                        }
                        int colorArgb = color.ToArgb();
                        row[x * 3] = (byte)(colorArgb & 0xff); // B
                        row[x * 3 + 1] = (byte)((colorArgb >> 8) & 0xff); // G
                        row[x * 3 + 2] = (byte)((colorArgb >> 16) & 0xff); // R
                    }
                }
                image.UnlockBits(data);
            }
        }

        protected void DrawOriginalFunction(Bitmap image)
        {
            if (f == null) return;

            int width = image.Width;
            int height = image.Height;

            for (int y = 0; y < height; y++)
            {
                double dy = (y - origin.Y) * scale;
                for (int x = 0; x < width; x++)
                {
                    double dx = (x - origin.X) * scale;
                    double val = f(dx, dy) + valueDrift;
                    image.SetPixel(x, y, Draw.ColorRamp(val * 0.1 + 0.5));
                }
            }
        }
    }
}
