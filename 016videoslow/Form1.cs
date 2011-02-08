using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using System.Threading;
using System.IO;
using ScreenShot;

namespace _016videoslow
{
    public partial class Form1 : Form
    {
        protected Bitmap frameImage = null;

        protected string videoFileName = "video.bin";

        public Form1()
        {
            InitializeComponent();
        }

        private void buttonCapture_Click(object sender, EventArgs e)
        {
            this.WindowState = FormWindowState.Minimized;
            ScreenCapture sc = new ScreenCapture();
            string fn = String.Format(textInputMask.Text, 0);
            string dir = Path.GetDirectoryName(fn);
            Directory.CreateDirectory(dir);
            dir += "\\capturelog.txt";
            StreamWriter log = new StreamWriter(new FileStream(dir, FileMode.Create));

            Image im;
            Stopwatch sw = new Stopwatch();
            Thread.Sleep(200);

            int frameNo = 0;
            long msCurrent;       // time of the next capture in milliseconds
            long msTotal = (long)((double)numericDuration.Value * 1000.0);

            sw.Reset();
            sw.Start();
            do
            {
                im = sc.CaptureScreen();
                fn = String.Format(textInputMask.Text, frameNo++);
                im.Save(fn, ImageFormat.Png);
                msCurrent = (long)(frameNo * 1000.0 / (double)numericFps.Value);
                long msSleep = msCurrent - sw.ElapsedMilliseconds;
                if (msSleep > 0)
                {
                    log.WriteLine("Frame {0:0000}: sleeping {1} ms", frameNo, msSleep);
                    Thread.Sleep((int)msSleep);
                }
                else
                    log.WriteLine("Frame {0:0000}: busy! ({1} ms)", frameNo, msSleep);
            }
            while (sw.ElapsedMilliseconds < msTotal);
            labelSpeed.Text = String.Format("Captured {0} frames in {1:f} s!", frameNo, (float)(sw.ElapsedMilliseconds * 0.001));
            log.Close();
            sw.Stop();

            this.WindowState = FormWindowState.Normal;
        }

        private void buttonEncode_Click(object sender, EventArgs e)
        {
            SaveFileDialog sfd = new SaveFileDialog();
            sfd.Title = "Save Video File";
            sfd.Filter = "BIN Files|*.bin";
            sfd.AddExtension = true;
            sfd.FileName = videoFileName;
            if (sfd.ShowDialog() != DialogResult.OK)
                return;

            FileStream fs = new FileStream(videoFileName = sfd.FileName, FileMode.Create);
            Stream outStream;

            string imageFileName = String.Format(textInputMask.Text, 0);
            if (!File.Exists(imageFileName))
            {
                return;
            }

            StreamWriter log = new StreamWriter(new FileStream("encodelog.txt", FileMode.Create));
            log.WriteLine("Encoding an image sequence into a compressed video file: " + videoFileName);
            Stopwatch watch = new Stopwatch();
            watch.Reset();
            watch.Start();
            long lastWatchTime = 0;

            frameImage = (Bitmap)Image.FromFile(imageFileName);
            lastWatchTime = LogCurrentStopwatchState("Loaded image file " + imageFileName + " in {0} ms.", log, watch, lastWatchTime);

            VideoCodec codec = new VideoCodec();
            outStream = codec.EncodeHeader(frameImage.Width, frameImage.Height, (float)numericFps.Value, fs);
            lastWatchTime = LogCurrentStopwatchState("Encoded header in {0} ms.", log, watch, lastWatchTime);
            int frameIndex = 0;
            do
            {
                codec.EncodeFrame(frameIndex, frameImage, outStream);
                lastWatchTime = LogCurrentStopwatchState("Encoded frame no. " + frameIndex + " in {0} ms.", log, watch, lastWatchTime);

                imageFileName = String.Format(textInputMask.Text, ++frameIndex);
                if (!File.Exists(imageFileName)) break;
                frameImage = (Bitmap)Image.FromFile(imageFileName);
                lastWatchTime = LogCurrentStopwatchState("Loaded image file " + imageFileName + " in {0} ms.", log, watch, lastWatchTime);
            } while (true);

            outStream.Close();
            fs.Close();
            log.WriteLine("Finished encoding the seqence. Total time: {0} ms.", watch.ElapsedMilliseconds);
            watch.Stop();
            log.Close();
        }

        private static long LogCurrentStopwatchState(string message, StreamWriter log, Stopwatch watch, long lastWatchTime)
        {
            long currentWatchTime = watch.ElapsedMilliseconds;
            log.WriteLine(message, currentWatchTime - lastWatchTime);
            return currentWatchTime;
        }

        private void buttonDecode_Click(object sender, EventArgs e)
        {
            string imageFileName = String.Format(textOutputMask.Text, 0);
            string dir = Path.GetDirectoryName(imageFileName);
            Directory.CreateDirectory(dir);

            FileStream fs = new FileStream(videoFileName, FileMode.Open);
            
            if (fs == null)
            {
                return;
            }

            VideoCodec codec = new VideoCodec();
            StreamWriter log = new StreamWriter(new FileStream("decodelog.txt", FileMode.Create));
            log.WriteLine("Decoding an image sequence from a compressed video file: " + videoFileName);
            Stopwatch watch = new Stopwatch();
            watch.Reset();
            watch.Start();
            long lastWatchTime = 0;

            Stream inStream = codec.DecodeHeader(fs);
            lastWatchTime = LogCurrentStopwatchState("Decoded header in {0} ms.", log, watch, lastWatchTime);
            int frameIndex = 0;
            do
            {
                frameImage = codec.DecodeFrame(frameIndex, inStream);
                if (frameImage == null) break;
                lastWatchTime = LogCurrentStopwatchState("Decoded frame no. " + frameIndex + " in {0} ms.", log, watch, lastWatchTime);
                imageFileName = String.Format(textOutputMask.Text, frameIndex++);
                frameImage.Save(imageFileName, ImageFormat.Png);
                lastWatchTime = LogCurrentStopwatchState("Saved decoded image into file " + imageFileName + " in {0} ms.", log, watch, lastWatchTime);
            }
            while (true);

            inStream.Close();
            fs.Close();
            log.WriteLine("Finished decoding the seqence. Total time: {0} ms.", watch.ElapsedMilliseconds);
            watch.Stop();
            log.Close();
        }
    }
}
