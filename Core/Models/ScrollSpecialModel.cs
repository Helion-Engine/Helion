using Helion.World;
using Helion.World.Geometry.Sectors;
using Helion.World.Special;
using Helion.World.Special.SectorMovement;
using Helion.World.Special.Specials;

namespace Helion.Models;

public class ScrollSpecialModel : ISpecialModel
{
    public int? SectorId { get; set; }
    public int? LineId { get; set; }
    public int PlaneType { get; set; }
    public int Type { get; set; }
    public bool Front { get; set; }
    public double SpeedX { get; set; }
    public double SpeedY { get; set; }
    public int? AccelSectorId { get; set; }
    public double? AccelSpeedX { get; set; }
    public double? AccelSpeedY { get; set; }
    public double? AccelLastZ { get; set; }
    public double[]? OffsetFrontX { get; set; }
    public double[]? OffsetFrontY { get; set; }
    public double[]? OffsetBackX { get; set; }
    public double[]? OffsetBackY { get; set; }
    public int ScrollFlags { get; set; }

    public ISpecial? ToWorldSpecial(IWorld world)
    {
        Sector? accelSector = null;
        if (AccelSectorId.HasValue && world.IsSectorIdValid(AccelSectorId.Value))
            accelSector = world.Sectors[AccelSectorId.Value];

        if (LineId.HasValue)
        {
            int lineId = LineId.Value;
            if (!world.IsLineIdValid(lineId))
                return null;

            return new ScrollSpecial(world.Lines[lineId], accelSector, this);
        }
        else if (SectorId.HasValue)
        {
            int sectorId = SectorId.Value;
            if (!world.IsSectorIdValid(sectorId))
                return null;

            return new ScrollSpecial(world.Sectors[sectorId].GetSectorPlane((SectorPlaneFace)PlaneType), accelSector, this);
        }

        return new ScrollSpecial(world.Lines[0], Geometry.Vectors.Vec2D.Zero, Maps.Specials.ZDoom.ZDoomLineScroll.MiddleTexture);
    }
}
