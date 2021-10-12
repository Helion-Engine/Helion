using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Render.Legacy.Context;
using Helion.Render.Legacy.Context.Types;
using Helion.Resources;

namespace Helion.Render.Legacy.Texture.Legacy;

public class GLLegacyTexture : GLTexture
{
    public GLLegacyTexture(int textureId, string name, Dimension dimension, Vec2I offset, ResourceNamespace resourceNamespace,
        IGLFunctions functions, TextureTargetType textureType)
        : base(textureId, name, dimension, offset, resourceNamespace, functions, textureType)
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
