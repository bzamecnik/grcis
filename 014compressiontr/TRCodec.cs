﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Drawing;
using System.Text;
using System.IO;
using System.IO.Compression;
using Support;

namespace _014compressiontr
{
  class TRCodec
  {
    #region protected data

    protected const uint MAGIC = 0x5d21028a;

    #endregion

    #region constructor

    public TRCodec ()
    {
    }

    #endregion

    #region Codec API

    public void EncodeImage ( Bitmap inp, Stream outs )
    {
      if ( inp  == null ||
           outs == null ) return;

      int width  = inp.Width;
      int height = inp.Height;
      if ( width < 1 || height < 1 ) return;

      // !!!{{ TODO: add the encoding code here

      DeflateStream ds = new BufferedDeflateStream( 16384, outs, CompressionMode.Compress, true );

      // file header: [ MAGIC, width, height ]
      ds.WriteByte( (byte)((MAGIC >> 24) & 0xff) );
      ds.WriteByte( (byte)((MAGIC >> 16) & 0xff) );
      ds.WriteByte( (byte)((MAGIC >>  8) & 0xff) );
      ds.WriteByte( (byte)( MAGIC        & 0xff) );

      ds.WriteByte( (byte)((width >> 8) & 0xff) );
      ds.WriteByte( (byte)( width       & 0xff) );

      ds.WriteByte( (byte)((height >> 8) & 0xff) );
      ds.WriteByte( (byte)( height       & 0xff) );

      for ( int y = 0; y < height; y++ )
        for ( int x = 0; x < width; x++ )
        {
          byte gr = (byte)(inp.GetPixel( x, y ).GetBrightness() * 255.0f);
          ds.WriteByte( gr );
        }

      ds.Close();

      // !!!}}
    }

    public Bitmap DecodeImage ( Stream inps )
    {
      if ( inps == null ) return null;

      // !!!{{ TODO: add the decoding code here

      DeflateStream ds = new DeflateStream( inps, CompressionMode.Decompress, true );

      int buffer;
      buffer = ds.ReadByte();
      if ( buffer < 0 || buffer != ((MAGIC >> 24) & 0xff) ) return null;
      buffer = ds.ReadByte();
      if ( buffer < 0 || buffer != ((MAGIC >> 16) & 0xff) ) return null;
      buffer = ds.ReadByte();
      if ( buffer < 0 || buffer != ((MAGIC >>  8) & 0xff) ) return null;
      buffer = ds.ReadByte();
      if ( buffer < 0 || buffer != ( MAGIC        & 0xff) ) return null;

      int width, height;
      width = ds.ReadByte();
      if ( width < 0 ) return null;
      buffer = ds.ReadByte();
      if ( buffer < 0 ) return null;
      width = (width << 8) + buffer;
      height = ds.ReadByte();
      if ( height < 0 ) return null;
      buffer = ds.ReadByte();
      if ( buffer < 0 ) return null;
      height = (height << 8) + buffer;

      if ( width < 1 || height < 1 )
        return null;

      Bitmap result = new Bitmap( width, height, System.Drawing.Imaging.PixelFormat.Format24bppRgb );

      for ( int y = 0; y < height; y++ )
        for ( int x = 0; x < width; x++ )
        {
          int gr = ds.ReadByte();
          result.SetPixel( x, y, Color.FromArgb( gr, gr, gr ) );
        }

      ds.Close();
      return result;

      // !!!}}
    }

    #endregion

  }

}
