using Helion.Resource.Definitions.Decorate.Properties.Enums;
using Helion.World.Entities.Definition.Properties.Components;

namespace Helion.Worlds.Entities.Definition.Properties.Components
{
    public class PowerupProperty
    {
        public PowerupColor? Color;
        public PowerupColorMap? Colormap;
        public int Duration;
        public PowerupModeType Mode = PowerupModeType.None;
        public int Strength;
        public string Type = "";
    }
}