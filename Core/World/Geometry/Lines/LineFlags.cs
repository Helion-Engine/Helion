using Helion.Maps.Shared;
using Helion.Maps.Specials;

namespace Helion.World.Geometry.Lines;

public struct LineFlags
{
    public LineAutomapFlags Automap;
    public LineBlockFlags Blocking;
    public UnpeggedFlags Unpegged;
    public LineActivations Activations;
    public bool BlockSound;
    public bool Repeat;
    public bool PassThrough;
    public bool Secret => Automap.DrawAsOneSided;

    public LineFlags(MapLineFlags flags)
    {
        Automap.AlwaysDraw = flags.AlwaysDrawAutomap;
        Automap.NeverDraw = flags.NoDrawAutomap;
        Automap.DrawAsOneSided = flags.DrawAsOneSidedAutomap;

        Blocking.Hitscan = false;
        Blocking.Monsters = flags.BlockPlayersAndMonsters || flags.BlockMonsters;
        Blocking.Players = flags.BlockPlayersAndMonsters || flags.BlockPlayers;
        Blocking.PlayersMbf21 = flags.BlockPlayersMbf21;
        Blocking.LandMonstersMbf21 = flags.BlockLandMonstersMbf21;
        Blocking.Projectiles = false;

        Unpegged.Upper = flags.UpperUnpegged;
        Unpegged.Lower = flags.LowerUnpegged;

        BlockSound = flags.BlockSound;

        Activations = flags.Activations;
        Repeat = flags.RepeatSpecial;
        PassThrough = flags.PassThrough;
    }

    public override bool Equals(object? obj)
    {
        if (obj is not LineFlags lineFlags)
            return false;

        return lineFlags.Automap.Equals(Automap) &&
            lineFlags.Blocking.Equals(Blocking) &&
            lineFlags.Unpegged.Equals(Unpegged) &&
            lineFlags.Activations == Activations &&
            lineFlags.BlockSound == BlockSound &&
            lineFlags.Repeat == Repeat &&
            lineFlags.PassThrough == PassThrough;
    }

    public override int GetHashCode()
    {
        return base.GetHashCode();
    }
}
