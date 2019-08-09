using Helion.Render.OpenGL.Context;
using Helion.Util.Geometry;

namespace Helion.Render.OpenGL.Texture.Bindless
{
    public class GLBindlessTexture : GLTexture
    {
        public GLBindlessTexture(int id, int textureId, Dimension dimension, GLFunctions functions) : 
            base(id, textureId, dimension, functions)
        {
        }
    }
}