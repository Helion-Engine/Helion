using Helion.Maps.Doom.Components.Types;
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

        public LineFlags(DoomLineFlags flags)
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

            // TODO
            ActivationType = ActivationType.None;
            Repeat = false;
        }
    }
}