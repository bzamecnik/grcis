namespace _016videoslow
{
  partial class Form1
  {
    /// <summary>
    /// Required designer variable.
    /// </summary>
    private System.ComponentModel.IContainer components = null;

    /// <summary>
    /// Clean up any resources being used.
    /// </summary>
    /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
    protected override void Dispose ( bool disposing )
    {
      if ( disposing && (components != null) )
      {
        components.Dispose();
      }
      base.Dispose( disposing );
    }

    #region Windows Form Designer generated code

    /// <summary>
    /// Required method for Designer support - do not modify
    /// the contents of this method with the code editor.
    /// </summary>
    private void InitializeComponent ()
    {
        this.buttonDecode = new System.Windows.Forms.Button();
        this.buttonEncode = new System.Windows.Forms.Button();
        this.textInputMask = new System.Windows.Forms.TextBox();
        this.textOutputMask = new System.Windows.Forms.TextBox();
        this.label1 = new System.Windows.Forms.Label();
        this.numericFps = new System.Windows.Forms.NumericUpDown();
        this.label2 = new System.Windows.Forms.Label();
        this.label3 = new System.Windows.Forms.Label();
        this.buttonCapture = new System.Windows.Forms.Button();
        this.label4 = new System.Windows.Forms.Label();
        this.numericDuration = new System.Windows.Forms.NumericUpDown();
        this.labelSpeed = new System.Windows.Forms.Label();
        this.label5 = new System.Windows.Forms.Label();
        this.codingStatusLabel = new System.Windows.Forms.Label();
        this.encodingBackgroundWorker = new System.ComponentModel.BackgroundWorker();
        this.decodingBackgroundWorker = new System.ComponentModel.BackgroundWorker();
        this.buttonPlay = new System.Windows.Forms.Button();
        this.playbackBackgroundWorker = new System.ComponentModel.BackgroundWorker();
        this.buttonStop = new System.Windows.Forms.Button();
        this.groupBox1 = new System.Windows.Forms.GroupBox();
        this.deflateCheckBox = new System.Windows.Forms.CheckBox();
        this.mcSquareNumeric = new System.Windows.Forms.NumericUpDown();
        this.mcHorizVertNumeric = new System.Windows.Forms.NumericUpDown();
        this.label8 = new System.Windows.Forms.Label();
        this.label7 = new System.Windows.Forms.Label();
        this.label6 = new System.Windows.Forms.Label();
        this.blockTypeVizCheckBox = new System.Windows.Forms.CheckBox();
        this.label9 = new System.Windows.Forms.Label();
        this.intraFreqNumeric = new System.Windows.Forms.NumericUpDown();
        ((System.ComponentModel.ISupportInitialize)(this.numericFps)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.numericDuration)).BeginInit();
        this.groupBox1.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.mcSquareNumeric)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.mcHorizVertNumeric)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.intraFreqNumeric)).BeginInit();
        this.SuspendLayout();
        // 
        // buttonDecode
        // 
        this.buttonDecode.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
        this.buttonDecode.Location = new System.Drawing.Point(361, 294);
        this.buttonDecode.Name = "buttonDecode";
        this.buttonDecode.Size = new System.Drawing.Size(144, 23);
        this.buttonDecode.TabIndex = 5;
        this.buttonDecode.Text = "Decode";
        this.buttonDecode.UseVisualStyleBackColor = true;
        this.buttonDecode.Click += new System.EventHandler(this.buttonDecode_Click);
        // 
        // buttonEncode
        // 
        this.buttonEncode.Anchor = System.Windows.Forms.AnchorStyles.Right;
        this.buttonEncode.Location = new System.Drawing.Point(361, 194);
        this.buttonEncode.Name = "buttonEncode";
        this.buttonEncode.Size = new System.Drawing.Size(144, 23);
        this.buttonEncode.TabIndex = 13;
        this.buttonEncode.Text = "Encode";
        this.buttonEncode.UseVisualStyleBackColor = true;
        this.buttonEncode.Click += new System.EventHandler(this.buttonEncode_Click);
        // 
        // textInputMask
        // 
        this.textInputMask.Location = new System.Drawing.Point(95, 75);
        this.textInputMask.Name = "textInputMask";
        this.textInputMask.Size = new System.Drawing.Size(244, 20);
        this.textInputMask.TabIndex = 14;
        this.textInputMask.Text = "input\\frame{0:0000}.png";
        // 
        // textOutputMask
        // 
        this.textOutputMask.Location = new System.Drawing.Point(95, 253);
        this.textOutputMask.Name = "textOutputMask";
        this.textOutputMask.Size = new System.Drawing.Size(244, 20);
        this.textOutputMask.TabIndex = 15;
        this.textOutputMask.Text = "output\\frame{0:0000}.png";
        // 
        // label1
        // 
        this.label1.AutoSize = true;
        this.label1.Location = new System.Drawing.Point(14, 14);
        this.label1.Name = "label1";
        this.label1.Size = new System.Drawing.Size(27, 13);
        this.label1.TabIndex = 16;
        this.label1.Text = "Fps:";
        // 
        // numericFps
        // 
        this.numericFps.DecimalPlaces = 1;
        this.numericFps.Location = new System.Drawing.Point(47, 12);
        this.numericFps.Maximum = new decimal(new int[] {
            60,
            0,
            0,
            0});
        this.numericFps.Minimum = new decimal(new int[] {
            2,
            0,
            0,
            65536});
        this.numericFps.Name = "numericFps";
        this.numericFps.Size = new System.Drawing.Size(57, 20);
        this.numericFps.TabIndex = 17;
        this.numericFps.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
        // 
        // label2
        // 
        this.label2.AutoSize = true;
        this.label2.Location = new System.Drawing.Point(19, 78);
        this.label2.Name = "label2";
        this.label2.Size = new System.Drawing.Size(62, 13);
        this.label2.TabIndex = 18;
        this.label2.Text = "Input mask:";
        // 
        // label3
        // 
        this.label3.AutoSize = true;
        this.label3.Location = new System.Drawing.Point(19, 253);
        this.label3.Name = "label3";
        this.label3.Size = new System.Drawing.Size(70, 13);
        this.label3.TabIndex = 19;
        this.label3.Text = "Output mask:";
        // 
        // buttonCapture
        // 
        this.buttonCapture.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.buttonCapture.Location = new System.Drawing.Point(361, 41);
        this.buttonCapture.Name = "buttonCapture";
        this.buttonCapture.Size = new System.Drawing.Size(144, 23);
        this.buttonCapture.TabIndex = 20;
        this.buttonCapture.Text = "Screen capture";
        this.buttonCapture.UseVisualStyleBackColor = true;
        this.buttonCapture.Click += new System.EventHandler(this.buttonCapture_Click);
        // 
        // label4
        // 
        this.label4.AutoSize = true;
        this.label4.Location = new System.Drawing.Point(110, 14);
        this.label4.Name = "label4";
        this.label4.Size = new System.Drawing.Size(50, 13);
        this.label4.TabIndex = 21;
        this.label4.Text = "Duration:";
        // 
        // numericDuration
        // 
        this.numericDuration.DecimalPlaces = 1;
        this.numericDuration.Location = new System.Drawing.Point(166, 12);
        this.numericDuration.Maximum = new decimal(new int[] {
            1000,
            0,
            0,
            0});
        this.numericDuration.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
        this.numericDuration.Name = "numericDuration";
        this.numericDuration.Size = new System.Drawing.Size(85, 20);
        this.numericDuration.TabIndex = 22;
        this.numericDuration.Value = new decimal(new int[] {
            10,
            0,
            0,
            0});
        // 
        // labelSpeed
        // 
        this.labelSpeed.AutoSize = true;
        this.labelSpeed.Location = new System.Drawing.Point(19, 46);
        this.labelSpeed.Name = "labelSpeed";
        this.labelSpeed.Size = new System.Drawing.Size(79, 13);
        this.labelSpeed.TabIndex = 23;
        this.labelSpeed.Text = "Capture speed:";
        // 
        // label5
        // 
        this.label5.AutoSize = true;
        this.label5.Location = new System.Drawing.Point(12, 310);
        this.label5.Name = "label5";
        this.label5.Size = new System.Drawing.Size(40, 13);
        this.label5.TabIndex = 24;
        this.label5.Text = "Status:";
        // 
        // codingStatusLabel
        // 
        this.codingStatusLabel.AutoSize = true;
        this.codingStatusLabel.Location = new System.Drawing.Point(87, 320);
        this.codingStatusLabel.Name = "codingStatusLabel";
        this.codingStatusLabel.Size = new System.Drawing.Size(0, 13);
        this.codingStatusLabel.TabIndex = 25;
        // 
        // encodingBackgroundWorker
        // 
        this.encodingBackgroundWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.encodingBackgroundWorker_DoWork);
        // 
        // decodingBackgroundWorker
        // 
        this.decodingBackgroundWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.decodingBackgroundWorker_DoWork);
        // 
        // buttonPlay
        // 
        this.buttonPlay.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
        this.buttonPlay.Location = new System.Drawing.Point(195, 363);
        this.buttonPlay.Name = "buttonPlay";
        this.buttonPlay.Size = new System.Drawing.Size(144, 23);
        this.buttonPlay.TabIndex = 5;
        this.buttonPlay.Text = "Play";
        this.buttonPlay.UseVisualStyleBackColor = true;
        this.buttonPlay.Click += new System.EventHandler(this.buttonPlay_Click);
        // 
        // playbackBackgroundWorker
        // 
        this.playbackBackgroundWorker.WorkerSupportsCancellation = true;
        this.playbackBackgroundWorker.DoWork += new System.ComponentModel.DoWorkEventHandler(this.playbackBackgroundWorker_DoWork);
        this.playbackBackgroundWorker.RunWorkerCompleted += new System.ComponentModel.RunWorkerCompletedEventHandler(this.playbackBackgroundWorker_RunWorkerCompleted);
        // 
        // buttonStop
        // 
        this.buttonStop.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
        this.buttonStop.Enabled = false;
        this.buttonStop.Location = new System.Drawing.Point(361, 363);
        this.buttonStop.Name = "buttonStop";
        this.buttonStop.Size = new System.Drawing.Size(144, 23);
        this.buttonStop.TabIndex = 5;
        this.buttonStop.Text = "Stop";
        this.buttonStop.UseVisualStyleBackColor = true;
        this.buttonStop.Click += new System.EventHandler(this.buttonStop_Click);
        // 
        // groupBox1
        // 
        this.groupBox1.Controls.Add(this.intraFreqNumeric);
        this.groupBox1.Controls.Add(this.label9);
        this.groupBox1.Controls.Add(this.deflateCheckBox);
        this.groupBox1.Controls.Add(this.mcSquareNumeric);
        this.groupBox1.Controls.Add(this.mcHorizVertNumeric);
        this.groupBox1.Controls.Add(this.label8);
        this.groupBox1.Controls.Add(this.label7);
        this.groupBox1.Controls.Add(this.label6);
        this.groupBox1.Location = new System.Drawing.Point(13, 101);
        this.groupBox1.Name = "groupBox1";
        this.groupBox1.Size = new System.Drawing.Size(326, 146);
        this.groupBox1.TabIndex = 26;
        this.groupBox1.TabStop = false;
        this.groupBox1.Text = "Encoding options";
        // 
        // deflateCheckBox
        // 
        this.deflateCheckBox.AutoSize = true;
        this.deflateCheckBox.Location = new System.Drawing.Point(10, 88);
        this.deflateCheckBox.Name = "deflateCheckBox";
        this.deflateCheckBox.Size = new System.Drawing.Size(164, 17);
        this.deflateCheckBox.TabIndex = 5;
        this.deflateCheckBox.Text = "Use DEFLATE compression?";
        this.deflateCheckBox.UseVisualStyleBackColor = true;
        // 
        // mcSquareNumeric
        // 
        this.mcSquareNumeric.Location = new System.Drawing.Point(146, 62);
        this.mcSquareNumeric.Maximum = new decimal(new int[] {
            256,
            0,
            0,
            0});
        this.mcSquareNumeric.Name = "mcSquareNumeric";
        this.mcSquareNumeric.Size = new System.Drawing.Size(120, 20);
        this.mcSquareNumeric.TabIndex = 4;
        // 
        // mcHorizVertNumeric
        // 
        this.mcHorizVertNumeric.Location = new System.Drawing.Point(146, 36);
        this.mcHorizVertNumeric.Maximum = new decimal(new int[] {
            256,
            0,
            0,
            0});
        this.mcHorizVertNumeric.Name = "mcHorizVertNumeric";
        this.mcHorizVertNumeric.Size = new System.Drawing.Size(120, 20);
        this.mcHorizVertNumeric.TabIndex = 3;
        // 
        // label8
        // 
        this.label8.AutoSize = true;
        this.label8.Location = new System.Drawing.Point(6, 64);
        this.label8.Name = "label8";
        this.label8.Size = new System.Drawing.Size(44, 13);
        this.label8.TabIndex = 2;
        this.label8.Text = "Square:";
        // 
        // label7
        // 
        this.label7.AutoSize = true;
        this.label7.Location = new System.Drawing.Point(6, 38);
        this.label7.Name = "label7";
        this.label7.Size = new System.Drawing.Size(134, 13);
        this.label7.TabIndex = 1;
        this.label7.Text = "Horizontal and vertical line:";
        // 
        // label6
        // 
        this.label6.AutoSize = true;
        this.label6.Location = new System.Drawing.Point(7, 20);
        this.label6.Name = "label6";
        this.label6.Size = new System.Drawing.Size(179, 13);
        this.label6.TabIndex = 0;
        this.label6.Text = "Motion compensation vector search:";
        // 
        // blockTypeVizCheckBox
        // 
        this.blockTypeVizCheckBox.AutoSize = true;
        this.blockTypeVizCheckBox.Location = new System.Drawing.Point(23, 279);
        this.blockTypeVizCheckBox.Name = "blockTypeVizCheckBox";
        this.blockTypeVizCheckBox.Size = new System.Drawing.Size(205, 17);
        this.blockTypeVizCheckBox.TabIndex = 28;
        this.blockTypeVizCheckBox.Text = "Save block type visualization images?";
        this.blockTypeVizCheckBox.UseVisualStyleBackColor = true;
        // 
        // label9
        // 
        this.label9.AutoSize = true;
        this.label9.Location = new System.Drawing.Point(7, 108);
        this.label9.Name = "label9";
        this.label9.Size = new System.Drawing.Size(110, 13);
        this.label9.TabIndex = 6;
        this.label9.Text = "Intra frame frequency:";
        // 
        // intraFreqNumeric
        // 
        this.intraFreqNumeric.Location = new System.Drawing.Point(146, 106);
        this.intraFreqNumeric.Maximum = new decimal(new int[] {
            100000,
            0,
            0,
            0});
        this.intraFreqNumeric.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
        this.intraFreqNumeric.Name = "intraFreqNumeric";
        this.intraFreqNumeric.Size = new System.Drawing.Size(120, 20);
        this.intraFreqNumeric.TabIndex = 7;
        this.intraFreqNumeric.Value = new decimal(new int[] {
            1,
            0,
            0,
            0});
        // 
        // Form1
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(517, 405);
        this.Controls.Add(this.blockTypeVizCheckBox);
        this.Controls.Add(this.groupBox1);
        this.Controls.Add(this.codingStatusLabel);
        this.Controls.Add(this.label5);
        this.Controls.Add(this.labelSpeed);
        this.Controls.Add(this.numericDuration);
        this.Controls.Add(this.label4);
        this.Controls.Add(this.buttonCapture);
        this.Controls.Add(this.label3);
        this.Controls.Add(this.label2);
        this.Controls.Add(this.numericFps);
        this.Controls.Add(this.label1);
        this.Controls.Add(this.textOutputMask);
        this.Controls.Add(this.textInputMask);
        this.Controls.Add(this.buttonEncode);
        this.Controls.Add(this.buttonStop);
        this.Controls.Add(this.buttonPlay);
        this.Controls.Add(this.buttonDecode);
        this.MinimumSize = new System.Drawing.Size(500, 150);
        this.Name = "Form1";
        this.Text = "016 video compression";
        ((System.ComponentModel.ISupportInitialize)(this.numericFps)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.numericDuration)).EndInit();
        this.groupBox1.ResumeLayout(false);
        this.groupBox1.PerformLayout();
        ((System.ComponentModel.ISupportInitialize)(this.mcSquareNumeric)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.mcHorizVertNumeric)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.intraFreqNumeric)).EndInit();
        this.ResumeLayout(false);
        this.PerformLayout();

    }

    #endregion

    private System.Windows.Forms.Button buttonDecode;
    private System.Windows.Forms.Button buttonEncode;
    private System.Windows.Forms.TextBox textInputMask;
    private System.Windows.Forms.TextBox textOutputMask;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.NumericUpDown numericFps;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.Label label3;
    private System.Windows.Forms.Button buttonCapture;
    private System.Windows.Forms.Label label4;
    private System.Windows.Forms.NumericUpDown numericDuration;
    private System.Windows.Forms.Label labelSpeed;
    private System.Windows.Forms.Label label5;
    private System.Windows.Forms.Label codingStatusLabel;
    private System.ComponentModel.BackgroundWorker encodingBackgroundWorker;
    private System.ComponentModel.BackgroundWorker decodingBackgroundWorker;
    private System.Windows.Forms.Button buttonPlay;
    private System.ComponentModel.BackgroundWorker playbackBackgroundWorker;
    private System.Windows.Forms.Button buttonStop;
    private System.Windows.Forms.GroupBox groupBox1;
    private System.Windows.Forms.Label label7;
    private System.Windows.Forms.Label label6;
    private System.Windows.Forms.NumericUpDown mcSquareNumeric;
    private System.Windows.Forms.NumericUpDown mcHorizVertNumeric;
    private System.Windows.Forms.Label label8;
    private System.Windows.Forms.CheckBox blockTypeVizCheckBox;
    private System.Windows.Forms.CheckBox deflateCheckBox;
    private System.Windows.Forms.NumericUpDown intraFreqNumeric;
    private System.Windows.Forms.Label label9;
  }
}

