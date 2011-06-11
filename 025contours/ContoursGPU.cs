#region --- License ---

// Iso-contours of implicit functions.
// Author: Bohumir Zamecnik <bohumir.zamecnik at gmail dot com>
// Date: May/June 2011
// Original algorithm:
//   Josef Pelikan: Raster algorithms for computing iso-contours,
//   KSVI MFF UK, 1992.

// The skeleton of the application is based on the Julia fractal demo
// from OpenTK examples.

/* Licensed under the MIT/X11 license.
 * Copyright (c) 2006-2008 the OpenTK Team.
 * This notice may not be removed from any source distribution.
 * See license.txt for licensing detailed licensing details.
 * 
 * Written by Christoph Brandtner
 */
#endregion

using System;
using System.Collections.Generic;
using System.Linq;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using OpenTK.Input;

namespace _025contours
{
    public class IsoContoursGpu : GameWindow
    {
        #region Private Fields

        // GLSL Objects
        int VertexShaderObject, FragmentShaderObject, ProgramObject;
        int TextureObject;

        // scale factor for XY coordinates
        float UniformScaleFactor = 0.4f;
        // offset in XY window coordinates
        float UniformOffsetX = 0;
        float UniformOffsetY = 0;
        // offset in Z coordinate - function value
        float UniformValueDrift = 0.0f;

        private static readonly int MaxThresholdCount = 256;
        private float[] UniformThresholds = new float[MaxThresholdCount];
        private int UniformThresholdCount = 50;

        int currentFunctionIndex = 0;

        int screenshotCount = 0;

        // number of frames whose time is averaged
        int renderTimeAveragingFrameCount = 10;
        int frameIndex = 0;
        double renderTimeSum = 0;

        string fragmentShaderTemplate;

        GlslFunctionEditor functionEditor;

        private bool dragging = false;

        private int lastX, lastY;

        MouseButton? lastButton;

        /// <summary>
        /// Implicit R^2->R function to be evaluated in fragment shader
        /// (source code of a body of a GLSL function).
        /// </summary>
        /// <remarks>
        /// <para>
        /// The function can be two float input parameters x, y
        /// and have to return a single float value.
        /// </para>
        /// <para>
        /// For example, such a source
        /// <code>
        /// return sin(0.1 * x) + cos(0.1 * y);
        /// </code>
        /// will be expanded into the following or similar function:
        /// <code>
        /// float f(in float x, in float y) {
        ///     return sin(0.1 * x) + cos(0.1 * y);
        /// }
        /// </code>
        /// </para>
        /// </remarks>
        string FunctionSource { get; set; }

        IDictionary<string, string> ImplicitFunctions = new Dictionary<string, string>();

        public IsoContoursGpu()
            : base(512, 512)
        {
            ImplicitFunctions.Add("waves",
@"    return sin(0.1 * x) + cos(0.1 * y);");
            ImplicitFunctions.Add("drop",
@"    float r = 0.1 * sqrt(x * x + y * y);
    return ((r <= 10e-16) ? 10.0 : (10.0 * sin(r) / r));");
            ImplicitFunctions.Add("inverse_gradient",
@"    return 0.001 * x * y;");
        }

        #endregion private Fields

        #region OnLoad

        /// <summary>
        /// Setup OpenGL and load resources here.
        /// </summary>
        /// <param name="e">Not used.</param>
        protected override void OnLoad(EventArgs e)
        {
            PrepareThresholds(UniformThresholdCount);

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
                fragmentShaderTemplate = sr.ReadToEnd();
            }

            FunctionSource = GetImplicitFunctionSource();

            string fragmentShaderSource = FillFunctionIntoFragmentShader(
                fragmentShaderTemplate, FunctionSource);

            FragmentShaderObject = GL.CreateShader(ShaderType.FragmentShader);
            GL.ShaderSource(FragmentShaderObject, fragmentShaderSource);
            GL.CompileShader(FragmentShaderObject);

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
            Mouse.ButtonDown += MouseButtonDown;
            Mouse.ButtonUp += MouseButtonUp;
            Mouse.Move += MouseMove;
            //Mouse.WheelChanged += MouseWheelChanged;
        }

