namespace _016videoslow
{
    partial class VideoForm
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.videoPlaybackPictureBox = new System.Windows.Forms.PictureBox();
            ((System.ComponentModel.ISupportInitialize)(this.videoPlaybackPictureBox)).BeginInit();
            this.SuspendLayout();
            // 
            // videoPlaybackPictureBox
            // 
            this.videoPlaybackPictureBox.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                        | System.Windows.Forms.AnchorStyles.Left)
                        | System.Windows.Forms.AnchorStyles.Right)));
            this.videoPlaybackPictureBox.Location = new System.Drawing.Point(0, 0);
            this.videoPlaybackPictureBox.Name = "videoPlaybackPictureBox";
            this.videoPlaybackPictureBox.Size = new System.Drawing.Size(284, 262);
            this.videoPlaybackPictureBox.TabIndex = 0;
            this.videoPlaybackPictureBox.TabStop = false;
            // 
            // VideoForm
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(284, 262);
            this.Controls.Add(this.videoPlaybackPictureBox);
            this.Name = "VideoForm";
            this.Text = "Video playback";
            ((System.ComponentModel.ISupportInitialize)(this.videoPlaybackPictureBox)).EndInit();
            this.ResumeLayout(false);

        }

        #endregion

        private System.Windows.Forms.PictureBox videoPlaybackPictureBox;
    }
}