using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Drawing;
using VolumeData;
using System.Diagnostics;

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
            Bitmap image = SliceCollector.RenderGlowingFog(dataSet, null);
            sw.Stop();
            Console.WriteLine("Rendered volume in {0} ms", sw.ElapsedMilliseconds);
            image.Save(@"headCT_z_sabella.png");
        }
    }
}
