
using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Geometry.Sectors;
using Helion.World.Impl.SinglePlayer;
using Xunit;

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
            new PlaneData(128, PlaneFace3D.Top, sector),
            new PlaneData(64, PlaneFace3D.Top, sector, RenderPlanes: SectorPlanes.Ceiling),
            new PlaneData(48, PlaneFace3D.Bottom, GameActions.GetSector(World, 83), RenderPlanes: SectorPlanes.Ceiling),
            new PlaneData(48, PlaneFace3D.Top, GameActions.GetSector(World, 83)),
            new PlaneData(32, PlaneFace3D.Bottom, GameActions.GetSector(World, 81)),
            new PlaneData(32, PlaneFace3D.Top, GameActions.GetSector(World, 81)),
            new PlaneData(24, PlaneFace3D.Bottom, GameActions.GetSector(World, 82)),
            new PlaneData(16, PlaneFace3D.Top, GameActions.GetSector(World, 82), RenderPlanes: SectorPlanes.Ceiling),
            new PlaneData(0, PlaneFace3D.Bottom, GameActions.GetSector(World, 80), RenderPlanes: SectorPlanes.Ceiling),
            new PlaneData(0, PlaneFace3D.Bottom, GameActions.GetSector(World, 80))
            );
    }

    [Fact(DisplayName = "Light transfer with solid and non-solid plane sort")]
    public void LightTransferWithSolidSort()
    {
        var sector = GameActions.GetSector(World, 89);
        AssertPlanes3D(sector,
            new PlaneData(128, PlaneFace3D.Top, sector),
            new PlaneData(80, PlaneFace3D.Top, GameActions.GetSector(World, 89)),
            new PlaneData(64, PlaneFace3D.Bottom, GameActions.GetSector(World, 83)),
            new PlaneData(64, PlaneFace3D.Top, GameActions.GetSector(World, 86), RenderPlanes: SectorPlanes.Ceiling),
            new PlaneData(48, PlaneFace3D.Bottom, GameActions.GetSector(World, 83), RenderPlanes: SectorPlanes.Ceiling),
            new PlaneData(48, PlaneFace3D.Top, GameActions.GetSector(World, 83)),
            new PlaneData(32, PlaneFace3D.Bottom, GameActions.GetSector(World, 81)),
            new PlaneData(32, PlaneFace3D.Top, GameActions.GetSector(World, 81)),
            new PlaneData(24, PlaneFace3D.Bottom, GameActions.GetSector(World, 82)),
            new PlaneData(16, PlaneFace3D.Top, GameActions.GetSector(World, 82), RenderPlanes: SectorPlanes.Ceiling),
            new PlaneData(0, PlaneFace3D.Bottom, GameActions.GetSector(World, 80), RenderPlanes: SectorPlanes.Ceiling),
            new PlaneData(0, PlaneFace3D.Bottom, GameActions.GetSector(World, 80))
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

    [Fact(DisplayName = "Complex 3D sectors sort with overlapping render styles")]
    public void ComplexSort()
    {
        var sector = GameActions.GetSector(World, 139);
        AssertPlanes3D(sector,
            new PlaneData(1152, PlaneFace3D.Top, sector),                          //0
            new PlaneData(1016, PlaneFace3D.Top, GameActions.GetSector(World, 139), NoRenderWall: true, RenderPlanes: SectorPlanes.Floor), //1
            new PlaneData(1016, PlaneFace3D.Top, GameActions.GetSector(World, 163)), //2
            new PlaneData(1015, PlaneFace3D.Bottom, GameActions.GetSector(World, 137), RenderPlanes: SectorPlanes.Floor), //3
            new PlaneData(992, PlaneFace3D.Bottom, GameActions.GetSector(World, 136)), //4
            new PlaneData(992, PlaneFace3D.Top, GameActions.GetSector(World, 137)), //5
            new PlaneData(992, PlaneFace3D.Bottom, GameActions.GetSector(World, 136)), //6
            new PlaneData(880, PlaneFace3D.Top, GameActions.GetSector(World, 136)), //7
            new PlaneData(880, PlaneFace3D.Bottom, GameActions.GetSector(World, 129)), //8
            new PlaneData(760, PlaneFace3D.Top, GameActions.GetSector(World, 129), NoRenderWall: true, RenderPlanes: SectorPlanes.Floor), //9
            new PlaneData(760, PlaneFace3D.Top, GameActions.GetSector(World, 154)), //10
            new PlaneData(759, PlaneFace3D.Bottom, GameActions.GetSector(World, 157), RenderPlanes: SectorPlanes.Floor), //11
            new PlaneData(736, PlaneFace3D.Bottom, GameActions.GetSector(World, 156)), //12
            new PlaneData(736, PlaneFace3D.Top, GameActions.GetSector(World, 157)), //13
            new PlaneData(736, PlaneFace3D.Bottom, GameActions.GetSector(World, 156)), //14
            new PlaneData(624, PlaneFace3D.Top, GameActions.GetSector(World, 156)), //15
            new PlaneData(624, PlaneFace3D.Bottom, GameActions.GetSector(World, 159)), //16
            new PlaneData(504, PlaneFace3D.Top, GameActions.GetSector(World, 159), NoRenderWall: true, RenderPlanes: SectorPlanes.Ceiling), //17
            new PlaneData(504, PlaneFace3D.Bottom, GameActions.GetSector(World, 145), RenderPlanes: SectorPlanes.Ceiling), //18
            new PlaneData(504, PlaneFace3D.Top, GameActions.GetSector(World, 145)), //19
            new PlaneData(480, PlaneFace3D.Bottom, GameActions.GetSector(World, 147)), //20
            new PlaneData(480, PlaneFace3D.Top, GameActions.GetSector(World, 148)), //21
            new PlaneData(480, PlaneFace3D.Bottom, GameActions.GetSector(World, 147)), //22
            new PlaneData(368, PlaneFace3D.Top, GameActions.GetSector(World, 147)), //23
            new PlaneData(368, PlaneFace3D.Bottom, GameActions.GetSector(World, 150)), //24
            new PlaneData(256, PlaneFace3D.Top, GameActions.GetSector(World, 150)), //25
            new PlaneData(129, PlaneFace3D.Bottom, GameActions.GetSector(World, 150)), //26
            new PlaneData(128, PlaneFace3D.Top, GameActions.GetSector(World, 150), NoRenderWall: true, RenderPlanes: SectorPlanes.Floor), //27
            new PlaneData(128, PlaneFace3D.Top, GameActions.GetSector(World, 98)), //28
            new PlaneData(127, PlaneFace3D.Bottom, GameActions.GetSector(World, 103), RenderPlanes: SectorPlanes.Floor), //29
            new PlaneData(104, PlaneFace3D.Bottom, GameActions.GetSector(World, 102)), //30
            new PlaneData(104, PlaneFace3D.Top, GameActions.GetSector(World, 103)), //31
            new PlaneData(104, PlaneFace3D.Bottom, GameActions.GetSector(World, 102)), //32
            new PlaneData(-8, PlaneFace3D.Top, GameActions.GetSector(World, 102)), //33
            new PlaneData(-8, PlaneFace3D.Bottom, GameActions.GetSector(World, 105)), //34
            new PlaneData(-64, PlaneFace3D.Bottom, GameActions.GetSector(World, 105))  //35
            );
    }

    [Fact(DisplayName = "3D sector light transfer pool with floating solid sector")]
    private void LightTransferLavaPool()
    {
        var lavaSector = GameActions.GetSector(World, 167);
        AssertPlanes3D(lavaSector,
            new PlaneData(512, PlaneFace3D.Top, lavaSector),
            new PlaneData(0, PlaneFace3D.Top, lavaSector),
            new PlaneData(-16, PlaneFace3D.Bottom, GameActions.GetSector(World, 171)),
            new PlaneData(-16, PlaneFace3D.Top, GameActions.GetSector(World, 171)),
            new PlaneData(-32, PlaneFace3D.Bottom, GameActions.GetSector(World, 170)),
            new PlaneData(-32, PlaneFace3D.Top, GameActions.GetSector(World, 170)),
            new PlaneData(-48, PlaneFace3D.Bottom, GameActions.GetSector(World, 169)),
            new PlaneData(-48, PlaneFace3D.Top, GameActions.GetSector(World, 169), RenderPlanes: SectorPlanes.Ceiling),
            new PlaneData(-56, PlaneFace3D.Bottom, GameActions.GetSector(World, 168), RenderPlanes: SectorPlanes.Ceiling),
            new PlaneData(-56, PlaneFace3D.Top, GameActions.GetSector(World, 168), RenderPlanes: SectorPlanes.Ceiling),
            new PlaneData(-128, PlaneFace3D.Bottom, GameActions.GetSector(World, 172), RenderPlanes: SectorPlanes.Ceiling),
            new PlaneData(-128, PlaneFace3D.Bottom, GameActions.GetSector(World, 172))
            );

        var floatingSector = GameActions.GetSector(World, 176);
        AssertPlanes3D(floatingSector,
            new PlaneData(512, PlaneFace3D.Top, floatingSector),
            new PlaneData(69, PlaneFace3D.Top, floatingSector),
            new PlaneData(0, PlaneFace3D.Top, GameActions.GetSector(World, 177)),
            new PlaneData(-16, PlaneFace3D.Bottom, GameActions.GetSector(World, 171)),
            new PlaneData(-16, PlaneFace3D.Top, GameActions.GetSector(World, 171)),
            new PlaneData(-32, PlaneFace3D.Bottom, GameActions.GetSector(World, 170)),
            new PlaneData(-32, PlaneFace3D.Top, GameActions.GetSector(World, 170)),
            new PlaneData(-48, PlaneFace3D.Bottom, GameActions.GetSector(World, 169)),
            new PlaneData(-48, PlaneFace3D.Top, GameActions.GetSector(World, 169), RenderPlanes: SectorPlanes.Ceiling),
            new PlaneData(-56, PlaneFace3D.Bottom, GameActions.GetSector(World, 168), RenderPlanes: SectorPlanes.Ceiling),
            new PlaneData(-56, PlaneFace3D.Top, GameActions.GetSector(World, 168), RenderPlanes: SectorPlanes.Ceiling),
            new PlaneData(-80, PlaneFace3D.Bottom, GameActions.GetSector(World, 172)),
            new PlaneData(-128, PlaneFace3D.Bottom, GameActions.GetSector(World, 177), RenderPlanes: SectorPlanes.Ceiling),
            new PlaneData(-128, PlaneFace3D.Bottom, GameActions.GetSector(World, 172))
            );
    }

    [Fact(DisplayName = "3D sector light water pool with floating solid sector")]
    private void LightTransferWaterPool()
    {
        var waterSector = GameActions.GetSector(World, 173);
        AssertPlanes3D(waterSector,
            new PlaneData(512, PlaneFace3D.Top, waterSector),
            new PlaneData(-16, PlaneFace3D.Top, waterSector),
            new PlaneData(-64, PlaneFace3D.Bottom, GameActions.GetSector(World, 174)),
            new PlaneData(-64, PlaneFace3D.Top, GameActions.GetSector(World, 174), RenderPlanes: SectorPlanes.Ceiling),
            new PlaneData(-128, PlaneFace3D.Bottom, GameActions.GetSector(World, 175), RenderPlanes: SectorPlanes.Ceiling),
            new PlaneData(-128, PlaneFace3D.Bottom, GameActions.GetSector(World, 175))
            );

        var floatingSector = GameActions.GetSector(World, 178);
        AssertPlanes3D(floatingSector,
            new PlaneData(512, PlaneFace3D.Top, floatingSector),
            new PlaneData(69, PlaneFace3D.Top, floatingSector),
            new PlaneData(-16, PlaneFace3D.Top, GameActions.GetSector(World, 177)),
            new PlaneData(-64, PlaneFace3D.Bottom, GameActions.GetSector(World, 174)),
            new PlaneData(-64, PlaneFace3D.Top, GameActions.GetSector(World, 174), RenderPlanes: SectorPlanes.Ceiling),
            new PlaneData(-80, PlaneFace3D.Bottom, GameActions.GetSector(World, 175)),
            new PlaneData(-128, PlaneFace3D.Bottom, GameActions.GetSector(World, 177), RenderPlanes: SectorPlanes.Ceiling),
            new PlaneData(-128, PlaneFace3D.Bottom, GameActions.GetSector(World, 175))
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
