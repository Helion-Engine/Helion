using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using Helion.Graphics;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Resources;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Images;
using Helion.Util.Geometry;
using static Helion.Util.Assertion.Assert;
using Image = Helion.Graphics.Image;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Sky.Sphere
{
    public class SkySphereTexture : IDisposable
    {
        private const string DefaultSky = "RSKY1";
        private const int PixelRowsToEvaluate = 16;

        public float ScaleU = 1.0f;
        private readonly ArchiveCollection m_archiveCollection;
        private readonly IGLFunctions gl;
        private readonly LegacyGLTextureManager m_textureManager;
        private GLLegacyTexture m_texture;
        private bool m_allocatedNewTexture;
        private bool m_generatedSky;

        public SkySphereTexture(ArchiveCollection archiveCollection, IGLFunctions functions,
            LegacyGLTextureManager textureManager)
        {
            m_archiveCollection = archiveCollection;
            gl = functions;
            m_textureManager = textureManager;
            m_texture = textureManager.NullTexture;
        }

        ~SkySphereTexture()
        {
            ReleaseUnmanagedResources();
        }

        public GLLegacyTexture GetTexture()
        {
            GenerateSkyIfNeeded();
            return m_texture;
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        private static float CalculateScale(int imageWidth)
        {
            // If the texture is huge, we'll just assume the user wants a one-
            // to-one scaling. See the bottom return comment on why this is
            // negative.
            if (imageWidth >= 1024)
                return -1.0f;
            
            // We want to fit either 4 '256 width textures' onto the sphere
            // or 1 '1024 width texture' onto the same area. While we're at
            // it, we can just make it so that the texture scales around to
            // it's nearest power of two.
            //
            // To do so, first find out X when we have width = 2^X.
            double roundedExponent = Math.Round(Math.Log(imageWidth, 2));
            
            // We want to fit it onto a sky that is 1024 in width. We can now
            // do `1024 / width` where width is a power of two. Since 1024 is
            // 2^10, then we can find out the scaling factor with the following
            // rearrangement:
            //          1024 / width
            //        = 2^10 / 2^x       [because x is a whole number now]
            //        = 2^(10 - x)
            float factor = (float)Math.Pow(2, 10 - roundedExponent); 
            
            // We make the scale negative so that the U coordinate is reversed.
            // The sphere is made in a counter-clockwise direction but drawing
            // the texture in other ports appears visually to be clockwise. By
            // setting the U scaling to be negative, the shader will reverse
            // the direction of the texturing (which is what we want).
            return -factor;
        }

        private static Color CalculateAverageRowColor(int startY, int exclusiveEndY, Image skyImage)
        {
            int r = 0;
            int g = 0;
            int b = 0;

            for (int y = startY; y < exclusiveEndY; y++)
            {
                for (int x = 0; x < skyImage.Width; x++)
                {
                    Color color = skyImage.Bitmap.GetPixel(x, y);
                    r += color.R;
                    g += color.G;
                    b += color.B;
                }
            }

            int totalPixels = (exclusiveEndY - startY) * skyImage.Width;
            r /= totalPixels;
            g /= totalPixels;
            b /= totalPixels;

            return Color.FromArgb(255, r, g, b);
        }

        private static Bitmap CreateFadedSky(int rowsToEvaluate, Color bottomFadeColor, Color topFadeColor, 
            Image skyImage)
        {
            int quarterY = skyImage.Height / 2;
            int middleY = quarterY + skyImage.Height;
            int threeQuartersY = middleY + skyImage.Height;
            Pen topPen = new Pen(Color.FromArgb(255, topFadeColor));
            Pen bottomPen = new Pen(Color.FromArgb(255, bottomFadeColor));

            // The sky texture looks like this:
            //
            //  0  o----------o 
            //     |Fade color|
            // 1/8 o..........o  <- Blending
            //     |          |
            //     | Texture  |
            // 1/2 o----------o
            //     |          |
            //     | Texture  |
            // 7/8 o..........o  <- Blending
            //     |Fade color|
            //  1  o----------o
            //
            // This is why we multiply by four. Note that there is no blending
            // at the horizon (middle line).
            Bitmap bitmap = new Bitmap(skyImage.Width, skyImage.Height * 3);
            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bitmap);
            g.FillRectangle(topPen.Brush, 0, 0, skyImage.Width, middleY);
            g.FillRectangle(bottomPen.Brush, 0, middleY, skyImage.Width, middleY);
            
            g.DrawImage(skyImage.Bitmap, 0, middleY - skyImage.Height);
            g.DrawImage(skyImage.Bitmap, 0, middleY);
            
            g.CompositingMode = CompositingMode.SourceOver;
            BlendSeam(quarterY + rowsToEvaluate - 1, quarterY - 1, topFadeColor);
            BlendSeam(threeQuartersY - rowsToEvaluate, threeQuartersY + 1, bottomFadeColor);

            return bitmap;
            
            void BlendSeam(int startY, int endExclusiveY, Color fadeColor)
            {
                Precondition(startY != endExclusiveY, "Cannot blend an empty range");
                Precondition(startY >= 0 && endExclusiveY < bitmap.Height, "Start blend index out of range");
                Precondition(endExclusiveY >= -1 && endExclusiveY <= bitmap.Height, "End blend index out of range");

                int blendRange = Math.Abs(endExclusiveY - startY);
                int alphaStepDelta = 255 / Math.Max(Math.Min(blendRange, 255), 1);
                int alpha = 0;

                // We need to support both directions, which is why this is
                // using a step variable and a while loop. The idea is to
                // go along each row and blend it, and then decrease the
                // alpha each row we proceed to by a small bit so it looks
                // like it's blending to the background color slowly.
                int step = (startY < endExclusiveY ? 1 : -1);
                int iteration = 0;
                int y = startY;
                while (iteration < blendRange)
                {
                    Pen pen = new Pen(Color.FromArgb(alpha, fadeColor));
                    g.DrawLine(pen, 0, y, bitmap.Width - 1, y);
                
                    alpha += alphaStepDelta;

                    y += step;
                    iteration++;
                }
            }
        }

        private void GenerateSkyIfNeeded()
        {
            if (m_generatedSky)
                return;
         
            // This sucks, but we need the archive to be populated before we
            // can draw the sky. This has to be lazily loaded when we request
            // rendering so we know all the textures have been loaded.
            GenerateSkyTextures();
            m_generatedSky = true;
        }

        private void GenerateSkyTextures()
        {
            Image? skyImage = GetSkyImage();
            if (skyImage == null)
                return;

            ScaleU = CalculateScale(skyImage.Width);
            m_texture = CreateSkyTexture(skyImage);
            m_allocatedNewTexture = true;
        }

        private Image GetSkyImage()
        {
            ArchiveImageRetriever imageRetriever = new ArchiveImageRetriever(m_archiveCollection);
            return imageRetriever.Get(DefaultSky, ResourceNamespace.Textures);
        }

        private GLLegacyTexture CreateSkyTexture(Image skyImage)
        {
            // Most (all?) skies are tall enough that we don't have to worry
            // about this, but if we run into a sky that is small then we
            // don't want to consume more than half of it. We also need to
            // make sure that we don't get a zero value if someone tries to
            // provide a single pixel sky (since Height(1) / 2 would be 0, so
            // we clamp it to be at least 1).
            int rowsToEvaluate = Math.Min(Math.Max(skyImage.Height / 2, 1), PixelRowsToEvaluate);
            
            int bottomStartY = skyImage.Height - rowsToEvaluate;
            int bottomExclusiveEndY = skyImage.Height;
            Color topFadeColor = CalculateAverageRowColor(0, rowsToEvaluate, skyImage);
            Color bottomFadeColor = CalculateAverageRowColor(bottomStartY, bottomExclusiveEndY, skyImage);

            Bitmap fadedSkyImage = CreateFadedSky(rowsToEvaluate, bottomFadeColor, topFadeColor, skyImage);
            return CreateTexture(fadedSkyImage, $"[SKY] {DefaultSky}");
        }

        private GLLegacyTexture CreateTexture(Bitmap fadedSkyImage, string debugName = "")
        {
            int textureId = gl.GenTexture();
            Dimension dimension = new Dimension(fadedSkyImage.Width, fadedSkyImage.Height);
            Image image = new Image(fadedSkyImage, new ImageMetadata());
            GLLegacyTexture texture = new GLLegacyTexture(0, textureId, debugName, dimension, gl, TextureTargetType.Texture2D);

            m_textureManager.UploadAndSetParameters(texture, image, debugName, ResourceNamespace.Global);
            
            return texture;
        }

        private void ReleaseUnmanagedResources()
        {
            if (m_allocatedNewTexture) 
                m_texture.Dispose();
        }
    }
}