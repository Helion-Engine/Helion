using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Render.Common.Textures;

namespace Helion.Render.OpenGL.Textures
{
    /// <summary>
    /// A handle to a texture. Such a handle may either be the entire texture,
    /// or is a component of a larger texture (such as in a texture atlas).
    /// </summary>
    public class GLTextureHandleHandle : IRenderableTextureHandle
    {
        public int Index { get; }
        public Box2I Area { get; }
        public Box2F UV { get; }
        public Dimension Dimension => Area.Dimension;
        public readonly GLTexture Texture;

        public GLTextureHandleHandle(int index, Box2I area, Box2F uv, GLTexture texture)
        {
            Index = index;
            Texture = texture;
            Area = area;
            UV = uv;
        }
    }
}
