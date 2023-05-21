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

    public const int InstantToggleSpeed = int.MaxValue;

    public SectorMoveData(SectorPlaneFace moveType, MoveDirection startDirection, MoveRepetition repetition,
        double speed, int delay, CrushData? crush = null,
        int? floorChangeTextureHandle = null,
        int? ceilingChangeTextureHandle = null,
        SectorDamageSpecial? damageSpecial = null,
        double? returnSpeed = null,
        SectorMoveFlags flags = SectorMoveFlags.None)
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
    }
}
