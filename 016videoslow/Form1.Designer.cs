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
        ((System.ComponentModel.ISupportInitialize)(this.numericFps)).BeginInit();
        ((System.ComponentModel.ISupportInitialize)(this.numericDuration)).BeginInit();
        this.SuspendLayout();
        // 
        // buttonDecode
        // 
        this.buttonDecode.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
        this.buttonDecode.Location = new System.Drawing.Point(361, 98);
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
        this.buttonEncode.Location = new System.Drawing.Point(361, 73);
        this.buttonEncode.Name = "buttonEncode";
        this.buttonEncode.Size = new System.Drawing.Size(144, 23);
        this.buttonEncode.TabIndex = 13;
        this.buttonEncode.Text = "Encode";
        this.buttonEncode.UseVisualStyleBackColor = true;
        this.buttonEncode.Click += new System.EventHandler(this.buttonEncode_Click);
        // 
        // textInputMask
        // 
        this.textInputMask.Location = new System.Drawing.Point(91, 75);
        this.textInputMask.Name = "textInputMask";
        this.textInputMask.Size = new System.Drawing.Size(248, 20);
        this.textInputMask.TabIndex = 14;
        this.textInputMask.Text = "input\\frame{0:0000}.png";
        // 
        // textOutputMask
        // 
        this.textOutputMask.Location = new System.Drawing.Point(90, 101);
        this.textOutputMask.Name = "textOutputMask";
        this.textOutputMask.Size = new System.Drawing.Size(249, 20);
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
        this.label2.Location = new System.Drawing.Point(22, 78);
        this.label2.Name = "label2";
        this.label2.Size = new System.Drawing.Size(62, 13);
        this.label2.TabIndex = 18;
        this.label2.Text = "Input mask:";
        // 
        // label3
        // 
        this.label3.AutoSize = true;
        this.label3.Location = new System.Drawing.Point(14, 104);
        this.label3.Name = "label3";
        this.label3.Size = new System.Drawing.Size(70, 13);
        this.label3.TabIndex = 19;
        this.label3.Text = "Output mask:";
        // 
        // buttonCapture
        // 
        this.buttonCapture.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
        this.buttonCapture.Location = new System.Drawing.Point(361, 9);
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
        this.labelSpeed.Location = new System.Drawing.Point(14, 43);
        this.labelSpeed.Name = "labelSpeed";
        this.labelSpeed.Size = new System.Drawing.Size(79, 13);
        this.labelSpeed.TabIndex = 23;
        this.labelSpeed.Text = "Capture speed:";
        // 
        // label5
        // 
        this.label5.AutoSize = true;
        this.label5.Location = new System.Drawing.Point(44, 135);
        this.label5.Name = "label5";
        this.label5.Size = new System.Drawing.Size(40, 13);
        this.label5.TabIndex = 24;
        this.label5.Text = "Status:";
        // 
        // codingStatusLabel
        // 
        this.codingStatusLabel.AutoSize = true;
        this.codingStatusLabel.Location = new System.Drawing.Point(87, 135);
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
        // Form1
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(517, 162);
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
        this.Controls.Add(this.buttonDecode);
        this.MinimumSize = new System.Drawing.Size(500, 150);
        this.Name = "Form1";
        this.Text = "016 video compression";
        ((System.ComponentModel.ISupportInitialize)(this.numericFps)).EndInit();
        ((System.ComponentModel.ISupportInitialize)(this.numericDuration)).EndInit();
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
  }
}

