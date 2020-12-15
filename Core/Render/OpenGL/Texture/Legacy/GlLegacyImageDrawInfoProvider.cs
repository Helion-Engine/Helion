using System;
using System.Drawing;
using Helion.Graphics.String;
using Helion.Render.OpenGL.Texture.Fonts;
using Helion.Render.Shared;
using Helion.Resources;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;

namespace Helion.Render.OpenGL.Texture.Legacy
{
    public class GlLegacyImageDrawInfoProvider : IImageDrawInfoProvider
    {
        private readonly LegacyGLTextureManager m_textureManager;
        
        public GlLegacyImageDrawInfoProvider(LegacyGLTextureManager textureManager)
        {
            m_textureManager = textureManager;
        }

        public bool ImageExists(string image)
        {
            return m_textureManager.Contains(image);
        }

        public Dimension GetImageDimension(string image, Namespace resourceNamespace = Namespace.Global)
        {
            m_textureManager.TryGet(image, resourceNamespace, out GLLegacyTexture texture);
            return texture.Dimension;
        }

        public Vec2I GetImageOffset(string image, Namespace resourceNamespace = Namespace.Global)
        {
            m_textureManager.TryGet(image, resourceNamespace, out GLLegacyTexture texture);
            return texture.Metadata.Offset;
        }

        public int GetFontHeight(string font) => m_textureManager.GetFont(font).Metrics.MaxHeight;
        
        public Dimension GetDrawArea(ColoredString str, string font, int fontSize, int maxWidth, bool wrap)
        {
            GLFontTexture<GLLegacyTexture> fontTexture = m_textureManager.GetFont(font);

            float scaleFactor = (float)fontSize / GetFontHeight(font);
            int finalWidth = 0;
            int currentWidth = 0;
            bool notFirstChar = false;
            int rowsOfCharacters = 1;

            foreach (ColoredChar c in str)
            {
                int charWidth = (int)(fontTexture[c.Character].Dimension.Width * scaleFactor);

                if (notFirstChar && currentWidth + charWidth >= maxWidth)
                {
                    finalWidth = Math.Max(finalWidth, currentWidth);
                    currentWidth = 0;
                    rowsOfCharacters++;
                }
                
                currentWidth += charWidth;
                notFirstChar = true;
            }
            
            finalWidth = Math.Max(finalWidth, currentWidth);
            int finalHeight = (int)(rowsOfCharacters * fontTexture.Metrics.MaxHeight * scaleFactor);
            
            return new Dimension(finalWidth, finalHeight);
        }

        public Rectangle GetDrawArea(ColoredString str, string font, Vec2I topLeft, int? fontSize = null)
        {
            GLFontTexture<GLLegacyTexture> fontTexture = m_textureManager.GetFont(font);

            int width = 0;
            foreach (var c in str)
                width += fontTexture[c.Character].Width;
            return new Rectangle(topLeft.X, topLeft.Y, width, fontTexture.Metrics.MaxHeight);
        }
    }
}