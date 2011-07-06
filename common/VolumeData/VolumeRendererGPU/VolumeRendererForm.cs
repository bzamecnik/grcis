#region --- License ---
/* Licensed under the MIT/X11 license.
 * Copyright (c) 2006-2008 the OpenTK Team.
 * This notice may not be removed from any source distribution.
 * See license.txt for licensing details.
 */
#endregion

namespace VolumeRendererGPU
{
    using System;
    using System.Diagnostics;
    using System.Drawing;
    using System.IO;
    using System.Runtime.InteropServices;
    using System.Windows.Forms;
    using OpenTK;
    using OpenTK.Graphics.OpenGL;
    using OpenTK.Input;
    using VolumeData;

    public class VolumeRendererForm : GameWindow
    {
        VolumeDataSet volumeDataSet;
        VolumeDataSet.VolumeGrid currentVolume;

        int volume3dTexture = 0;

        float uniformDepth;
        float uniformSelectedDepth;
        //Vector2 uniformValueRange;
        Matrix4 uniformCameraToWorld;
        Matrix4 worldToCamera;

        string volumeFilenameHead = @"..\..\..\headCT.head";
        string volumeFilenameRaw = @"..\..\..\headCT.raw";

        int vertexShaderObject, fragmentShaderObject, shaderProgram;

        private bool dragging = false;
        private int lastX, lastY;
        MouseButton? lastButton;

        public VolumeRendererForm()
            : base(800, 600)
        {
            //worldToCamera = Matrix4.Identity;
            //uniformCameraToWorld = Matrix4.Identity;

            Vector3 eye = 2.0f * new Vector3(
                    (float)(Math.Sin(0.2) * Math.Cos(0)),
                    (float)(Math.Sin(0.2) * Math.Sin(0)),
                    (float)(Math.Cos(0.2)));
            UpdateCameraTransform(eye);
        }

        private VolumeDataSet LoadVolumeDataSet()
        {
            Stopwatch sw = Stopwatch.StartNew();
            var dataSet = VolumeDataSet.LoadFromFile(volumeFilenameHead, volumeFilenameRaw);
            sw.Stop();
            Console.WriteLine("Loaded volume in {0} ms", sw.ElapsedMilliseconds);
            return dataSet;
        }

        public static void RunExample()
        {
            using (VolumeRendererForm example = new VolumeRendererForm())
            {
                example.Run(30.0, 0.0);
            }
        }

        #region GameWindow event handlers

        protected override void OnLoad(EventArgs e)
        {
            //GL.Enable(EnableCap.Texture2D);
            GL.Enable(EnableCap.Texture3DExt);

            using (StreamReader vs = new StreamReader("Data/Shaders/VertexShader.glsl"))
            using (StreamReader fs = new StreamReader("Data/Shaders/FragmentShader.glsl"))
                CreateShaders(vs.ReadToEnd(), fs.ReadToEnd(),
                    out vertexShaderObject, out fragmentShaderObject,
                    out shaderProgram);

            // TODO: set this.Width and this.Height according to the volume

            volumeDataSet = LoadVolumeDataSet();
            currentVolume = volumeDataSet.TimeSlices[0];
            volume3dTexture = LoadVolumeSliceTexture();

            GL.ActiveTexture(TextureUnit.Texture0);
            GL.BindTexture(TextureTarget.Texture3D, volume3dTexture);

            // texture unit numbers must be passed here, not texture ids!
            // texture units are numered from 0
            // Texture0 = 0
            // Texture1 = 1
            GL.Uniform1(GL.GetUniformLocation(shaderProgram, "volumeTexture"), 0);
            uniformDepth = volumeDataSet.Size[2];
            GL.Uniform1(GL.GetUniformLocation(shaderProgram, "depth"), uniformDepth);
            GL.Uniform1(GL.GetUniformLocation(shaderProgram, "selectedDepth"), uniformSelectedDepth);
            GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram, "cameraToWorld"),
                false, ref uniformCameraToWorld);

