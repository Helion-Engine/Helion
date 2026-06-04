using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Resources;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Texture.Legacy;

public class GLLegacyTexture : GLTexture
{
    public TextureFlags Flags;

    public GLLegacyTexture(int textureId, string name, Dimension dimension, Vec2I offset, ResourceNamespace ns, TextureTarget target, 
        int transparentPixelCount, int blankRowsFromTop = 0, int blankRowsFromBottom = 0)
        : base(textureId, name, dimension, offset, ns, target, transparentPixelCount, blankRowsFromTop, blankRowsFromBottom)
    {
    }

    public void Bind()
    {
        GL.BindTexture(Target, TextureId);
    }

    public void Unbind()
    {
        GL.BindTexture(Target, 0);
    }

    public override string ToString() => $"{TextureId}:{Name}";
}
