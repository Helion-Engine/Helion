using Helion.Graphics;
using Helion.Util;

namespace Helion.Resources.Textures;

public record Texture(int Index, string Name, Image Image, ResourceNamespace Namespace, object? RenderStore = null)
{
    public bool IsNullTexture => Index == Constants.NoTextureIndex;
}

