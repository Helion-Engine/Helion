using Helion.Maps.Specials;
using Helion.World.Geometry.Lines;
using Helion.World.Geometry.Sectors;
using Helion.World.Special.Specials;

namespace Helion.World.Special.SectorMovement;

public readonly struct SectorMoveData
{
    public readonly SectorPlaneFace SectorMoveType;
    public readonly MoveRepetition MoveRepetition;
    public readonly double Speed;
    public readonly double ReturnSpeed;
    public readonly int Delay;
    public readonly CrushData? Crush;
    public readonly int? FloorChangeTextureHandle;
    public readonly int? CeilingChangeTextureHandle;
    public readonly SectorDamageSpecial? DamageSpecial;
    public readonly MoveDirection StartDirection;
    public readonly SectorMoveFlags Flags;
    public readonly SectorEffect? SectorEffect;
    public readonly InstantKillEffect? KillEffect;
    public readonly int LightTag;

    public const int InstantToggleSpeed = int.MaxValue;

    public SectorMoveData(SectorPlaneFace moveType, MoveDirection startDirection, MoveRepetition repetition,
        double speed, int delay, CrushData? crush = null,
        int? floorChangeTextureHandle = null,
        int? ceilingChangeTextureHandle = null,
        SectorDamageSpecial? damageSpecial = null,
        double? returnSpeed = null,
        SectorMoveFlags flags = SectorMoveFlags.None,
        SectorEffect? sectorEffect = null,
        InstantKillEffect? killEffect = null,
        int lightTag = 0)
    {
        SectorMoveType = moveType;
        StartDirection = startDirection;
        MoveRepetition = repetition;
        Speed = speed;
        Delay = delay;
        Crush = crush;
        FloorChangeTextureHandle = floorChangeTextureHandle;
        CeilingChangeTextureHandle = ceilingChangeTextureHandle;
        DamageSpecial = damageSpecial;
        ReturnSpeed = returnSpeed ?? speed;
        Flags = flags;
        SectorEffect = sectorEffect;
        KillEffect = killEffect;
        LightTag = lightTag;
    }
}
