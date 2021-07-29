using Helion.Maps.Shared;
using Helion.Maps.Specials;

namespace Helion.World.Geometry.Lines
{
    public class LineFlags
    {
        public LineAutomapFlags Automap;
        public LineBlockFlags Blocking;
        public UnpeggedFlags Unpegged;
        public ActivationType ActivationType;
        public bool BlockSound;
        public bool Repeat;
        public bool MonsterCanActivate;
        public bool Secret => Automap.DrawAsOneSided;

        public LineFlags(MapLineFlags flags)
        {
            Automap.AlwaysDraw = flags.AlwaysDrawAutomap;
            Automap.NeverDraw = flags.NoDrawAutomap;
            Automap.DrawAsOneSided = flags.DrawAsOneSidedAutomap;

            Blocking.Hitscan = false;
            Blocking.Monsters = flags.BlockPlayersAndMonsters || flags.BlockMonsters;
            Blocking.Players = flags.BlockPlayersAndMonsters;
            Blocking.Projectiles = false;
            
            Unpegged.Upper = flags.UpperUnpegged;
            Unpegged.Lower = flags.LowerUnpegged;
            
            BlockSound = flags.BlockSound;

            ActivationType = flags.ActivationType;
            Repeat = flags.RepeatSpecial;
        }
    }
}