#region --- License of the OpenTK skeleton application ---
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
        Matrix4 uniformCameraToWorld;

        float uniformRayStepLength = 1.0f;
        float uniformAttenuationExponent = 1.25f;
        float uniformAttenuationThreshold = 1e-10f;

        //string volumeFilenameHead = @"..\..\..\headCT.head";
        //string volumeFilenameRaw = @"..\..\..\headCT.raw";
        string volumeFilenameHead = @"..\..\..\fullHeadCT.head";
        string volumeFilenameRaw = @"..\..\..\fullHeadCT.raw";

        int vertexShaderObject, fragmentShaderObject, shaderProgram;

        private bool dragging = false;
        private int lastX, lastY;
        MouseButton? lastButton;

        public VolumeRendererForm()
            : base(512, 512)
        {
            UpdateCameraTransform(MathHelper.PiOver2, MathHelper.Pi);
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
            // Volume data are now in a GPU texture are are no longer in main memory.
            // Assume that the data will not be needed on CPU anymore.
            currentVolume.Values = null;
            GC.Collect();

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
            GL.Uniform2(GL.GetUniformLocation(shaderProgram, "senzorSize"), new Vector2(1.0f, Width / (float)Height));

            uniformRayStepLength = 1.0f / volumeDataSet.Size[2];

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

            // convert currentVolume from double[,,] to float[]* volumeData
            IntPtr volumeData = Marshal.AllocHGlobal(Marshal.SizeOf(typeof(float)) * width * height * depth);
            Stopwatch sw = Stopwatch.StartNew();

            unsafe
            {
                int rowIndex = 0;
                float scale = 1 / 4095.0f; // normalization of CT data
                var values = currentVolume.Values;
                for (int z = 0; z < depth; z++)
                {
                    for (int y = 0; y < height; y++)
                    {
                        float* row = (float*)volumeData + rowIndex;
                        for (int x = 0; x < width; x++)
                        {
                            float value = (float)currentVolume.Values[rowIndex + x];
                            row[x] = value * scale;
                        }
                        rowIndex += width;
                    }
                }
                sw.Stop();
                Console.WriteLine("Converted volume to 3D texture in {0} ms", sw.ElapsedMilliseconds);
            }

            int textureId = GL.GenTexture();
            GL.BindTexture(TextureTarget.Texture3D, textureId);

            sw.Reset();
            sw.Start();

            GL.TexImage3D(TextureTarget.Texture3D, 0, PixelInternalFormat.One,
                width, height, depth, 0,
                PixelFormat.Luminance, PixelType.Float, volumeData);

            sw.Stop();
            Console.WriteLine("Uploaded 3D volume texture to GPU in {0} ms", sw.ElapsedMilliseconds);

            // we can safely delete the volumeData as it was copied to texture memory
            Marshal.FreeHGlobal(volumeData);

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
            GL.UniformMatrix4(GL.GetUniformLocation(shaderProgram, "cameraToWorld"),
                false, ref uniformCameraToWorld);
            // ray casting parameters
            GL.Uniform1(GL.GetUniformLocation(shaderProgram, "rayStepLength"), uniformRayStepLength);
            // Sabella parameters
            GL.Uniform1(GL.GetUniformLocation(shaderProgram, "attenuationExponent"), uniformAttenuationExponent);
            GL.Uniform1(GL.GetUniformLocation(shaderProgram, "attenuationThreshold"), uniformAttenuationThreshold);

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
            else if (e.Key == Key.T)
            {
                uniformAttenuationExponent *= 1.01f;
                Console.WriteLine("uniformAttenuationExponent: {0}", uniformAttenuationExponent);
            }
            else if (e.Key == Key.G)
            {
                uniformAttenuationExponent /= 1.01f;
                Console.WriteLine("uniformAttenuationExponent: {0}", uniformAttenuationExponent);
            }
            else if (e.Key == Key.Y)
            {
                uniformAttenuationThreshold *= 2f;
                uniformAttenuationThreshold = Math.Min(uniformAttenuationThreshold, 1);
                uniformAttenuationThreshold = Math.Max(uniformAttenuationThreshold, 0);
                Console.WriteLine("uniformAttenuationThreshold: {0}", uniformAttenuationThreshold);
            }
            else if (e.Key == Key.H)
            {
                uniformAttenuationThreshold *= 0.5f;
                uniformAttenuationThreshold = Math.Min(uniformAttenuationThreshold, 1);
                uniformAttenuationThreshold = Math.Max(uniformAttenuationThreshold, 0);
                Console.WriteLine("uniformAttenuationThreshold: {0}", uniformAttenuationThreshold);
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

                // [0; PI]
                double eyeTheta = Math.PI * ((e.Y / (float)Height));
                // [0; 2 PI]
                double eyePhi = 2 * Math.PI * (e.X / (float)Width);

                UpdateCameraTransform((float)eyeTheta, (float)eyePhi);
            }
            lastX = e.X;
            lastY = e.Y;
        }

        private void UpdateCameraTransform(float eyeTheta, float eyePhi)
        {
            uniformCameraToWorld = Matrix4.CreateTranslation(new Vector3(0, 0, -0.75f));
            uniformCameraToWorld *= Matrix4.Scale(new Vector3(1, -1, 1));
            uniformCameraToWorld *= Matrix4.CreateRotationX((float)eyeTheta);
            uniformCameraToWorld *= Matrix4.CreateRotationZ((float)eyePhi);
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