        private string FillFunctionIntoFragmentShader(string fragmentShaderTemplate, string functionSource)
        {
            return fragmentShaderTemplate.Replace("// ### FUNCTION ###", functionSource);
        }

        private string GetImplicitFunctionSource()
        {
            if ((ImplicitFunctions.Count > 0) && (currentFunctionIndex < ImplicitFunctions.Count))
            {
                return ImplicitFunctions.Values.ElementAt(currentFunctionIndex);
            }
            else
            {
                return "return 0.0;";
            }
        }

        private void RecompileFragmentShader()
        {
            //GL.UseProgram(0);
            //GL.DetachShader(ProgramObject, FragmentShaderObject);

            FunctionSource = GetImplicitFunctionSource();
            string fragmentShaderSource = FillFunctionIntoFragmentShader(
                fragmentShaderTemplate, FunctionSource);

            GL.ShaderSource(FragmentShaderObject, fragmentShaderSource);
            GL.CompileShader(FragmentShaderObject);

            string logInfo;
            GL.GetShaderInfoLog(FragmentShaderObject, out logInfo);
            if (logInfo.Length > 0 && !logInfo.Contains("hardware"))
                Trace.WriteLine("Fragment Shader Log:\n" + logInfo);
            else
                Trace.WriteLine("Fragment Shader compiled without complaint.");

            // Link the Shaders to a usable Program
            //GL.AttachShader(ProgramObject, FragmentShaderObject);
            GL.LinkProgram(ProgramObject);

            // make current
            //GL.UseProgram(ProgramObject);
        }

        void KeyDown(object sender, KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.W)
            {
                UniformOffsetY -= 10;
            }
            else if (e.Key == Key.S)
            {
                UniformOffsetY += 10;
            }
            else if (e.Key == Key.D)
            {
                UniformOffsetX -= 10;
            }
            else if (e.Key == Key.A)
            {
                UniformOffsetX += 10;
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
                currentFunctionIndex += 1;
                currentFunctionIndex %= ImplicitFunctions.Count;
                RecompileFragmentShader();
            }
            else if (e.Key == Key.P)
            {
                PrepareThresholds(UniformThresholdCount + 1);
            }
            else if (e.Key == Key.M)
            {
                PrepareThresholds(UniformThresholdCount - 1);
            }
            else if (e.Key == Key.R)
            {
                ResetScale();
            }
        }

        void KeyUp(object sender, KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.F1)
            {
                MessageBox.Show(
@"===== Program help =====

Compute iso-contours of an implicit function R^2 -> R.

==== Key controls ====

F - choose next function
P - more thresholds (uniformly distributed, up to 256)
M - less thresholds

W - scroll up
S - scroll down
A - sroll left
D - scroll right
Z - zoom in
X - zoom out
Q - shift function value up
E - shift function value down
R - reset scroll, zoom, shift

F1 - show help
F4 - edit functions
F11 - toggle full screen
F12 - save screen shot

==== Mouse controls ==== 

Left button + drag - scroll up/down, left/right
Right button + drag up/down - zoom in/out
Right button + drag left/right - shift function value
SHIFT + left button + drag up/down - change number of thresholds

==== Credits ====

Implementation - Bohumír Zameèník, 2011, MFF UK
Algorithm - Josef Pelikán, 1992, MFF UK
Program skeleton - OpenTK Library Examples
", "Iso-contours of implicit functions on GPU");
            }
            else if (e.Key == Key.F4)
            {
                if (functionEditor == null)
                {
                    functionEditor = new GlslFunctionEditor();
                }
                functionEditor.ImplicitFunctions = ImplicitFunctions;
                functionEditor.ShowDialog();
                if (functionEditor.DialogResult == DialogResult.OK)
                {
                    ImplicitFunctions = functionEditor.ImplicitFunctions;
                }
                RecompileFragmentShader();
            }
            else if (e.Key == Key.F11)
            {
                bool isFullscreen = (WindowState == WindowState.Fullscreen);
                WindowState = isFullscreen ? WindowState.Normal : WindowState.Fullscreen;
            }
            else if (e.Key == Key.F12)
            {
                SaveScreenshot();
            }
        }

