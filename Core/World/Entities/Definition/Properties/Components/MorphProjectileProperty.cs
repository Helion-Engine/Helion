using Helion.Resources.Definitions.Decorate.Properties.Enums;

namespace Helion.World.Entities.Definition.Properties.Components;

public class MorphProjectileProperty
{
    public int DurationTicks;
    public string MonsterClass = "";
    public MorphStyle MorphStyle = MorphStyle.None;
    public string MorphFlash = "";
    public string PlayerClass = "";
    public string UnmorphFlash = "";
}
