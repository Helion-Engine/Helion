using Helion.Resources.Definitions.Decorate.Properties.Enums;

namespace Helion.World.Entities.Definition.Properties.Components;

public class PowerupProperty
{
    public PowerupColor? Color;
    public PowerupColorMap? Colormap;
    public int Duration;
    public PowerupModeType Mode = PowerupModeType.None;
    public int Strength;
    public string Type = string.Empty;
}