        void MouseButtonDown(object sender, MouseButtonEventArgs e)
        {
            if (e.Button == MouseButton.Left ||
                 e.Button == MouseButton.Right)
            {
                dragging = true;
                lastX = e.X;
                lastY = e.Y;
                lastButton = e.Button;
            }
        }

        void MouseButtonUp(object sender, MouseButtonEventArgs e)
        {
            dragging = false;
            lastButton = null;
        }

        void MouseMove(object sender, MouseMoveEventArgs e)
        {
            if (!dragging || !lastButton.HasValue) return;

            if (lastButton.Value == MouseButton.Left)
            {
                if (Keyboard[Key.ShiftLeft] || Keyboard[Key.ShiftRight])
                {
                    PrepareThresholds(UniformThresholdCount - (e.YDelta / 2));
                }
                else
                {
                    UniformOffsetX += e.X - lastX;
                    UniformOffsetY -= e.Y - lastY;
                    lastX = e.X;
                    lastY = e.Y;
                }
            }

            if (lastButton.Value == MouseButton.Right)
            {
                UniformScaleFactor *= (float)Math.Exp(0.01 * (e.Y - lastY));
                UniformValueDrift += 0.02f * (e.X - lastX);
                lastX = e.X;
                lastY = e.Y;
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
                this.Title = string.Format("Iso-contours. Average FPS: {0:0.##}, time: {1:0.##} ms, thresholds: {2}. Press F1 for help.",
                    1 / renderTimeAvg, 1000 * renderTimeAvg, UniformThresholdCount);
                renderTimeSum = 0;
            }
            GL.Clear(ClearBufferMask.ColorBufferBit);

            GL.UseProgram(ProgramObject);

            GL.Uniform1(GL.GetUniformLocation(ProgramObject, "scale"), UniformScaleFactor);
            GL.Uniform2(GL.GetUniformLocation(ProgramObject, "offset"), new Vector2(UniformOffsetX, UniformOffsetY));
            GL.Uniform1(GL.GetUniformLocation(ProgramObject, "valueDrift"), UniformValueDrift);

            GL.Uniform1(GL.GetUniformLocation(ProgramObject, "thresholds"), UniformThresholdCount, ref UniformThresholds[0]);
            GL.Uniform1(GL.GetUniformLocation(ProgramObject, "thresholdCount"), UniformThresholdCount);

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

        private void PrepareThresholds(int thresholdCount)
        {
            if ((thresholdCount <= 0) || (thresholdCount > MaxThresholdCount))
            {
                thresholdCount = Math.Min(thresholdCount, MaxThresholdCount);
                thresholdCount = Math.Max(thresholdCount, 1);
                return;
            }
            UniformThresholdCount = thresholdCount;

            UniformThresholds = new float[thresholdCount];

            float thresholdMin = -4.0f;
            float thresholdMax = 4.0f;

            float thresholdStep = (thresholdMax - thresholdMin) / (float)thresholdCount;
            float threshold = thresholdMin;
            for (int i = 0; i < thresholdCount; i++)
            {
                threshold += thresholdStep;
                UniformThresholds[i] = threshold;
            }
        }

        private void ResetScale()
        {
            UniformOffsetX = Width / 2.0f;
            UniformOffsetY = Height / 2.0f;
            UniformScaleFactor = 0.4f;
            UniformValueDrift = 0.0f;
            PrepareThresholds(50);
        }
    }
}
