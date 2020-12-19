using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;
using Helion.Resource.Textures;
using Helion.Util.Geometry;

namespace Helion.Render.OpenGL.Textures.Legacy
{
    public class GLLegacyTexture : GLTexture
    {
        public GLLegacyTexture(int textureId, string name, Dimension dimension, IGLFunctions functions,
            TextureTargetType textureType, Texture texture) :
            base(textureId, name, dimension, functions, textureType, texture)
        {
        }

        public void Bind()
        {
            gl.BindTexture(TextureType, TextureId);
        }

        public void Unbind()
        {
            gl.BindTexture(TextureType, 0);
        }
    }
}