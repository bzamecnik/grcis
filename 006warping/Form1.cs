﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace _006warping
{
  public partial class Form1 : Form
  {
    protected Image inputImage = null;

    public Form1 ()
    {
      InitializeComponent();
      this.KeyDown += new KeyEventHandler(pictureResult.KeyPressed);
    }

    private void buttonOpen_Click ( object sender, EventArgs e )
    {
      OpenFileDialog ofd = new OpenFileDialog();

      ofd.Title = "Open Image File";
      ofd.Filter = "Bitmap Files|*.bmp" +
          "|Gif Files|*.gif" +
          "|JPEG Files|*.jpg" +
          "|PNG Files|*.png" +
          "|TIFF Files|*.tif" +
          "|All image types|*.bmp;*.gif;*.jpg;*.png;*.tif";

      ofd.FilterIndex = 6;
      ofd.FileName = "";
      if ( ofd.ShowDialog() != DialogResult.OK )
        return;

      inputImage = Image.FromFile( ofd.FileName );

      computeImage();
    }

    private void computeImage ()
    {
      if ( inputImage == null ) return;
      pictureResult.SetPicture( (Bitmap)inputImage );
    }

    private void recompute()
    {
        if (inputImage == null)
        {
            return;
        }
        pictureResult.Invalidate();
    }

    private void buttonSave_Click ( object sender, EventArgs e )
    {
      if ( inputImage == null ) return;

      SaveFileDialog sfd = new SaveFileDialog();
      sfd.Title = "Save PNG file";
      sfd.Filter = "PNG Files|*.png";
      sfd.AddExtension = true;
      sfd.FileName = "";
      if ( sfd.ShowDialog() != DialogResult.OK )
        return;

      pictureResult.GetPicture().Save( sfd.FileName, System.Drawing.Imaging.ImageFormat.Png );
    }

    private void numericParam_ValueChanged ( object sender, EventArgs e )
    {
        pictureResult.MaxDistance = (double)numericParam.Value;
        recompute();
    }

    private void checkBox1_CheckedChanged(object sender, EventArgs e)
    {
        pictureResult.DrawFeatures = drawFeaturesCheckBox.Checked;
        recompute();
    }
  }
}
