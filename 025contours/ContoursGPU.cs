#region --- License ---
/* Licensed under the MIT/X11 license.
 * Copyright (c) 2006-2008 the OpenTK Team.
 * This notice may not be removed from any source distribution.
 * See license.txt for licensing detailed licensing details.
 * 
 * Written by Christoph Brandtner
 */
#endregion

using System;
using System.Windows.Forms;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace _025contours
{
    public class IsoContoursGpu : GameWindow
    {
        public IsoContoursGpu()
            : base(512, 512)
        {
        }

        #region Private Fields

        // GLSL Objects
        int VertexShaderObject, FragmentShaderObject, ProgramObject;
        int TextureObject;

        // scale factor for XY coordinates
        float UniformScaleFactor = 0.4f;
        // offset in XY window coordinates
        float UniformOffsetX = 100;
        float UniformOffsetY = 100;
        // offset in Z coordinate - function value
        float UniformValueDrift = 0.0f;

        int totalFunctionsCount = 2;
        int UnifromFunctionIndex = 1;

        int screenshotCount = 0;

        // number of frames whose time is averaged
        int renderTimeAveragingFrameCount = 10;
        int frameIndex = 0;
        double renderTimeSum = 0;

        #endregion private Fields

        #region OnLoad

        /// <summary>
        /// Setup OpenGL and load resources here.
        /// </summary>
        /// <param name="e">Not used.</param>
        protected override void OnLoad(EventArgs e)
        {
            // Check for necessary capabilities:
            string version = GL.GetString(StringName.Version);
            int major = (int)version[0];
            int minor = (int)version[2];
            if (major < 2)
            {
                MessageBox.Show("You need at least OpenGL 2.0 to run this example. Aborting.",
                                 "GLSL not supported", MessageBoxButtons.OK, MessageBoxIcon.Exclamation);
                this.Exit();
            }

            this.VSync = VSyncMode.On;

            GL.Disable(EnableCap.Dither);
            GL.ClearColor(0.2f, 0f, 0.4f, 0f);

            string LogInfo;

            #region Shaders

            // Load&Compile Vertex Shader
            using (StreamReader sr = new StreamReader("Shaders/Contour_VS.glsl"))
            {
                VertexShaderObject = GL.CreateShader(ShaderType.VertexShader);
                GL.ShaderSource(VertexShaderObject, sr.ReadToEnd());
                GL.CompileShader(VertexShaderObject);
            }

            GL.GetShaderInfoLog(VertexShaderObject, out LogInfo);
            if (LogInfo.Length > 0 && !LogInfo.Contains("hardware"))
                Trace.WriteLine("Vertex Shader Log:\n" + LogInfo);
            else
                Trace.WriteLine("Vertex Shader compiled without complaint.");


            // Load&Compile Fragment Shader
            using (StreamReader sr = new StreamReader("Shaders/Contour_FS.glsl"))
            {
                FragmentShaderObject = GL.CreateShader(ShaderType.FragmentShader);
                GL.ShaderSource(FragmentShaderObject, sr.ReadToEnd());
                GL.CompileShader(FragmentShaderObject);
            }

            GL.GetShaderInfoLog(FragmentShaderObject, out LogInfo);
            if (LogInfo.Length > 0 && !LogInfo.Contains("hardware"))
                Trace.WriteLine("Fragment Shader Log:\n" + LogInfo);
            else
                Trace.WriteLine("Fragment Shader compiled without complaint.");


            // Link the Shaders to a usable Program
            ProgramObject = GL.CreateProgram();
            GL.AttachShader(ProgramObject, VertexShaderObject);
            GL.AttachShader(ProgramObject, FragmentShaderObject);
            GL.LinkProgram(ProgramObject);

            // make current
            GL.UseProgram(ProgramObject);

            // Flag ShaderObjects for delete when app exits
            GL.DeleteShader(VertexShaderObject);
            GL.DeleteShader(FragmentShaderObject);
            #endregion Shaders

            #region Textures

            //// Load&Bind the 1D texture for color lookups
            //GL.ActiveTexture(TextureUnit.Texture0); // select TMU0
            //GL.GenTextures(1, out TextureObject);
            //GL.TexParameter(TextureTarget.Texture1D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            //GL.TexParameter(TextureTarget.Texture1D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            //GL.TexParameter(TextureTarget.Texture1D, TextureParameterName.TextureWrapS, (int)(TextureWrapMode)All.ClampToEdge);

            //using (Bitmap bitmap = new Bitmap("Data/Textures/ColorTable.bmp"))
            //{
            //    BitmapData data = bitmap.LockBits(new System.Drawing.Rectangle(0, 0, bitmap.Width, bitmap.Height), ImageLockMode.ReadOnly,
            //                                      System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            //    GL.TexImage1D(TextureTarget.Texture1D, 0, PixelInternalFormat.Rgb8, data.Width, 0, OpenTK.Graphics.OpenGL.PixelFormat.Bgr,
            //                  PixelType.UnsignedByte, data.Scan0);
            //    bitmap.UnlockBits(data);
            //}
            #endregion Textures

            Keyboard.KeyUp += KeyUp;
            Keyboard.KeyDown += KeyDown;
        }

        void KeyDown(object sender, KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.W)
            {
                UniformOffsetY += 10;
            }
            else if (e.Key == Key.S)
            {
                UniformOffsetY -= 10;
            }
            else if (e.Key == Key.D)
            {
                UniformOffsetX += 10;
            }
            else if (e.Key == Key.A)
            {
                UniformOffsetX -= 10;
            }
            else if (e.Key == Key.Q)
            {
                UniformValueDrift -= 0.1f;
            }
            else if (e.Key == Key.E)
            {
                UniformValueDrift += 0.1f;
            }
            else if (e.Key == Key.Z)
            {
                UniformScaleFactor *= 0.95f;
            }
            else if (e.Key == Key.X)
            {
                UniformScaleFactor /= 0.95f;
            }
            else if (e.Key == Key.F)
            {
                UnifromFunctionIndex += 1;
                UnifromFunctionIndex %= totalFunctionsCount;
            }
        }

        void KeyUp(object sender, KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.F12)
            {
                SaveScreenshot();
            }
        }

        private void SaveScreenshot()
        {
            Bitmap bmp = new Bitmap(this.Width, this.Height);
            System.Drawing.Imaging.BitmapData data =
                bmp.LockBits(new System.Drawing.Rectangle(0, 0, this.Width, this.Height),
                             System.Drawing.Imaging.ImageLockMode.WriteOnly,
                             System.Drawing.Imaging.PixelFormat.Format24bppRgb);
            GL.ReadPixels(0, 0, this.Width, this.Height,
                          OpenTK.Graphics.OpenGL.PixelFormat.Bgr,
                          OpenTK.Graphics.OpenGL.PixelType.UnsignedByte,
                          data.Scan0);
            bmp.UnlockBits(data);
            bmp.RotateFlip(RotateFlipType.RotateNoneFlipY);
            bmp.Save("isocontours_" + (screenshotCount++).ToString() + ".png", ImageFormat.Png);
        }

        #endregion

        #region OnUnload

        protected override void OnUnload(EventArgs e)
        {
            if (TextureObject != 0)
                GL.DeleteTextures(1, ref TextureObject);

            if (ProgramObject != 0)
                GL.DeleteProgram(ProgramObject); // implies deleting the previously flagged ShaderObjects
        }

        #endregion

        #region OnResize

        /// <summary>
        /// Respond to resize events here.
        /// </summary>
        /// <param name="e">Contains information on the new GameWindow size.</param>
        /// <remarks>There is no need to call the base implementation.</remarks>
        protected override void OnResize(EventArgs e)
        {
            UniformOffsetX = Width / 2;
            UniformOffsetY = Height / 2;

            GL.Viewport(0, 0, Width, Height);

            GL.MatrixMode(MatrixMode.Projection);
            GL.LoadIdentity();
            GL.Ortho(-1.0, 1.0, -1.0, 1.0, 0.0, 1.0); // 2D setup

            GL.MatrixMode(MatrixMode.Modelview);
            GL.LoadIdentity();
        }

        #endregion

        #region OnUpdateFrame

        /// <summary>
        /// Add your game logic here.
        /// </summary>
        /// <param name="e">Contains timing information.</param>
        /// <remarks>There is no need to call the base implementation.</remarks>
        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            if (Keyboard[OpenTK.Input.Key.Escape])
            {
                this.Exit();
            }
        }

        #endregion

        #region OnRenderFrame

        /// <summary>
        /// Add your game rendering code here.
        /// </summary>
        /// <param name="e">Contains timing information.</param>
        /// <remarks>There is no need to call the base implementation.</remarks>
        protected override void OnRenderFrame(FrameEventArgs e)
        {
            frameIndex++;
            frameIndex %= renderTimeAveragingFrameCount;
            renderTimeSum += e.Time;
            if (frameIndex == 0)
            {
                double renderTimeAvg = renderTimeSum / renderTimeAveragingFrameCount;
                this.Title = string.Format("Average FPS: {0:0.##}, time: {1:0.##} ms",
                    1 / renderTimeAvg, 1000 * renderTimeAvg);
                renderTimeSum = 0;
            }
            GL.Clear(ClearBufferMask.ColorBufferBit);

            // First, render the next frame of the Julia fractal.
            GL.UseProgram(ProgramObject);

            // pass uniforms into the fragment shader
            // first the texture
            //GL.Uniform1(GL.GetUniformLocation(ProgramObject, "COLORTABLE"), TextureObject);
            // the rest are floats
            GL.Uniform1(GL.GetUniformLocation(ProgramObject, "scale"), UniformScaleFactor);
            GL.Uniform2(GL.GetUniformLocation(ProgramObject, "offset"), new Vector2(UniformOffsetX, UniformOffsetY));
            GL.Uniform1(GL.GetUniformLocation(ProgramObject, "valueDrift"), UniformValueDrift);
            GL.Uniform1(GL.GetUniformLocation(ProgramObject, "functionIndex"), UnifromFunctionIndex);

            // Fullscreen quad. Using immediate mode, since this app is fragment shader limited anyways.
            GL.Begin(BeginMode.Quads);
            {
                GL.TexCoord2(0, 1); GL.Vertex2(-1.0f, 1.0f);
                GL.TexCoord2(0, 0); GL.Vertex2(-1.0f, -1.0f);
                GL.TexCoord2(1, 0); GL.Vertex2(1.0f, -1.0f);
                GL.TexCoord2(1, 1); GL.Vertex2(1.0f, 1.0f);
            }
            GL.End();

            SwapBuffers();
        }

        #endregion
    }
}
