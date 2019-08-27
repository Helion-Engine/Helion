using System.Drawing;
using Helion.Graphics.String;
using Helion.Render.OpenGL.Texture.Fonts;
using Helion.Render.Shared.Text;
using Helion.Util.Geometry;

namespace Helion.Render.OpenGL.Texture.Legacy
{
    public class GLLegacyTextDrawCalculator : ITextDrawCalculator
    {
        private readonly LegacyGLTextureManager m_textureManager;
        
        public GLLegacyTextDrawCalculator(LegacyGLTextureManager textureManager)
        {
            m_textureManager = textureManager;
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