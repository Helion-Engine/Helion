using Helion.Maps.Specials;
using Helion.World.Geometry.Sectors;
using Helion.World.Special.Specials;

namespace Helion.World.Special.SectorMovement;

public struct SectorMoveData
{
    public SectorPlaneFace SectorMoveType;
    public MoveRepetition MoveRepetition;
    public double Speed;
    public double ReturnSpeed;
    public int Delay;
    public CrushData? Crush;
    public int? FloorChangeTextureHandle;
    public int? CeilingChangeTextureHandle;
    public SectorDamageSpecial? DamageSpecial;
    public MoveDirection StartDirection;
    public SectorMoveFlags Flags;
    public SectorEffect? SectorEffect;
    public InstantKillEffect? KillEffect;
    public Sector3D? Sector3D;
    public int LightTag;

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
