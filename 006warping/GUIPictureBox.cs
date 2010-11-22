﻿using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Data;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace _006warping
{
  [Designer( "System.Windows.Forms.Design.PictureBoxDesigner, System.Design, Version=2.0.0.0, Culture=neutral, PublicKeyToken=b03f5f7f11d50a3a" )]
  public partial class GUIPictureBox : PictureBox
  {
      public GUIPictureBox() {
          features = new Features(this);
          //Features.Add(new Feature{ StartPoint = new Point(100, 70), EndPoint = new Point(50, 120) });
          //Features.Add(new Feature { StartPoint = new Point(50, 75), EndPoint = new Point(150, 200) });
          featurePen = new Pen(FeaturesColor, 2.0f);
          MaxDistance = 50.0f;
      }

    #region data

    /// <summary>
    /// Input image.
    /// </summary>
    private Bitmap input;

    #endregion

    #region warping feature settings
    
    /// <summary>
    ///  Color to paint the features.
    /// </summary>
    protected Color featuresColor = Color.LightGreen;

    /// <summary>
    /// Gets or sets the features' color.
    /// </summary>
    public Color FeaturesColor
    {
      get { return featuresColor; }
      set
      {
        featuresColor = value;
        featurePen.Color = value;
        // !!! TODO: redraw the current features?
      }
    }

    private Pen featurePen;

    Features features;

    public double MaxDistance { get; set; }

    #endregion

    #region set/get pictures

    /// <summary>
    /// Sets a new input picture.
    /// </summary>
    /// <param name="newInput">New input picture</param>
    public void SetPicture ( Bitmap newInput )
    {
      input = newInput;
      Image = (Bitmap)newInput.Clone();
    }

    /// <summary>
    /// Gets the current output picture.
    /// </summary>
    public Bitmap GetPicture ()
    {
      return (Bitmap)Image;
    }

    #endregion

    #region display results

    protected override void OnPaint ( PaintEventArgs e )
    {
        // custom drawing        
        Image = WarpImage(input, e.ClipRectangle);
        base.OnPaint(e);
        PaintFeatures(e.Graphics, features);
    }

    private Bitmap WarpImage(Bitmap inputImage, Rectangle clipRectangle)
    {
        if (inputImage == null)
        {
            return null;
        }

        if (features.Count < 1)
        {
            return inputImage;
        }
        
        Bitmap warpedImage = (Bitmap)inputImage.Clone();
        int maxY = Math.Min(clipRectangle.Bottom, inputImage.Height);
        int maxX = Math.Min(clipRectangle.Right, inputImage.Width);

        for (int y = clipRectangle.Top; y < maxY; y++)
        {
            for (int x = clipRectangle.Left; x < maxX; x++)
            {
                // at least one feature has its influence
                bool isWarped = false;
                double shiftX = 0.0;
                double shiftY = 0.0;
                foreach (Feature feature in features.List)
                {
                    double distX = feature.EndPoint.X - x;
                    double distY = feature.EndPoint.Y - y;
                    double distance = Math.Sqrt(distX * distX + distY * distY);
                    if (distance < MaxDistance)
                    {
                        isWarped = true;
                        double featureWeight = (MaxDistance - distance) / MaxDistance;
                        //double featureWeight = 0.5 + Math.Cos(distance * Math.PI / MaxDistance) * 0.5;
                        shiftX += feature.Direction.Width * featureWeight;
                        shiftY += feature.Direction.Height * featureWeight;
                    }
                }
                
                if (isWarped)
                {
                    double sourceX = x - shiftX;
                    double sourceY = y - shiftY;
                    Color color = Color.Black;
                    if ((sourceX >= 0.0) && (sourceX <= inputImage.Width - 1.0) &&
                        (sourceY >= 0.0) && (sourceY <= inputImage.Height - 1.0))
                    {
                        color = inputImage.GetPixel((int)sourceX, (int)sourceY);
                    }
                    warpedImage.SetPixel(x, y, color);
                }
            }
        }
        return warpedImage;
    }

    private void PaintFeatures(Graphics graphics, Features features) {
        foreach (Feature feature in features.List) {
            graphics.DrawLine(featurePen, feature.StartPoint, feature.EndPoint);
            graphics.FillRectangle(Brushes.Orange, feature.EndPoint.X - 3, feature.EndPoint.Y - 3, 5, 5);
        }
    }

    #endregion

    #region mouse events

    /// <summary>
    /// Stores position of mouse when button was pressed.
    /// </summary>
    protected Point mouseDownPoint;

    /// <summary>
    /// Indicates wheter the right mouse button is down.
    /// </summary>
    protected bool rightButtonDown = false;

    protected override void OnMouseDown ( MouseEventArgs e )
    {
      base.OnMouseDown( e );
      mouseDownPoint = e.Location;
      if ( e.Button == MouseButtons.Right )
        rightButtonDown = true;
    }

    protected override void OnMouseUp ( MouseEventArgs e )
    {
      base.OnMouseUp( e );

      if ( input == null )
        return;

      if ( e.Button == MouseButtons.Right )
        rightButtonDown = false;

      if ( e.Location == mouseDownPoint )
        return;

      UseWaitCursor = true;

      // choose action according to the mouse button
      if ( e.Button == MouseButtons.Left )
      {
        // add new feature
        features.Add(new Feature(mouseDownPoint, e.Location));
        UseWaitCursor = false;
        return;
      }

      if ( e.Button == MouseButtons.Right )
      {
        // !!! TODO: shift the last feature?
          if (features.Count > 0)
          {
              Size shift = new Size(mouseDownPoint.X - e.Location.X, mouseDownPoint.Y - e.Location.Y);
              Feature feature = features.List[features.Count - 1];
              feature.Shift(shift);
              Invalidate(new Rectangle(
                  Math.Min(mouseDownPoint.X, e.Location.X),
                  Math.Min(mouseDownPoint.Y, e.Location.Y),
                  Math.Abs(mouseDownPoint.X - e.Location.X) + 1,
                  Math.Abs(mouseDownPoint.Y - e.Location.Y) + 1));
          }
      }
      UseWaitCursor = false;
   }

    protected override void OnMouseMove ( MouseEventArgs e )
    {
      base.OnMouseMove( e );

      if ( rightButtonDown )
        return;

      // !!! TODO: active contour?
      UseWaitCursor = true;
      Invalidate( new Rectangle( Math.Min( mouseDownPoint.X, e.Location.X ),
                                 Math.Min( mouseDownPoint.Y, e.Location.Y ),
                                 Math.Abs( mouseDownPoint.X - e.Location.X ) + 1,
                                 Math.Abs( mouseDownPoint.Y - e.Location.Y ) + 1 ) );
      UseWaitCursor = false;
    }

    #endregion

    #region keyboard events

    protected override void OnKeyDown ( KeyEventArgs e )
    {
      base.OnKeyDown( e );
      KeyPressed( e.KeyCode );
    }

    public void KeyPressed ( Keys key )
    {
      if ( key == Keys.Back )
      {
        // !!! TODO: remove the last feature?
          if (features.Count > 1)
          {
              Feature removedFeature = features.List[features.Count - 1];
              features.List.RemoveAt(features.Count - 1);
              Invalidate(removedFeature.BoundingBox);
          }
          
      }

      if ( key == Keys.Delete )
      {
        // !!! TODO: remove the current feature?
          if (features.Count > 0)
          {
              Feature removedFeature = features.List[features.Count];
              features.List.RemoveAt(features.Count);
              Invalidate(removedFeature.BoundingBox);
          }
      }
    }

    #endregion
  }


  class Features
  {
      GUIPictureBox guiPictureBox;

      public Features(GUIPictureBox guiPictureBox) {
          this.guiPictureBox = guiPictureBox;
          List = new List<Feature>();
      }

      public List<Feature> List
      {
          get;
          private set;
      }

      public int Count {
          get { return List.Count; }
      }

      public void Add(Feature feature)
      {
          List.Add(feature);
          guiPictureBox.Invalidate(feature.BoundingBox);
      }
  }
  class Feature {
      private Point startPoint;
      public Point StartPoint
      {
          get { return startPoint; }
          set {
              startPoint = value;
              direction = computeDirection();
          }
      }

      private Point endPoint;
      public Point EndPoint
      {
          get { return endPoint; }
          set
          {
              endPoint = value;
              direction = computeDirection();
          }
      }

      private Size computeDirection()
      {
          return new Size (EndPoint.X - StartPoint.X, EndPoint.Y - StartPoint.Y);
      }
      
      private Size direction;
      public Size Direction { get { return direction; } private set { direction = value; } }

      public Rectangle BoundingBox
      {
          get
          {
              return new Rectangle(
                  Math.Min(StartPoint.X, EndPoint.X),
                  Math.Min(StartPoint.Y, EndPoint.Y),
                  Math.Abs(StartPoint.X - EndPoint.X) + 1,
                  Math.Abs(StartPoint.Y - EndPoint.Y) + 1);
          }
      }

      public Feature()
          : this(new Point(0, 0), new Point(0, 0))
      { }

      public Feature(Point start, Point end)
      {
          StartPoint = start;
          EndPoint = end;
      }

      public void Shift(Size shift) {
          StartPoint -= shift;
          EndPoint -= shift;
      }

  }
}