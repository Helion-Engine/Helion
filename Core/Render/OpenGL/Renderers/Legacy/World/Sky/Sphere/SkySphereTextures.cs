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
    public class SkySphereTextures : IDisposable
    {
        private const string DefaultSky = "RSKY1";
        private const int PixelRowsToEvaluate = 16;

        private readonly ArchiveCollection m_archiveCollection;
        private readonly IGLFunctions gl;
        private readonly LegacyGLTextureManager m_textureManager;
        private GLLegacyTexture m_lowerTexture;
        private GLLegacyTexture m_upperTexture;
        private bool m_allocatedNewTextures;
        private bool m_generatedSky;

        public SkySphereTextures(ArchiveCollection archiveCollection, IGLFunctions functions,
            LegacyGLTextureManager textureManager)
        {
            m_archiveCollection = archiveCollection;
            gl = functions;
            m_textureManager = textureManager;
            m_lowerTexture = textureManager.NullTexture;
            m_upperTexture = textureManager.NullTexture;
        }

        ~SkySphereTextures()
        {
            ReleaseUnmanagedResources();
        }

        public GLLegacyTexture GetUpperSky()
        {
            GenerateSkyIfNeeded();
            return m_upperTexture;
        }

        public GLLegacyTexture GetLowerSky()
        {
            GenerateSkyIfNeeded();
            return m_lowerTexture;
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
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

            CreateUpperSkyTexture(skyImage);
            CreateLowerSkyTexture(skyImage);
            m_allocatedNewTextures = true;
        }
        
        private Image GetSkyImage()
        {
            ArchiveImageRetriever imageRetriever = new ArchiveImageRetriever(m_archiveCollection);
            return imageRetriever.Get(DefaultSky, ResourceNamespace.Textures);
        }
        
        private void CreateUpperSkyTexture(Image skyImage)
        {
            int rowsToEvaluate = Math.Min(skyImage.Height, PixelRowsToEvaluate);
            
            Color fadeColor = CalculateAverageRowColor(0, rowsToEvaluate, skyImage);
            Bitmap fadedSkyImage = CreateFadedSkyTop(rowsToEvaluate, fadeColor, skyImage);

            m_upperTexture = CreateTexture(fadedSkyImage, $"[SKY] Upper: {DefaultSky}");
        }

        private void CreateLowerSkyTexture(Image skyImage)
        {
            int rowsToEvaluate = Math.Min(skyImage.Height, PixelRowsToEvaluate);
            int startY = skyImage.Height - rowsToEvaluate;
            int exclusiveEndY = skyImage.Height;
            
            Color fadeColor = CalculateAverageRowColor(startY, exclusiveEndY, skyImage);
            Bitmap fadedSkyImage = CreateFadedSkyBottom(rowsToEvaluate, fadeColor, skyImage);

            m_lowerTexture = CreateTexture(fadedSkyImage, $"[SKY] Lower: {DefaultSky}");
        }
        
        private Bitmap CreateFadedSkyTop(int blendRange, Color fadeColor, Image skyImage)
        {
            Precondition(blendRange < 255 && blendRange > 0, "Invalid blending range for sky texture");
            
            Bitmap bitmap = new Bitmap(skyImage.Width, skyImage.Height * 2);
            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bitmap);
            g.Clear(fadeColor);
            
            g.DrawImage(skyImage.Bitmap, new Point(0, skyImage.Height));

            g.CompositingMode = CompositingMode.SourceOver;
            int alpha = 255;
            int alphaReductionFactor = 255 / Math.Max(Math.Min(blendRange, 255), 1);
            
            for (int y = skyImage.Height; y < skyImage.Height + blendRange; y++)
            {
                Pen pen = new Pen(Color.FromArgb(alpha, fadeColor));
                g.DrawLine(pen, new Point(0, y), new Point(skyImage.Width - 1, y));
                
                alpha -= alphaReductionFactor;
            }

            return bitmap;
        }
        
        private Bitmap CreateFadedSkyBottom(int blendRange, Color fadeColor, Image skyImage)
        {
            Precondition(blendRange < 255 && blendRange > 0, "Invalid blending range for sky texture");
            
            Bitmap bitmap = new Bitmap(skyImage.Width, skyImage.Height * 2);
            System.Drawing.Graphics g = System.Drawing.Graphics.FromImage(bitmap);
            g.Clear(fadeColor);
            
            g.DrawImage(skyImage.Bitmap, new Point(0, 0));

            g.CompositingMode = CompositingMode.SourceOver;
            int alpha = 255;
            int alphaReductionFactor = 255 / Math.Max(Math.Min(blendRange, 255), 1);
            
            for (int y = skyImage.Height - 1; y >= skyImage.Height - blendRange; y--)
            {
                Pen pen = new Pen(Color.FromArgb(alpha, fadeColor));
                g.DrawLine(pen, new Point(0, y), new Point(skyImage.Width - 1, y));
                
                alpha -= alphaReductionFactor;
            }

            return bitmap;
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
            if (!m_allocatedNewTextures) 
                return;
            
            m_lowerTexture.Dispose();
            m_upperTexture.Dispose();
        }
    }
}