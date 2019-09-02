using System.Collections.Generic;
using Helion.Resources.Definitions.Animdefs.Textures;

namespace Helion.Resources.Definitions.Animdefs.Switches
{
    public class AnimatedSwitch
    {
        public readonly string StartTexture;
        public readonly SwitchType SwitchType;
        public readonly IList<AnimatedTextureComponent> Components = new List<AnimatedTextureComponent>();
        public string? Sound;

        public AnimatedSwitch(string texture, SwitchType switchType)
        {
            StartTexture = texture.ToUpper();
            SwitchType = switchType;
        }

        public override string ToString() => $"{StartTexture} ({SwitchType}: components={Components.Count})";
    }
}