namespace Helion.World.Special.SectorMovement;

public enum SectorDest
{
    None,
    LowestAdjacentFloor,
    LowestAdjacentCeiling,
    HighestAdjacentFloor,
    HighestAdjacentCeiling,
    NextLowestFloor,
    NextLowestCeiling,
    NextHighestFloor,
    NextHighestCeiling,
    Floor,
    Ceiling,
    ShortestLowerTexture,
    ShortestUpperTexture,
}
