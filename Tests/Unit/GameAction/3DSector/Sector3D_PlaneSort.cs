
using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Geometry.Sectors;
using Helion.World.Impl.SinglePlayer;
using Xunit;
using System.Linq;

namespace Helion.Tests.Unit.GameAction._3DSector;

[Collection("GameActions")]
public class Sector3D_PlaneSort
{
    record struct PlaneData(double Z, PlaneFace3D Face, Sector LightSector, bool Ignore = false);
    private readonly SinglePlayerWorld World;

    public Sector3D_PlaneSort()
    {
        World = WorldAllocator.LoadMap("Resources/sector3d-map.zip", "sector3d-map.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact(DisplayName = "Light transfer with non-solid plane sort + overlapping light transfer")]
    public void LightTransferWithNonSolidSort()
    {
        var sector = GameActions.GetSector(World, 90);

        // Sector 83 w/light transfer is 32 -> 64 and overlaps the other sectors (32 -> 48, 24 -> 32, 0 -> 16)
        AssertPlanes3D(sector, 
            new PlaneData(128, PlaneFace3D.Top, sector),
            new PlaneData(64, PlaneFace3D.Top, sector),
            new PlaneData(48, PlaneFace3D.Bottom, GameActions.GetSector(World, 83)),
            new PlaneData(48, PlaneFace3D.Top, GameActions.GetSector(World, 81)),
            new PlaneData(32, PlaneFace3D.Bottom, GameActions.GetSector(World, 81)),
            new PlaneData(32, PlaneFace3D.Top, GameActions.GetSector(World, 82)),
            new PlaneData(24, PlaneFace3D.Bottom, GameActions.GetSector(World, 82)),
            new PlaneData(16, PlaneFace3D.Top, GameActions.GetSector(World, 80)),
            new PlaneData(0, PlaneFace3D.Bottom, GameActions.GetSector(World, 80)),
            new PlaneData(0, PlaneFace3D.Bottom, GameActions.GetSector(World, 80))
            );
    }

    [Fact(DisplayName = "Light transfer with solid and non-solid plane sort")]
    public void LightTransferWithSolidSort()
    {
        var sector = GameActions.GetSector(World, 89);
        sector.Sectors3D.Length.Should().Be(6);
        sector.SectorPlanes3D.Length.Should().Be(14);
    }

    [Fact(DisplayName = "3D sector completely inside sort")]
    public void SectorCompletelyInsideSort()
    {
        var sector = GameActions.GetSector(World, 89);
        sector.Sectors3D.Length.Should().Be(6);
        sector.SectorPlanes3D.Length.Should().Be(14);
    }

    [Fact(DisplayName = "Complex 3D sectors sort")]
    public void ComplexSort()
    {

    }

    private static void AssertPlanes3D(Sector sector, params PlaneData[] planes)
    {
        //sector.SectorPlanes3D.Length.Should().Be(planes.Length);
        //sector.Sectors3D.Length.Should().Be((planes.Length - 2) / 2);

        for (int i = 0; i < planes.Length; i++)
        {
            var plane = planes[i];
            var plane3D = sector.SectorPlanes3D[i];
            plane3D.GetZ().Should().Be(plane.Z);
            plane3D.Face.Should().Be(plane.Face);
            plane3D.LightSector.Should().Be(plane.LightSector);
            plane3D.Ignore.Should().Be(plane.Ignore);

            if (plane3D.Sector3D == null)
                continue;

            var light = plane3D.Face == PlaneFace3D.Bottom ? plane3D.Sector3D.LightBottom : plane3D.Sector3D.LightTop;
            light.Should().Be(plane.LightSector);
        }
    }
}
