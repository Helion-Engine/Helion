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
    public bool TwoSided;
    public readonly bool Secret => Automap.DrawAsOneSided;

    public LineFlags(MapLineFlags flags)
    {
        Automap.AlwaysDraw = flags.AlwaysDrawAutomap;
        Automap.NeverDraw = flags.NoDrawAutomap;
        Automap.DrawAsOneSided = flags.DrawAsOneSidedAutomap;

        Blocking.Hitscan = flags.BlockHitscan;
        Blocking.Monsters = flags.BlockPlayersAndMonsters || flags.BlockMonsters;
        Blocking.Players = flags.BlockPlayersAndMonsters || flags.BlockPlayers;
        Blocking.PlayersMbf21 = flags.BlockPlayersMbf21;
        Blocking.LandMonstersMbf21 = flags.BlockLandMonstersMbf21;
        Blocking.Projectiles = false;
        Blocking.LandMonsters = flags.BlockLandMonsters;
        Blocking.FloatMonsters = flags.BlockFloatMonsters;

        Unpegged.Upper = flags.UpperUnpegged;
        Unpegged.Lower = flags.LowerUnpegged;

        BlockSound = flags.BlockSound;

        Activations = flags.Activations;
        Repeat = flags.RepeatSpecial;
        PassThrough = flags.PassThrough;
        TwoSided = flags.TwoSided;
    }

    public override readonly bool Equals(object? obj)
    {
        if (obj is not LineFlags lineFlags)
            return false;

        return lineFlags.Automap.Equals(Automap) &&
            lineFlags.Blocking.Equals(Blocking) &&
            lineFlags.Unpegged.Equals(Unpegged) &&
            lineFlags.Activations == Activations &&
            lineFlags.BlockSound == BlockSound &&
            lineFlags.Repeat == Repeat &&
            lineFlags.PassThrough == PassThrough &&
            lineFlags.TwoSided == TwoSided;
    }

    public override readonly int GetHashCode()
    {
        return base.GetHashCode();
    }
}
