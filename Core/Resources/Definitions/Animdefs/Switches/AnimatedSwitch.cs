using System.Collections.Generic;
using Helion.Resources.Definitions.Animdefs.Textures;
using Helion.Util;

namespace Helion.Resources.Definitions.Animdefs.Switches
{
    public class AnimatedSwitch
    {
        public readonly string StartTexture;
        public readonly SwitchType SwitchType;
        public readonly IList<AnimatedTextureComponent> Components = new List<AnimatedTextureComponent>();
        public string? Sound;
        public int StartTextureIndex;

        public AnimatedSwitch(string texture, SwitchType switchType)
        {
            StartTexture = texture.ToUpper();
            SwitchType = switchType;
        }

        public bool IsMatch(int textureIndex)
        {
            if (StartTextureIndex == Constants.NoTextureIndex || Components[0].TextureIndex == Constants.NoTextureIndex)
                return false;

            return StartTextureIndex == textureIndex || Components[0].TextureIndex == textureIndex;
        }

        public int GetOpposingTexture(int textureIndex)
        {
            if (StartTextureIndex != textureIndex) 
                return StartTextureIndex;
            return Components[0].TextureIndex;
        }

        public override string ToString() => $"{StartTexture} ({SwitchType}: components={Components.Count})";
    }
}