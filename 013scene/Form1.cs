﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.IO;
using OpenTK;
using Scene3D;

namespace _013scene
{
  public partial class Form1 : Form
  {
    protected SceneBrep scene = new SceneBrep();

    protected Bitmap outputImage = null;

    public Form1 ()
    {
      InitializeComponent();
    }

    private void buttonOpen_Click ( object sender, EventArgs e )
    {
      OpenFileDialog ofd = new OpenFileDialog();

      ofd.Title = "Open Scene File";
      ofd.Filter = "Wavefront OBJ Files|*.obj" +
          "|All scene types|*.obj";

      ofd.FilterIndex = 1;
      ofd.FileName = "";
      if ( ofd.ShowDialog() != DialogResult.OK )
        return;

      WavefrontObj objReader = new WavefrontObj();
      objReader.MirrorConversion = false;
      StreamReader reader = new StreamReader( new FileStream( ofd.FileName, FileMode.Open ) );
      int faces = objReader.ReadBrep( reader, scene );
      reader.Close();
      scene.BuildCornerTable();
      int errors = scene.CheckCornerTable( null );

      labelFaces.Text = String.Format( "{0} faces, {1} errors", faces, errors );
      redraw();
    }

    private void buttonGenerate_Click ( object sender, EventArgs e )
    {
      Cursor.Current = Cursors.WaitCursor;
      float variant = (float)numericVariant.Value;

      scene.Reset();
      Construction cn = new Construction();
      int faces = cn.AddMesh( scene, Matrix4.Identity, variant );
      scene.BuildCornerTable();
      int errors = scene.CheckCornerTable( null );

      Cursor.Current = Cursors.Default;

      labelFaces.Text = String.Format( "{0} faces, {1} errors", faces, errors );
      redraw();
    }

    private void redraw ()
    {
      if ( scene == null ) return;

      Cursor.Current = Cursors.WaitCursor;

      int width  = panel1.Width;
      int height = panel1.Height;
      outputImage = new Bitmap( width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb );

      Wireframe renderer = new Wireframe();
      renderer.Perspective = checkPerspective.Checked;
      renderer.Azimuth     = (double)numericAzimuth.Value;
      renderer.Elevation   = (double)numericElevation.Value;
      renderer.ViewVolume  = 30.0;
      renderer.Distance    = 10.0;
      renderer.DrawNormals = checkNormals.Checked;
      renderer.Render( outputImage, scene );

      pictureBox1.Image = outputImage;

      Cursor.Current = Cursors.Default;
    }

    private void buttonRedraw_Click ( object sender, EventArgs e )
    {
      redraw();
    }

    private void buttonSave_Click ( object sender, EventArgs e )
    {
      if ( outputImage == null ) return;

      SaveFileDialog sfd = new SaveFileDialog();
      sfd.Title = "Save PNG file";
      sfd.Filter = "PNG Files|*.png";
      sfd.AddExtension = true;
      sfd.FileName = "";
      if ( sfd.ShowDialog() != DialogResult.OK )
        return;

      outputImage.Save( sfd.FileName, System.Drawing.Imaging.ImageFormat.Png );
    }

    private void buttonSaveOBJ_Click ( object sender, EventArgs e )
    {
      if ( scene == null || scene.Triangles < 1 ) return;

      SaveFileDialog sfd = new SaveFileDialog();
      sfd.Title = "Save OBJ file";
      sfd.Filter = "OBJ Files|*.obj";
      sfd.AddExtension = true;
      sfd.FileName = "";
      if ( sfd.ShowDialog() != DialogResult.OK )
        return;

      WavefrontObj objWriter = new WavefrontObj();
      objWriter.MirrorConversion = true;
      StreamWriter writer = new StreamWriter( new FileStream( sfd.FileName, FileMode.Create ) );
      objWriter.WriteBrep( writer, scene );
      writer.Close();
    }
  }
}
