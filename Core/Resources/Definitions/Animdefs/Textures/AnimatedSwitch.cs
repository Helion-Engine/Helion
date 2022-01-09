using System.Collections.Generic;
using Helion.Resources.IWad;
using Helion.Util;

namespace Helion.Resources.Definitions.Animdefs.Textures;

public class AnimatedSwitch
{
    public readonly string Texture;
    public readonly IList<AnimatedTextureComponent> On = new List<AnimatedTextureComponent>();
    public readonly IList<AnimatedTextureComponent> Off = new List<AnimatedTextureComponent>();
    public string? Sound;
    public int StartTextureIndex;
    public IWadBaseType IWad;

    public AnimatedSwitch(string texture, IWadBaseType iwad = IWadBaseType.None)
    {
        Texture = texture;
        IWad = iwad;
   }

    public bool IsMatch(int textureIndex)
    {
        if (StartTextureIndex == Constants.NoTextureIndex || On[0].TextureIndex == Constants.NoTextureIndex)
            return false;

        return StartTextureIndex == textureIndex || On[0].TextureIndex == textureIndex;
    }

    public int GetOpposingTexture(int textureIndex)
    {
        if (StartTextureIndex != textureIndex)
            return StartTextureIndex;
        return On[0].TextureIndex;
    }

    public override string ToString() => $"{Texture} (On count: {On.Count}, Off count: {Off.Count})";
}
