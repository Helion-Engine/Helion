using Helion.Resources.Definitions.Decorate.Properties.Enums;

namespace Helion.World.Entities.Definition.Properties.Components;

public class MorphProjectileProperty
{
    public int DurationTicks;
    public string MonsterClass = string.Empty;
    public MorphStyle MorphStyle = MorphStyle.None;
    public string MorphFlash = string.Empty;
    public string PlayerClass = string.Empty;
    public string UnmorphFlash = string.Empty;
}
