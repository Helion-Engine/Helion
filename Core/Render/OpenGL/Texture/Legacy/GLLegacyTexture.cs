using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Resources;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Texture.Legacy;

public class GLLegacyTexture : GLTexture
{
    public TextureFlags Flags;
    public int ArrayIndex;
    public bool IsArray => ParentArrayTexture != null;
    public GLLegacyTexture? ParentArrayTexture;

    public GLLegacyTexture(int textureId, string name, Dimension dimension, Vec2I offset, ResourceNamespace ns, TextureTarget target, 
        int transparentPixelCount, int blankRowsFromTop = 0, int blankRowsFromBottom = 0, bool ownsTexture = true, TextureContext textureContext = TextureContext.Default)
        : base(textureId, name, dimension, offset, ns, TextureTarget.Texture2DArray, transparentPixelCount, blankRowsFromTop, blankRowsFromBottom, ownsTexture, textureContext)
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

    public override string ToString() => $"{TextureId}:{Name} [{Dimension}]";
}
