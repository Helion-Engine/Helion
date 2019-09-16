using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;
using Helion.Util.Geometry;

namespace Helion.Render.OpenGL.Texture.Legacy
{
    public class GLLegacyTexture : GLTexture
    {
        public GLLegacyTexture(int textureId, string name, Dimension dimension, IGLFunctions functions, TextureTargetType textureType) : 
            base(textureId, name, dimension, functions, textureType)
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