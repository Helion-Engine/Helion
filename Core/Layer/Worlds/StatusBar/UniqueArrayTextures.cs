using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Util.Container;
using System.Collections.Generic;

namespace Helion.Layer.Worlds.StatusBar;

internal sealed class UniqueArrayTextures
{
    public DynamicArray<IArrayTexture> ArrayTextures = new(512);
    public HashSet<string> UniqueNames = new(512);

    public void Add(IArrayTexture arrayTexture)
    {
        if (UniqueNames.Add(arrayTexture.FetchTextureName))
            ArrayTextures.Add(arrayTexture);
    }
}
