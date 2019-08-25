using Helion.Render.OpenGL.Texture.Fonts;

namespace Helion.Render.OpenGL.Texture.Legacy
{
    public class GLLegacyFontTexture : GLFontTexture<GLLegacyTexture>
    {
        public override GLLegacyTexture Texture { get; }
        public override GLFontMetrics Metrics { get; }

        public GLLegacyFontTexture(GLLegacyTexture texture, GLFontMetrics metrics)
        {
            Texture = texture;
            Metrics = metrics;
        }
    }
}