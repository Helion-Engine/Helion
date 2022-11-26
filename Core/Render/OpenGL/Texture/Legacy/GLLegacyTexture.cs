using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Render.OpenGL.Context;
using Helion.Resources;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Texture.Legacy;

public class GLLegacyTexture : GLTexture
{
    public GLLegacyTexture(int textureId, string name, Dimension dimension, Vec2I offset, ResourceNamespace ns, TextureTarget target)
        : base(textureId, name, dimension, offset, ns, target)
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
}
