using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Resources.Definitions.StatusBar;
using Helion.Util.Container;
using System.Collections.Generic;

namespace Helion.Layer.Worlds.StatusBar;

internal sealed class UniqueArrayTextures
{
    public DynamicArray<IArrayTexture> ArrayTextures = new(512);
    public DynamicArray<StatusBarBaseDef> FontItems = new(32);
    public HashSet<string> UniqueNames = new(512);
    public HashSet<string> UniqueFontNames = new(32);

    public void AddBaseDef(StatusBarBaseDef def)
    {
        if (def != null && def.IsFont())
        {
            var font = def.GetFont();
            if (!string.IsNullOrEmpty(font) && UniqueFontNames.Add(font))
                FontItems.Add(def);
        }
    }

    public void Add(IArrayTexture arrayTexture)
    {
        if (UniqueNames.Add(arrayTexture.FetchTextureName))
            ArrayTextures.Add(arrayTexture);
    }
}
