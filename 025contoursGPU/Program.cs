﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Forms;

namespace _025contours
{
    static class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        [STAThread]
        static void Main()
        {
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            using (IsoContoursGpu example = new IsoContoursGpu())
            {
                example.Run(30.0);
            }
        }
    }
}
