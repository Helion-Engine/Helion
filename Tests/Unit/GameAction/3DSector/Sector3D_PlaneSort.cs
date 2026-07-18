
using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Geometry.Sectors;
using Helion.World.Impl.SinglePlayer;
using System.Linq;
using Xunit;
using Xunit.Sdk;

namespace Helion.Tests.Unit.GameAction._3DSector;

[Collection("GameActions")]
public class Sector3D_PlaneSort
{
    record struct PlaneData(double Z, PlaneFace3D Face, Sector LightSector, bool NoRenderWall = false, SectorPlanes RenderPlanes = SectorPlanes.Floor | SectorPlanes.Ceiling);
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
            new PlaneData(128, PlaneFace3D.Top, sector), // 0
            new PlaneData(64, PlaneFace3D.Top, sector, RenderPlanes: SectorPlanes.Ceiling), // 1
            new PlaneData(48, PlaneFace3D.Bottom, GameActions.GetSector(World, 83), RenderPlanes: SectorPlanes.Ceiling), // 2
            new PlaneData(48, PlaneFace3D.Top, GameActions.GetSector(World, 83)), // 3
            new PlaneData(32, PlaneFace3D.Bottom, GameActions.GetSector(World, 81)), // 4
            new PlaneData(32, PlaneFace3D.Top, GameActions.GetSector(World, 81)), // 5
            new PlaneData(24, PlaneFace3D.Bottom, GameActions.GetSector(World, 82)), // 6
            new PlaneData(16, PlaneFace3D.Top, GameActions.GetSector(World, 82), RenderPlanes: SectorPlanes.Ceiling), // 7
            new PlaneData(0, PlaneFace3D.Bottom, GameActions.GetSector(World, 82), RenderPlanes: SectorPlanes.Ceiling), // 8
            new PlaneData(0, PlaneFace3D.Bottom, GameActions.GetSector(World, 82)) // 9
            );
    }

    [Fact(DisplayName = "Light transfer with solid and non-solid plane sort")]
    public void LightTransferWithSolidSort()
    {
        var sector = GameActions.GetSector(World, 89);
        AssertPlanes3D(sector,
            new PlaneData(128, PlaneFace3D.Top, sector), // 0
            new PlaneData(80, PlaneFace3D.Top, GameActions.GetSector(World, 89)), // 1
            new PlaneData(64, PlaneFace3D.Bottom, GameActions.GetSector(World, 83)), // 2
            new PlaneData(64, PlaneFace3D.Top, GameActions.GetSector(World, 86), RenderPlanes: SectorPlanes.Ceiling), // 3
            new PlaneData(48, PlaneFace3D.Bottom, GameActions.GetSector(World, 83), RenderPlanes: SectorPlanes.Ceiling), // 4
            new PlaneData(48, PlaneFace3D.Top, GameActions.GetSector(World, 83)), // 5
            new PlaneData(32, PlaneFace3D.Bottom, GameActions.GetSector(World, 81)), // 6
            new PlaneData(32, PlaneFace3D.Top, GameActions.GetSector(World, 81)), // 7
            new PlaneData(24, PlaneFace3D.Bottom, GameActions.GetSector(World, 82)), // 8
            new PlaneData(16, PlaneFace3D.Top, GameActions.GetSector(World, 82), RenderPlanes: SectorPlanes.Ceiling), // 9
            new PlaneData(0, PlaneFace3D.Bottom, GameActions.GetSector(World, 82), RenderPlanes: SectorPlanes.Ceiling), // 10
            new PlaneData(0, PlaneFace3D.Bottom, GameActions.GetSector(World, 82)) // 11
            );
    }

    [Fact(DisplayName = "3D sector completely inside sort")]
    public void SectorCompletelyInsideSort()
    {
        var sector = GameActions.GetSector(World, 94);
        AssertPlanes3D(sector,
            new PlaneData(128, PlaneFace3D.Top, sector),
            new PlaneData(112, PlaneFace3D.Top, GameActions.GetSector(World, 94)),
            new PlaneData(108, PlaneFace3D.Top, GameActions.GetSector(World, 92)),
            new PlaneData(88, PlaneFace3D.Bottom, GameActions.GetSector(World, 91)),
            new PlaneData(80, PlaneFace3D.Bottom, GameActions.GetSector(World, 91)),
            new PlaneData(0, PlaneFace3D.Bottom, GameActions.GetSector(World, 91))
            );
    }

    /*
    [Fact(DisplayName = "Complex 3D sectors sort with overlapping render styles")]
    public void ComplexSort()
    {
        var sector = GameActions.GetSector(World, 139);
        AssertPlanes3D(sector,
            new PlaneData(1152, PlaneFace3D.Top, sector),                          //0
            new PlaneData(1016, PlaneFace3D.Top, GameActions.GetSector(World, 139), NoRenderWall: true), //1
            new PlaneData(1015, PlaneFace3D.Bottom, GameActions.GetSector(World, 137)), //2
            new PlaneData(992, PlaneFace3D.Bottom, GameActions.GetSector(World, 136)), //3
            new PlaneData(992, PlaneFace3D.Top, GameActions.GetSector(World, 137)), //4
            new PlaneData(992, PlaneFace3D.Bottom, GameActions.GetSector(World, 136)), //5
            new PlaneData(880, PlaneFace3D.Top, GameActions.GetSector(World, 136)), //6
            new PlaneData(880, PlaneFace3D.Bottom, GameActions.GetSector(World, 129)), //7
            new PlaneData(760, PlaneFace3D.Top, GameActions.GetSector(World, 129), NoRenderWall: true), //8
            new PlaneData(759, PlaneFace3D.Bottom, GameActions.GetSector(World, 157)), //9
            new PlaneData(736, PlaneFace3D.Bottom, GameActions.GetSector(World, 156)), //10
            new PlaneData(736, PlaneFace3D.Top, GameActions.GetSector(World, 157)), //11
            new PlaneData(736, PlaneFace3D.Bottom, GameActions.GetSector(World, 156)), //12
            new PlaneData(624, PlaneFace3D.Top, GameActions.GetSector(World, 156)), //13
            new PlaneData(624, PlaneFace3D.Bottom, GameActions.GetSector(World, 159)), //14
            new PlaneData(504, PlaneFace3D.Top, GameActions.GetSector(World, 159), NoRenderWall: true, RenderPlanes: SectorPlanes.Ceiling), //15
            new PlaneData(504, PlaneFace3D.Bottom, GameActions.GetSector(World, 145), RenderPlanes: SectorPlanes.Ceiling), //16
            new PlaneData(504, PlaneFace3D.Top, GameActions.GetSector(World, 145)), //17
            new PlaneData(480, PlaneFace3D.Bottom, GameActions.GetSector(World, 147)), //18
            new PlaneData(480, PlaneFace3D.Top, GameActions.GetSector(World, 148)), // 19
            new PlaneData(480, PlaneFace3D.Bottom, GameActions.GetSector(World, 147)), //20
            new PlaneData(368, PlaneFace3D.Top, GameActions.GetSector(World, 147)), //21
            new PlaneData(368, PlaneFace3D.Bottom, GameActions.GetSector(World, 150)), //22
            new PlaneData(256, PlaneFace3D.Top, GameActions.GetSector(World, 150)), //23
            new PlaneData(129, PlaneFace3D.Bottom, GameActions.GetSector(World, 150)), //24
            new PlaneData(128, PlaneFace3D.Top, GameActions.GetSector(World, 150), NoRenderWall: true), //25
            new PlaneData(127, PlaneFace3D.Bottom, GameActions.GetSector(World, 103)), //26
            new PlaneData(104, PlaneFace3D.Bottom, GameActions.GetSector(World, 102)), //27
            new PlaneData(104, PlaneFace3D.Top, GameActions.GetSector(World, 103)), //28
            new PlaneData(104, PlaneFace3D.Bottom, GameActions.GetSector(World, 102)), //29
            new PlaneData(-8, PlaneFace3D.Top, GameActions.GetSector(World, 102)), //30
            new PlaneData(-8, PlaneFace3D.Bottom, GameActions.GetSector(World, 105)), //31
            new PlaneData(-64, PlaneFace3D.Bottom, GameActions.GetSector(World, 105))  //32
            );
    }
    */

    [Fact(DisplayName = "3D sector light transfer pool with floating solid sector")]
    public void LightTransferLavaPool()
    {
        var lavaSector = GameActions.GetSector(World, 167);
        AssertPlanes3D(lavaSector,
            new PlaneData(512, PlaneFace3D.Top, lavaSector), // 0
            new PlaneData(0, PlaneFace3D.Top, lavaSector), // 1
            new PlaneData(-16, PlaneFace3D.Bottom, GameActions.GetSector(World, 171)), // 2
            new PlaneData(-16, PlaneFace3D.Top, GameActions.GetSector(World, 171)), // 3
            new PlaneData(-32, PlaneFace3D.Bottom, GameActions.GetSector(World, 170)), // 4
            new PlaneData(-32, PlaneFace3D.Top, GameActions.GetSector(World, 170)), // 5
            new PlaneData(-48, PlaneFace3D.Bottom, GameActions.GetSector(World, 169)), // 6
            new PlaneData(-48, PlaneFace3D.Top, GameActions.GetSector(World, 169), RenderPlanes: SectorPlanes.Ceiling), // 7
            new PlaneData(-56, PlaneFace3D.Bottom, GameActions.GetSector(World, 168), RenderPlanes: SectorPlanes.Ceiling), // 8
            new PlaneData(-56, PlaneFace3D.Top, GameActions.GetSector(World, 168), RenderPlanes: SectorPlanes.Ceiling), // 9
            new PlaneData(-128, PlaneFace3D.Bottom, GameActions.GetSector(World, 168), RenderPlanes: SectorPlanes.Ceiling), // 10
            new PlaneData(-128, PlaneFace3D.Bottom, GameActions.GetSector(World, 168)) // 11
            );

        var floatingSector = GameActions.GetSector(World, 176);
        AssertPlanes3D(floatingSector,
            new PlaneData(512, PlaneFace3D.Top, floatingSector), // 0
            new PlaneData(69, PlaneFace3D.Top, floatingSector), // 1
            new PlaneData(0, PlaneFace3D.Top, GameActions.GetSector(World, 177)), // 2
            new PlaneData(-16, PlaneFace3D.Bottom, GameActions.GetSector(World, 171)), // 3
            new PlaneData(-16, PlaneFace3D.Top, GameActions.GetSector(World, 171)), // 4
            new PlaneData(-32, PlaneFace3D.Bottom, GameActions.GetSector(World, 170)), // 5
            new PlaneData(-32, PlaneFace3D.Top, GameActions.GetSector(World, 170)), // 6
            new PlaneData(-48, PlaneFace3D.Bottom, GameActions.GetSector(World, 169)), // 7
            new PlaneData(-48, PlaneFace3D.Top, GameActions.GetSector(World, 169), RenderPlanes: SectorPlanes.Ceiling), // 8
            new PlaneData(-56, PlaneFace3D.Bottom, GameActions.GetSector(World, 168), RenderPlanes: SectorPlanes.Ceiling), // 9
            new PlaneData(-80, PlaneFace3D.Bottom, GameActions.GetSector(World, 168)), // 10
            new PlaneData(-80, PlaneFace3D.Top, GameActions.GetSector(World, 168), RenderPlanes: SectorPlanes.Ceiling), // 11
            new PlaneData(-128, PlaneFace3D.Bottom, GameActions.GetSector(World, 168), RenderPlanes: SectorPlanes.Ceiling), // 12
            new PlaneData(-128, PlaneFace3D.Bottom, GameActions.GetSector(World, 168)) // 13
            );
    }

    [Fact(DisplayName = "3D sector light water pool with floating solid sector")]
    public void LightTransferWaterPool()
    {
        var waterSector = GameActions.GetSector(World, 173);
        AssertPlanes3D(waterSector,
            new PlaneData(512, PlaneFace3D.Top, waterSector), // 0
            new PlaneData(-16, PlaneFace3D.Top, waterSector), // 1
            new PlaneData(-64, PlaneFace3D.Bottom, GameActions.GetSector(World, 174)), // 2
            new PlaneData(-64, PlaneFace3D.Top, GameActions.GetSector(World, 174), RenderPlanes: SectorPlanes.Ceiling), // 3
            new PlaneData(-128, PlaneFace3D.Bottom, GameActions.GetSector(World, 174), RenderPlanes: SectorPlanes.Ceiling), // 4
            new PlaneData(-128, PlaneFace3D.Bottom, GameActions.GetSector(World, 174)) // 5
            );

        var floatingSector = GameActions.GetSector(World, 178);
        AssertPlanes3D(floatingSector,
            new PlaneData(512, PlaneFace3D.Top, floatingSector), // 0
            new PlaneData(69, PlaneFace3D.Top, floatingSector), // 1
            new PlaneData(-64, PlaneFace3D.Bottom, GameActions.GetSector(World, 177)), // 2
            new PlaneData(-64, PlaneFace3D.Top, GameActions.GetSector(World, 174)), // 3
            new PlaneData(-80, PlaneFace3D.Bottom, GameActions.GetSector(World, 174)), // 4
            new PlaneData(-80, PlaneFace3D.Top, GameActions.GetSector(World, 174)), // 5
            new PlaneData(-128, PlaneFace3D.Bottom, GameActions.GetSector(World, 174), RenderPlanes: SectorPlanes.Ceiling), // 6
            new PlaneData(-128, PlaneFace3D.Bottom, GameActions.GetSector(World, 174)) // 7
            );
    }

    [Fact(DisplayName = "3D sector simple water")]
    public void WaterSector()
    {
        var sector = GameActions.GetSector(World, 189);
        AssertPlanes3D(sector,
            new PlaneData(512, PlaneFace3D.Top, sector),
            new PlaneData(-16, PlaneFace3D.Top, sector, RenderPlanes: SectorPlanes.Ceiling),
            new PlaneData(-64, PlaneFace3D.Bottom, GameActions.GetSector(World, 190), RenderPlanes: SectorPlanes.Ceiling),
            new PlaneData(-64, PlaneFace3D.Bottom, GameActions.GetSector(World, 190), RenderPlanes: SectorPlanes.Ceiling)
            );
    }

    [Fact(DisplayName = "3D sector with overlapping planes should render inside walls")]
    public void OverlapRenderInside()
    {
        var sector = GameActions.GetSector(World, 229);
        sector.Sectors3D.Length.Should().Be(2);
        sector.Sectors3D[1].ShouldRenderInsideWalls.Should().BeTrue();

        foreach (var plane in sector.SectorPlanes3D)
            plane.NoRenderWall.Should().BeFalse();
    }

    [Fact(DisplayName = "Simple transfer light")]
    public void TransferLightSimple()
    {
        var sector = GameActions.GetSectorByTag(World, 89);
        var redSector = GameActions.GetSector(World, 306);
        AssertPlanes3D(sector,
            new PlaneData(80, PlaneFace3D.Top, sector),
            new PlaneData(80, PlaneFace3D.Top, redSector, RenderPlanes: SectorPlanes.Ceiling),
            new PlaneData(0, PlaneFace3D.Bottom, redSector, RenderPlanes: SectorPlanes.Ceiling),
            new PlaneData(0, PlaneFace3D.Bottom, redSector, RenderPlanes: SectorPlanes.Ceiling)
        );
    }

    [Fact(DisplayName = "Multiple 3D sectors with transfer light")]
    public void SectorsWithTransferLight()
    {
        var sector = GameActions.GetSectorByTag(World, 88);
        var yellowSector = GameActions.GetSector(World, 305);
        var redSector = GameActions.GetSector(World, 306);

        var topSector = GameActions.GetSector(World, 303);
        var bottomSector = GameActions.GetSector(World, 304);

        AssertPlanes3D(sector,
            new PlaneData(512, PlaneFace3D.Top, sector),
            new PlaneData(192, PlaneFace3D.Top, sector),
            new PlaneData(176, PlaneFace3D.Bottom, yellowSector),
            new PlaneData(176, PlaneFace3D.Top, topSector),
            new PlaneData(96, PlaneFace3D.Bottom, yellowSector),
            new PlaneData(96, PlaneFace3D.Top, yellowSector),
            new PlaneData(80, PlaneFace3D.Bottom, redSector),
            new PlaneData(80, PlaneFace3D.Top, bottomSector),
            new PlaneData(0, PlaneFace3D.Bottom, redSector),
            new PlaneData(0, PlaneFace3D.Bottom, redSector)
        );
    }

    [Fact(DisplayName = "Multiple 3D sectors with transfer light overlap")]
    public void SectorsWithTransferLightOverlap()
    {
        var sector = GameActions.GetSectorByTag(World, 90);
        var yellowSector = GameActions.GetSector(World, 311);
        var redSector = GameActions.GetSector(World, 312);

        AssertPlanes3D(sector,
            new PlaneData(512, PlaneFace3D.Top, sector),
            new PlaneData(224, PlaneFace3D.Top, sector),
            new PlaneData(192, PlaneFace3D.Top, yellowSector),
            new PlaneData(176, PlaneFace3D.Bottom, yellowSector),
            new PlaneData(96, PlaneFace3D.Top, yellowSector),
            new PlaneData(88, PlaneFace3D.Bottom, yellowSector),
            new PlaneData(88, PlaneFace3D.Top, yellowSector),
            new PlaneData(80, PlaneFace3D.Bottom, redSector),
            new PlaneData(0, PlaneFace3D.Bottom, redSector),
            new PlaneData(-88, PlaneFace3D.Bottom, redSector)
        );
    }

    private static void AssertPlanes3D(Sector sector, params PlaneData[] planes)
    {
        for (int i = 0; i < planes.Length; i++)
        {
            var plane = planes[i];
            var plane3D = sector.SectorPlanes3D[i];
            plane3D.GetZ().Should().Be(plane.Z);
            plane3D.Face.Should().Be(plane.Face);
            plane3D.LightSector.Should().Be(plane.LightSector);
            //plane3D.NoRenderWall.Should().Be(plane.NoRenderWall);

            if (plane3D.Sector3D == null)
                continue;

            var light = plane3D.Face == PlaneFace3D.Bottom ? plane3D.Sector3D.LightBottom : plane3D.Sector3D.LightTop;
            light.Should().Be(plane.LightSector);

            if (plane3D.Sector3D.ShouldRenderFlats)
                plane3D.Sector3D.RenderPlanes.Should().Be(plane.RenderPlanes);
        }
    }
}