            Keyboard.KeyUp += KeyUp;
            Mouse.ButtonDown += MouseButtonDown;
            Mouse.ButtonUp += MouseButtonUp;
            Mouse.Move += MouseMove;
        }

        protected override void OnUnload(EventArgs e)
        {
            // Clean up what we allocated before exiting
            if (volume3dTexture != 0)
                GL.DeleteTexture(volume3dTexture);

            if (shaderProgram != 0)
                GL.DeleteProgram(shaderProgram);
            if (fragmentShaderObject != 0)
                GL.DeleteShader(fragmentShaderObject);
        }

        protected override void OnResize(EventArgs e)
        {
            GL.Viewport(0, 0, Width, Height);
            //Draw();

            base.OnResize(e);
        }

        protected override void OnUpdateFrame(FrameEventArgs e)
        {
            base.OnUpdateFrame(e);

            if (Keyboard[Key.Escape])
            {
                this.Exit();
            }
        }

        protected override void OnRenderFrame(FrameEventArgs e)
        {
            this.Title = "FPS: " + 1 / e.Time;

            Draw();

            this.SwapBuffers();
        }

        #endregion

        #region Setting up layer textures

        private int LoadVolumeSliceTexture()
        {
            int width = volumeDataSet.Size[0];
            int height = volumeDataSet.Size[1];
            int depth = volumeDataSet.Size[2];

            //int width = 20;
            //int height = 30;
            //int depth = 40;

            // convert currentVolume from double[,,] to float[]* volumeData
            IntPtr volumeData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(float)) * width * height * depth);
            //float minValue = float.PositiveInfinity;
            //float maxValue = float.NegativeInfinity;
            unsafe
            {
                int rowIndex = 0;
                float scale = 1 / 4095.0f;
                for (int z = 0; z < depth; z++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        //float* row = (float*)volumeData + width * (height * z + y);
                        float* row = (float*)volumeData + rowIndex;
                        for (int x = 0; x < width; x++)
                        {
                            float value = (float)currentVolume[x, y, z, 0];
                            //minValue = Math.Min(value, minValue);
                            //maxValue = Math.Max(value, maxValue);
                            row[x] = value * scale;
                            //row[x] = (float)random.NextDouble();
                        }
                        rowIndex += width;
                    }
                }
            }
            //uniformValueRange = new Vector2(minValue, maxValue);

            int textureId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture3D, textureId);

            GL.TexImage3D(TextureTarget.Texture3D, 0, PixelInternalFormat.One,
                width, height, depth, 0,
                PixelFormat.Luminance, PixelType.Float, volumeData);

            // TODO: delete volumeData if needed
            //Marshal.FreeHGlobal(volumeData);

            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMinFilter, (int)TextureMinFilter.Linear);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureMagFilter, (int)TextureMagFilter.Linear);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapS, (int)TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapT, (int)TextureWrapMode.ClampToBorder);
            GL.TexParameter(TextureTarget.Texture3D, TextureParameterName.TextureWrapR, (int)TextureWrapMode.ClampToBorder);

            return textureId;
        }

        #endregion

        #region  Setting up shaders

        void CreateShaders(string vs, string fs,
            out int vertexObject, out int fragmentObject,
            out int program)
        {
            int statusCode;
            string info;

            vertexObject = GL.CreateShader(ShaderType.VertexShader);
            fragmentObject = GL.CreateShader(ShaderType.FragmentShader);

            // Compile vertex shader
            GL.ShaderSource(vertexObject, vs);
            GL.CompileShader(vertexObject);
            GL.GetShaderInfoLog(vertexObject, out info);
            GL.GetShader(vertexObject, ShaderParameter.CompileStatus, out statusCode);

            if (statusCode != 1)
                throw new ApplicationException(info);

            // Compile vertex shader
            GL.ShaderSource(fragmentObject, fs);
            GL.CompileShader(fragmentObject);
            GL.GetShaderInfoLog(fragmentObject, out info);
            GL.GetShader(fragmentObject, ShaderParameter.CompileStatus, out statusCode);

            if (statusCode != 1)
                throw new ApplicationException(info);

            program = GL.CreateProgram();
            GL.AttachShader(program, fragmentObject);
            GL.AttachShader(program, vertexObject);

            GL.LinkProgram(program);
            GL.UseProgram(program);
        }

        #endregion

        #region Drawing the scene

        private void Draw()
        {
            GL.Viewport(0, 0, Width, Height);

            GL.UseProgram(shaderProgram);

            // pass uniforms (which are modified) here
            GL.Uniform1(GL.GetUniformLocation(shaderProgram, "depth"), uniformDepth);
            GL.Uniform1(GL.GetUniformLocation(shaderProgram, "selectedDepth"), uniformSelectedDepth);
            //GL.Uniform2(GL.GetUniformLocation(shaderProgram, "valueRange"), uniformValueRange);
            GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram, "cameraToWorld"),
                false, ref uniformCameraToWorld);

            DrawFullScreenQuad();

            GL.UseProgram(0);
        }

        private void DrawFullScreenQuad()
        {
            GL.Begin(BeginMode.Quads);
            {
                // texture X goes from left to right, Y goes from top to bottom
                GL.TexCoord2(0, 0); GL.Vertex2(-1.0f, 1.0f); // bottom left
                GL.TexCoord2(0, 1); GL.Vertex2(-1.0f, -1.0f); // top left
                GL.TexCoord2(1, 1); GL.Vertex2(1.0f, -1.0f); // top right
                GL.TexCoord2(1, 0); GL.Vertex2(1.0f, 1.0f); // bottom right
            }
            GL.End();
        }

        #endregion

        #region User interaction

        private void KeyUp(object sender, KeyboardKeyEventArgs e)
        {
            if (e.Key == Key.F1)
            {
                MessageBox.Show(
@"===== Program help =====

==== Key controls ====

F1 - show help
F11 - toggle full screen
F12 - save screen shot

==== Credits ====

Implementation - Bohumír Zamečník, 2011, MFF UK
Program skeleton - OpenTK Library Examples
", "Volume visualization");
            }
            else if (e.Key == Key.F12)
            {
                SaveScreenshot();
            }
            else if (e.Key == Key.F11)
            {
                bool isFullscreen = (WindowState == WindowState.Fullscreen);
                WindowState = isFullscreen ? WindowState.Normal : WindowState.Fullscreen;
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
                //UniformImageLayerDepth *= (float)Math.Exp(0.005 * (e.Y - lastY));
                uniformSelectedDepth = e.Y / (float)Height;

                // [-PI/2; PI/2]
                double eyeTheta = Math.PI * ((e.Y / (float)Height) - 0.5);
                Console.WriteLine(eyeTheta);
                // [0; 2 PI]
                double eyePhi = 2 * Math.PI * (e.X / (float)Width);
                float eyeRadius = 2.0f;

                //Vector3 eye = eyeRadius * new Vector3(
                //    (float)(Math.Cos(eyeTheta) * Math.Cos(eyePhi)),
                //    (float)(Math.Cos(eyeTheta) * Math.Sin(eyePhi)),
                //    (float)(Math.Sin(eyeTheta)));
                //UpdateCameraTransform(eye);

                worldToCamera = Matrix4.CreateTranslation(new Vector3(0, 0, -eyeRadius));
                //worldToCamera *= Matrix4.CreateRotationX((float)eyeTheta);
                //worldToCamera *= Matrix4.CreateRotationZ((float)eyePhi);
                //worldToCamera = Matrix4.Identity;
                uniformCameraToWorld = Matrix4.Invert(worldToCamera);
            }
            lastX = e.X;
            lastY = e.Y;
        }

        private void UpdateCameraTransform(Vector3 eye)
        {
            worldToCamera = Matrix4.LookAt(eye, Vector3.Zero, Vector3.UnitY);
            uniformCameraToWorld = Matrix4.Invert(worldToCamera);
        }

        #endregion

        #region Saving images to files

        private void SaveScreenshot()
        {
            using (Bitmap bmp = new Bitmap(this.Width, this.Height))
            {
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
                bmp.Save(string.Format("{0}_screenshot.png", GetScreenshotId()), System.Drawing.Imaging.ImageFormat.Png);
            }
        }

        private static string GetScreenshotId()
        {
            return DateTime.Now.ToString("yyyy-MM-dd_hh-mm-ss");
        }

        #endregion
    }
}