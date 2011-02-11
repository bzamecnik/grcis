using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace _016videoslow
{
    public partial class VideoForm : Form
    {
        public VideoForm()
        {
            InitializeComponent();
        }

        public Image Image
        {
            set {
                Invoke((Action)delegate
                {
                    videoPlaybackPictureBox.Image = value;
                    videoPlaybackPictureBox.Refresh();
                });
            }
        }

        public double PlaybackFPS { get; set; }

        private double decodingFPS = 0.0;
        public double DecodingFPS
        {
            get
            {
                return decodingFPS;
            }
            set
            {
                decodingFPS = value;
                SetTitle();
            }
        }

        private void SetTitle() {
            Invoke((Action)delegate
            {
                Text = String.Format("Video playback. Playback FPS: {0:f}. Decoding FPS: {1:f}.", PlaybackFPS, DecodingFPS);
            });
        }
    }
}
