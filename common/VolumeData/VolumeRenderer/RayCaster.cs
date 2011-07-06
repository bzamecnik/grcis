using System;
using System.Diagnostics;
using System.Drawing;
using System.Linq;
using VolumeData;
using OpenTK;

namespace VolumeRenderer
{
    public class RayCaster
    {
        public static Bitmap RenderMaxIntensityProjection(VolumeDataSet dataSet, Bitmap outputImage)
        {
            int width = 512;
            int height = 512;
            if ((outputImage == null) || (outputImage.Width != width)
                || (outputImage.Height != height))
            {
                outputImage = new Bitmap(width, height);
            }

            var volumeTexture = dataSet.TimeSlices[0];

            float scaleFactor = 255.0f / 4095.0f;
            float rayStepLength = 100.0f / volumeTexture.Depth;
            Vector2 xyPixelSize = new Vector2(1 / (float)width, 1 / (float)height);
            Vector2 senzorSize = new Vector2(1, 1);
            Matrix4 cameraToWorld = Matrix4.CreateTranslation(new Vector3(0, 0, -0.5f));
            cameraToWorld *= Matrix4.CreateRotationY(0f * MathHelper.Pi);
            cameraToWorld *= Matrix4.CreateRotationX(-0.5f * MathHelper.Pi);
            cameraToWorld *= Matrix4.CreateRotationZ(0.25f * MathHelper.Pi);
            //.CreateRotationY((float)Math.PI);
            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++)
                {
                    float intensity = RayCastVolume(volumeTexture,
                        new Vector2(x * xyPixelSize.X, y * xyPixelSize.Y),
                        rayStepLength, senzorSize, ref cameraToWorld);
                    byte value = (byte)(intensity * scaleFactor);
                    outputImage.SetPixel(x, y, Color.FromArgb(value, value, value));
                }
            }

            return outputImage;
        }

        private static float RayCastVolume(
            VolumeDataSet.VolumeGrid volumeTexture,
            Vector2 pixelPos, float rayStepLength,
            Vector2 senzorSize, ref Matrix4 cameraToWorld)
        {
            // senzor position in camera space
            // map from [0; 1]^2 to [-w/2;w/2]x[-h/2;h/2]
            // TODO: use a matrix (ndcToCamera)
            Vector2 senzorPos = pixelPos - new Vector2(0.5f, 0.5f);
            senzorPos = Vector2.Multiply(senzorPos, senzorSize);
            // ray origin in world space
            Vector3 rayOrigin = (Vector4.Transform(new Vector4(senzorPos.X, senzorPos.Y, 0, 1), cameraToWorld)).Xyz;
            // case of orthogonal projection
            Vector3 rayDirection = (Vector4.Transform(new Vector4(0.0f, 0.0f, 1.0f, 0.0f), cameraToWorld)).Xyz;

            // clip the ray by the volume cube [0;1]^3 to a line segment
            // TODO

            Vector3 rayStart = rayOrigin;
            Vector3 rayEnd = rayOrigin + 1 * rayDirection;

            Vector3 position = rayStart;
            Vector3 rayStep = rayStepLength * Vector3.Normalize(rayDirection);
            int stepCount = (int)((rayEnd - rayStart).Length / rayStepLength);
            float colorAccum = 0;
            float maxIntensity = 0;
            for (int i = 0; i < stepCount; i += 1)
            {
                float intensity = Texture3D(volumeTexture, position);

                maxIntensity = Math.Max(intensity, maxIntensity);
                //colorAccum += intensity;

                position += rayStep;
            }
            colorAccum = maxIntensity;
            return colorAccum;

            // TODO:
            // - input:
            //    - pixel position on senzor [0;1]^2
            //    - constant:
            //      - volume data (3D texture)
            //      - senzor size in camera space
            //      - uniform spacing between points for ray marching
            // - output: color
            // - algorithm:
            //   - transform pixel position to senzor position in camera space
            //   - transform senzor position to world space -> ray origin
            //     - world to camera matrix
            //   - compute ray direction
            //     - using orthographic or perspective transform
            //       - orthographic - normal to senzor plane
            //       - perspective - (center of projection) - (ray origin)
            //   - clip the ray to the volume cube
            //     - compute six ray to square intersections
            //     - find out min/max ray parameters
            //     - compute start/end points (in world space)
            //       - no intersection -> zero ray marching steps
            //   - find out the number of steps of ray marching
            //   - march the ray
            //     - get volume density at the current position
            //     - evaluate and accumulate color
            //   - return the accumulated color
        }

        private static float Texture3D(VolumeDataSet.VolumeGrid volumeTexture, Vector3 position)
        {
            int width = volumeTexture.Width;
            int height = volumeTexture.Height;
            int depth = volumeTexture.Depth;
            // map from centered cube [-0.5;0.5]^3 to [0; 1]^3
            position += new Vector3(0.5f, 0.5f, 0.5f);
            int x = (int)Math.Round(position.X * width);
            int y = (int)Math.Round(position.Y * height);
            int z = (int)Math.Round(position.Z * depth);
            if ((x < 0) || (x >= width) ||
                (y < 0) || (y >= height) ||
                (z < 0) || (z >= depth))
            {
                return 0;
            }
            return (float)volumeTexture[x, y, z, 0];
        }
    }
}
