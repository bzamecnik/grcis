using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using VolumeData;
using System.Diagnostics;
using System.Runtime.InteropServices;

namespace VolumeRenderer
{
    class Program
    {
        static void Main(string[] args)
        {
            Stopwatch sw = Stopwatch.StartNew();
            var dataSet = VolumeDataSet.LoadFromFile(@"..\..\..\headCT.head", @"..\..\..\headCT.raw");
            sw.Stop();
            Console.WriteLine("Loaded volume in {0} ms", sw.ElapsedMilliseconds);
            sw.Reset();
            sw.Start();
            //Bitmap image = SliceCollector.RenderGlowingFog(dataSet, null);
            //Bitmap image = RayCaster.RenderMaxIntensityProjection(dataSet, null);
            Bitmap image = RayCaster.RenderGlowingFog(dataSet, null);
            sw.Stop();
            Console.WriteLine("Rendered volume in {0} ms", sw.ElapsedMilliseconds);
            //image.Save(@"headCT_z_sabella.png");
            //image.Save(@"headCT_raycast_mip.png");
            image.Save(@"headCT_raycast_sabella.png");
        }
    }
}
