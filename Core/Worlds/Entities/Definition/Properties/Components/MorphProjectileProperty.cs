using Helion.Resource.Definitions.Decorate.Properties.Enums;

namespace Helion.Worlds.Entities.Definition.Properties.Components
{
    public class MorphProjectileProperty
    {
        public int DurationTicks;
        public string MonsterClass = "";
        public MorphStyle MorphStyle = MorphStyle.None;
        public string MorphFlash = "";
        public string PlayerClass = "";
        public string UnmorphFlash = "";
    }
}