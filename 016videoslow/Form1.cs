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
            mcHorizVertNumeric.Value = VideoCodec.DEFAULT_MC_SEARCH_LINE_SIZE;
            mcSquareNumeric.Value = VideoCodec.DEFAULT_MC_SEARCH_SQUARE_SIZE;
            blockTypeVizCheckBox.Checked = VideoCodec.DEFAULT_VISUALIZE_MC_BLOCK_TYPES;
            deflateCheckBox.Checked = VideoCodec.DEFAULT_DEFLATE_COMPRESSION_ENABLED;
            intraFreqNumeric.Value = VideoCodec.DEFAULT_INTRA_FRAME_FREQUENCY;
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
            videoFileName = sfd.FileName;

            encodingBackgroundWorker.RunWorkerAsync();
        }

        private void encodingBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            string imageFileName = String.Format(textInputMask.Text, 0);
            if (!File.Exists(imageFileName))
            {
                return;
            }

            FileStream fs = new FileStream(videoFileName, FileMode.Create);
            Stream outStream;

            StreamWriter log = new StreamWriter(new FileStream("encodelog.txt", FileMode.Create));
            log.WriteLine("Encoding an image sequence into a compressed video file: " + videoFileName);
            Stopwatch watch = new Stopwatch();
            watch.Reset();
            watch.Start();
            long lastWatchTime = 0;
            long totalEncodingTime = 0;

            frameImage = (Bitmap)Image.FromFile(imageFileName);
            lastWatchTime += LogCurrentStopwatchState("Loaded image file " + imageFileName + " in {0} ms.", log, watch, lastWatchTime);

            VideoCodec codec = GetVideoCodec(log);
            outStream = codec.EncodeHeader(fs, frameImage.Width, frameImage.Height, (float)numericFps.Value, frameImage.PixelFormat);
            lastWatchTime += LogCurrentStopwatchState("Encoded header in {0} ms.", log, watch, lastWatchTime);
            int frameIndex = 0;
            do
            {
                codec.EncodeFrame(frameIndex, frameImage, outStream);
                
                long framEncTime = LogCurrentStopwatchState("Encoded frame no. " + frameIndex + " in {0} ms.", log, watch, lastWatchTime);
                totalEncodingTime += framEncTime;
                lastWatchTime += framEncTime;
                string labelText = String.Format("Encoded frames: {0}. Total time: {1} ms.", frameIndex + 1, watch.ElapsedMilliseconds);
                BeginInvoke((Action)delegate
                {
                    codingStatusLabel.Text = labelText;
                });

                frameIndex++;
                imageFileName = String.Format(textInputMask.Text, frameIndex);
                if (!File.Exists(imageFileName)) break;
                frameImage = (Bitmap)Image.FromFile(imageFileName);
                lastWatchTime += LogCurrentStopwatchState("Loaded image file " + imageFileName + " in {0} ms.", log, watch, lastWatchTime);
            } while (true);
            frameImage.Dispose();

            outStream.Close();
            fs.Close();
            log.WriteLine("Finished encoding the seqence of {0} frames.", frameIndex);
            log.WriteLine("Total time: {0} ms. Encoding time: {1} ms, average {2:f} ms / frame.", watch.ElapsedMilliseconds, totalEncodingTime, totalEncodingTime / (double)frameIndex);
            watch.Stop();
            log.Close();
        }

        private VideoCodec GetVideoCodec(StreamWriter log)
        {
            VideoCodec codec = new VideoCodec(log);
            codec.MCSearchLineSize = (int)mcHorizVertNumeric.Value;
            codec.MCSearchSquareSize = (int)mcSquareNumeric.Value;
            codec.VisualizeMCBlockTypes = blockTypeVizCheckBox.Checked;
            codec.DeflateCompressionEnabled = deflateCheckBox.Checked;
            codec.IntraFrameFrequency = (int)intraFreqNumeric.Value;
            return codec;
        }

        private static long LogCurrentStopwatchState(string message, StreamWriter log, Stopwatch watch, long lastWatchTime)
        {
            long currentWatchTime = watch.ElapsedMilliseconds;
            long diff = currentWatchTime - lastWatchTime;
            log.WriteLine(message, diff);
            return diff;
        }

        private void buttonDecode_Click(object sender, EventArgs e)
        {
            decodingBackgroundWorker.RunWorkerAsync();
        }

        private void decodingBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            string imageFileName = String.Format(textOutputMask.Text, 0);
            string dir = Path.GetDirectoryName(imageFileName);
            Directory.CreateDirectory(dir);

            FileStream fs = new FileStream(videoFileName, FileMode.Open);

            if (fs == null)
            {
                return;
            }

            StreamWriter log = new StreamWriter(new FileStream("decodelog.txt", FileMode.Create));
            VideoCodec codec = GetVideoCodec(log);
            log.WriteLine("Decoding an image sequence from a compressed video file: " + videoFileName);
            Stopwatch watch = new Stopwatch();
            watch.Reset();
            watch.Start();
            long lastWatchTime = 0;
            long totalDecodingTime = 0;

            Stream inStream = codec.DecodeHeader(fs);
            lastWatchTime += LogCurrentStopwatchState("Decoded header in {0} ms.", log, watch, lastWatchTime);
            int frameIndex = 0;
            do
            {
                frameImage = codec.DecodeFrame(frameIndex, inStream);
                if (frameImage == null) break;
                long framDecTime = LogCurrentStopwatchState("Decoded frame no. " + frameIndex + " in {0} ms.", log, watch, lastWatchTime);
                totalDecodingTime += framDecTime;
                lastWatchTime += framDecTime;
                imageFileName = String.Format(textOutputMask.Text, frameIndex);
                frameImage.Save(imageFileName, ImageFormat.Png);
                lastWatchTime += LogCurrentStopwatchState("Saved decoded image into file " + imageFileName + " in {0} ms.", log, watch, lastWatchTime);
                string labelText = String.Format("Decoded frames: {0}. Total time: {1} ms.", frameIndex + 1, watch.ElapsedMilliseconds);
                BeginInvoke((Action)delegate
                {
                    codingStatusLabel.Text = labelText;
                });
                frameIndex++;
            }
            while (true);
            //frameImage.Dispose();

            inStream.Close();
            fs.Close();
            log.WriteLine("Finished decoding the seqence of {0} frames.", frameIndex);
            log.WriteLine("Total time: {0} ms. Decoding time: {1} ms, average {2:f} ms / frame.", watch.ElapsedMilliseconds, totalDecodingTime, totalDecodingTime / (double)frameIndex);
            watch.Stop();
            log.Close();
        }

        private void buttonPlay_Click(object sender, EventArgs e)
        {
            if (!playbackBackgroundWorker.IsBusy)
            {
                VideoForm videoForm = new VideoForm();
                playbackBackgroundWorker.RunWorkerAsync(videoForm);
            }
        }

        private void playbackBackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            // TODO: handle situation when the video file is not available
            FileStream fs = new FileStream(videoFileName, FileMode.Open);
            if (fs == null)
            {
                return;
            }

            StreamWriter log = new StreamWriter(new FileStream("playbacklog.txt", FileMode.Create));
            VideoCodec codec = GetVideoCodec(null);

            Stream inStream = codec.DecodeHeader(fs);

            VideoForm videoForm = (VideoForm)e.Argument;
            e.Result = videoForm;

            double playbackFPS = (double)numericFps.Value;
            
            BeginInvoke((Action)delegate
            {
                videoForm.Size = codec.FrameSize;
                videoForm.PlaybackFPS = playbackFPS;
                videoForm.Show();
            });
            
            long expectedFrameDuration = (long)(1000.0 / playbackFPS);
            long lastTime = 0;
            long lastFPSTime = 0;
            Stopwatch sw = new Stopwatch();
            sw.Reset();
            sw.Start();

            int frameIndex = 0;
            do
            {
                lastTime = sw.ElapsedMilliseconds;
                frameImage = codec.DecodeFrame(frameIndex, inStream);
                if (frameImage == null) break;

                long currentTime = sw.ElapsedMilliseconds;
                long decodingTime = currentTime - lastTime;
                long sleepTime = expectedFrameDuration - decodingTime;

                if (playbackBackgroundWorker.CancellationPending)
                {
                    e.Cancel = true;
                    break;
                }

                videoForm.Image = frameImage;

                if ((currentTime - lastFPSTime) > 250)
                {
                    videoForm.DecodingFPS = 1000.0 / (double)decodingTime;
                    lastFPSTime = currentTime;
                }

                if (sleepTime > 0)
                {
                    Thread.Sleep((int)sleepTime);
                }
                frameIndex++;
            }
            while (!playbackBackgroundWorker.CancellationPending);

            sw.Stop();

            inStream.Close();
            fs.Close();
            log.Close();
        }

        private void playbackBackgroundWorker_RunWorkerCompleted(object sender, RunWorkerCompletedEventArgs e)
        {
            VideoForm videoForm = (VideoForm)e.Result;
            BeginInvoke((Action)delegate
            {
                videoForm.Hide();
            });
        }

        public void StopVideoPlayback()
        {
            BeginInvoke((Action)delegate
            {
                playbackBackgroundWorker.CancelAsync();
            });
        }

        private void buttonStop_Click(object sender, EventArgs e)
        {
            StopVideoPlayback();
        }
    }
}
