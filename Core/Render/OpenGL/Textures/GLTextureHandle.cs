using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Render.Common;

namespace Helion.Render.OpenGL.Textures
{
    public class GLTextureHandle : IRenderableTexture
    {
        public readonly int Index;
        public readonly GLTexture Texture;
        public readonly Box2F UV;
        public readonly Dimension Dimension;

        public GLTextureHandle(int index, GLTexture texture, Box2F uv, Dimension dimension)
        {
            Index = index;
            Texture = texture;
            UV = uv;
            Dimension = dimension;
        }
    }
}
