﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using System.Diagnostics;
using Raster;

namespace _008kdtree
{
  public partial class Form1 : Form
  {
    public Form1 ()
    {
      InitializeComponent();
    }

    private List<Segment2D> segments = null;

    private Bitmap output = null;

    private KDTree tree = null;

    const int SIZE = 600;

    private void buttonGenerate_Click ( object sender, EventArgs e )
    {
      // Lorentz attractor constants:
      // see http://astronomy.swin.edu.au/~pbourke/fractals/lorenz/
      const double DT    = 0.004;
      const double DELTA = 10.0;
      const double R     = 28.0;
      const double B     = 8.0 / 3.0;

      const double LINE_RADIUS_MIN = 0.3;
      const double LINE_RADIUS_MAX = 1.0;

      // Lorentz attractor variables:
      double lx = 0.1;
      double ly = 0.0;
      double lz = 0.0;
      double dt, dx, dy, dz;

      Cursor.Current = Cursors.WaitCursor;

      // target image:
      if ( checkVisual.Checked )
        output = new Bitmap( SIZE, SIZE, System.Drawing.Imaging.PixelFormat.Format24bppRgb );
      else
        output = null;

      // random number generator:
      int seed = (int)numericSeed.Value;
      Random rnd = (seed == 0) ? new Random() : new Random( seed );

      // set size (number of line segments):
      int size = (int)numericSize.Value;
      segments = new List<Segment2D>( size );

      double x, y, x1, y1, x2, y2;
      for ( int i = 0; i < size; i++ )
      {
        do
        {
          dt = DT * (1.0 + rnd.NextDouble());
          dx = dt * (DELTA * (ly - lx));
          dy = dt * (lx * (R - lz) - ly);
          dz = dt * (lx * ly - B * lz);
          lx += dx;
          ly += dy;
          lz += dz;
          x =  0.5 + 0.04 * lx;
          y = -0.2 + 0.03 * lz;
        } while ( x < 0.0 || x > 1.0 ||
                  y < 0.0 || y > 1.0 );

        x *= SIZE;
        y *= SIZE;
        double rad = LINE_RADIUS_MIN + (LINE_RADIUS_MAX - LINE_RADIUS_MIN) * rnd.NextDouble();
        double a   = rnd.NextDouble() * Math.PI;
        x2 = rad * Math.Sin( a );
        y2 = rad * Math.Cos( a );
        x1 = x - x2;
        y1 = y - y2;
        x2 += x;
        y2 += y;

        // the new line segment is ready:
        segments.Add( new Segment2D( (float)x1, (float)y1, (float)x2, (float)y2 ) );

        // draw the segment eventually:
        if ( output != null )
          Draw.Line( output, (int)x1, (int)y1, (int)x2, (int)y2, Color.Blue );
      }

      // Stopwatch - measuring the real time of KD-tree construction:
      Stopwatch sw = new Stopwatch();
      sw.Start();

      // Build the tree:
      tree = new KDTree();
      tree.BuildTree( segments );

      sw.Stop();
      // Build-phase statistics:
      long segStat, boxStat, heapStat;
      tree.GetStatistics( out segStat, out boxStat, out heapStat );
      labelStat.Text = String.Format( "Elapsed: {0:f} s [{1:D}-{2:D}-{3:D}]",
                                      1.0e-3 * sw.ElapsedMilliseconds,
                                      segStat, boxStat, heapStat );
      if ( output != null )
        pictureBox1.Image = output;

      Cursor.Current = Cursors.Default;
    }

    private void buttonQuery_Click ( object sender, EventArgs e )
    {
      const int NEAREST = 200;

      Cursor.Current = Cursors.WaitCursor;

      // random number generator:
      int seed = (int)numericSeed.Value;
      Random rnd = (seed == 0) ? new Random() : new Random( seed );

      // set size (number of rays):
      int size  = (int)numericQuery.Value;
      bool visual = (output != null) && checkVisual.Checked;

      // hash accumulator:
      long hash = 0L;
      int[] result = new int[ NEAREST ];
      const double colorMul = 1.0 / NEAREST;

      // reset the picture:
      if ( visual )
        foreach ( Segment2D seg in segments )
          Draw.Line( output, (int)seg.x1, (int)seg.y1, (int)seg.x2, (int)seg.y2, Color.Blue );

      // Stopwatch - measuring the real time of KD-tree queries:
      Stopwatch sw = new Stopwatch();
      long segStat, boxStat, heapStat, segCount = 0L;
      tree.GetStatistics( out segStat, out boxStat, out heapStat ); // to be sure
      sw.Start();

      // Query loop:
      for ( int i = 0; i++ < size; )
      {
        float x0 = (float)(1.0 + (SIZE - 2.0) * rnd.NextDouble());
        float y0 = (float)(1.0 + (SIZE - 2.0) * rnd.NextDouble());
        double angle = rnd.NextDouble() * Math.PI * 2.0;
        float dx = (float)Math.Sin( angle );
        float dy = (float)Math.Cos( angle );

        int n = tree.RayIntersect( x0, y0, dx, dy, NEAREST, result );
        if ( n == 0 ) continue;

        // check (and draw) the intersections:
        double last = 0.0;  // distance of the last intersection

        for ( int j = 0; j < n; j++ )
        {
          double dist = KDTree.RaySegment2D( x0, y0, dx, dy,
                                             segments[ result[ j ] ].x1, segments[ result[ j ] ].y1,
                                             segments[ result[ j ] ].x2, segments[ result[ j ] ].y2 );
          if ( dist < last )
            throw new Exception( "Error in result ordering" );
          last = dist;

          // the line segment is valid:
          segCount++;
          hash = hash * 348937L + result[ j ] * 8999L + 4831L;

          // eventually draw the intersected segment in nice color:
          if ( visual )
            Draw.Line( output,
                       (int)segments[ result[ j ] ].x1, (int)segments[ result[ j ] ].y1,
                       (int)segments[ result[ j ] ].x2, (int)segments[ result[ j ] ].y2,
                       Draw.ColorRamp( 1.0 - colorMul * j ) );
        }
      }

      sw.Stop();

      // Query-phase statistics:
      tree.GetStatistics( out segStat, out boxStat, out heapStat );
      labelStat.Text = String.Format( "Elapsed: {0:f} s [{1:D}-{2:D}-{3:D}-{4:D}]",
                                      1.0e-3 * sw.ElapsedMilliseconds,
                                      segCount, segStat, boxStat, heapStat );
      labelHash.Text = String.Format( "{0:X}", hash );

      if ( visual )
        pictureBox1.Invalidate();
      Cursor.Current = Cursors.Default;
    }

    private void buttonSave_Click ( object sender, EventArgs e )
    {
      SaveFileDialog sfd = new SaveFileDialog();
      sfd.Title = "Save PNG file";
      sfd.Filter = "PNG Files|*.png";
      sfd.AddExtension = true;
      sfd.FileName = "";
      if ( sfd.ShowDialog() != DialogResult.OK )
        return;

      pictureBox1.Image.Save( sfd.FileName, System.Drawing.Imaging.ImageFormat.Png );
    }

  }
}
