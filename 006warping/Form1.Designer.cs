namespace _006warping
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
        this.buttonOpen = new System.Windows.Forms.Button();
        this.buttonSave = new System.Windows.Forms.Button();
        this.numericParam = new System.Windows.Forms.NumericUpDown();
        this.label1 = new System.Windows.Forms.Label();
        this.richTextBox1 = new System.Windows.Forms.RichTextBox();
        this.label2 = new System.Windows.Forms.Label();
        this.groupBox1 = new System.Windows.Forms.GroupBox();
        this.drawFeaturesCheckBox = new System.Windows.Forms.CheckBox();
        this.pictureResult = new _006warping.GUIPictureBox();
        ((System.ComponentModel.ISupportInitialize)(this.numericParam)).BeginInit();
        this.groupBox1.SuspendLayout();
        ((System.ComponentModel.ISupportInitialize)(this.pictureResult)).BeginInit();
        this.SuspendLayout();
        // 
        // buttonOpen
        // 
        this.buttonOpen.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
        this.buttonOpen.Location = new System.Drawing.Point(570, 370);
        this.buttonOpen.Name = "buttonOpen";
        this.buttonOpen.Size = new System.Drawing.Size(130, 23);
        this.buttonOpen.TabIndex = 1;
        this.buttonOpen.Text = "Load image";
        this.buttonOpen.UseVisualStyleBackColor = true;
        this.buttonOpen.Click += new System.EventHandler(this.buttonOpen_Click);
        // 
        // buttonSave
        // 
        this.buttonSave.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Right)));
        this.buttonSave.Location = new System.Drawing.Point(570, 411);
        this.buttonSave.Name = "buttonSave";
        this.buttonSave.Size = new System.Drawing.Size(130, 23);
        this.buttonSave.TabIndex = 2;
        this.buttonSave.Text = "Save image";
        this.buttonSave.UseVisualStyleBackColor = true;
        this.buttonSave.Click += new System.EventHandler(this.buttonSave_Click);
        // 
        // numericParam
        // 
        this.numericParam.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
        this.numericParam.Increment = new decimal(new int[] {
            1,
            0,
            0,
            65536});
        this.numericParam.Location = new System.Drawing.Point(9, 36);
        this.numericParam.Name = "numericParam";
        this.numericParam.Size = new System.Drawing.Size(79, 20);
        this.numericParam.TabIndex = 3;
        this.numericParam.Value = new decimal(new int[] {
            50,
            0,
            0,
            0});
        this.numericParam.ValueChanged += new System.EventHandler(this.numericParam_ValueChanged);
        // 
        // label1
        // 
        this.label1.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Bottom | System.Windows.Forms.AnchorStyles.Left)));
        this.label1.Location = new System.Drawing.Point(6, 16);
        this.label1.Name = "label1";
        this.label1.Size = new System.Drawing.Size(97, 17);
        this.label1.TabIndex = 4;
        this.label1.Text = "Maximum distance";
        // 
        // richTextBox1
        // 
        this.richTextBox1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
        this.richTextBox1.Enabled = false;
        this.richTextBox1.Location = new System.Drawing.Point(523, 13);
        this.richTextBox1.Name = "richTextBox1";
        this.richTextBox1.Size = new System.Drawing.Size(177, 141);
        this.richTextBox1.TabIndex = 6;
        this.richTextBox1.Text = "Help:\n\nleft mouse + drag - create new feature\n\nActive features:\nright mouse + dra" +
            "g - move\nN - move to next\nP - move to previous";
        // 
        // label2
        // 
        this.label2.AutoSize = true;
        this.label2.Location = new System.Drawing.Point(526, 141);
        this.label2.Name = "label2";
        this.label2.Size = new System.Drawing.Size(0, 13);
        this.label2.TabIndex = 7;
        // 
        // groupBox1
        // 
        this.groupBox1.Controls.Add(this.label1);
        this.groupBox1.Controls.Add(this.numericParam);
        this.groupBox1.Location = new System.Drawing.Point(526, 201);
        this.groupBox1.Name = "groupBox1";
        this.groupBox1.Size = new System.Drawing.Size(174, 100);
        this.groupBox1.TabIndex = 8;
        this.groupBox1.TabStop = false;
        this.groupBox1.Text = "Parameters:";
        // 
        // drawFeaturesCheckBox
        // 
        this.drawFeaturesCheckBox.AutoSize = true;
        this.drawFeaturesCheckBox.Checked = true;
        this.drawFeaturesCheckBox.CheckState = System.Windows.Forms.CheckState.Checked;
        this.drawFeaturesCheckBox.Location = new System.Drawing.Point(526, 168);
        this.drawFeaturesCheckBox.Name = "drawFeaturesCheckBox";
        this.drawFeaturesCheckBox.Size = new System.Drawing.Size(90, 17);
        this.drawFeaturesCheckBox.TabIndex = 10;
        this.drawFeaturesCheckBox.Text = "draw features";
        this.drawFeaturesCheckBox.UseVisualStyleBackColor = true;
        this.drawFeaturesCheckBox.CheckedChanged += new System.EventHandler(this.checkBox1_CheckedChanged);
        // 
        // pictureResult
        // 
        this.pictureResult.ActiveFeatureColor = System.Drawing.Color.Orange;
        this.pictureResult.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom)
                    | System.Windows.Forms.AnchorStyles.Left)
                    | System.Windows.Forms.AnchorStyles.Right)));
        this.pictureResult.DrawFeatures = true;
        this.pictureResult.FeatureColor = System.Drawing.Color.LightGreen;
        this.pictureResult.Location = new System.Drawing.Point(12, 13);
        this.pictureResult.MaxDistance = 50;
        this.pictureResult.Name = "pictureResult";
        this.pictureResult.Size = new System.Drawing.Size(505, 421);
        this.pictureResult.TabIndex = 0;
        this.pictureResult.TabStop = false;
        // 
        // Form1
        // 
        this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
        this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
        this.ClientSize = new System.Drawing.Size(712, 446);
        this.Controls.Add(this.drawFeaturesCheckBox);
        this.Controls.Add(this.groupBox1);
        this.Controls.Add(this.label2);
        this.Controls.Add(this.richTextBox1);
        this.Controls.Add(this.buttonSave);
        this.Controls.Add(this.buttonOpen);
        this.Controls.Add(this.pictureResult);
        this.KeyPreview = true;
        this.MinimumSize = new System.Drawing.Size(620, 200);
        this.Name = "Form1";
        this.Text = "006 drag warping";
        ((System.ComponentModel.ISupportInitialize)(this.numericParam)).EndInit();
        this.groupBox1.ResumeLayout(false);
        ((System.ComponentModel.ISupportInitialize)(this.pictureResult)).EndInit();
        this.ResumeLayout(false);
        this.PerformLayout();

    }

    #endregion

    public GUIPictureBox pictureResult;
    public System.Windows.Forms.Button buttonOpen;
    public System.Windows.Forms.Button buttonSave;
    public System.Windows.Forms.NumericUpDown numericParam;
    private System.Windows.Forms.Label label1;
    private System.Windows.Forms.RichTextBox richTextBox1;
    private System.Windows.Forms.Label label2;
    private System.Windows.Forms.GroupBox groupBox1;
    private System.Windows.Forms.CheckBox drawFeaturesCheckBox;
  }
}
