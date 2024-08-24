using FluentAssertions;
using Helion.Resources.IWad;
using Helion.Util.RandomGenerators;
using Helion.World.Cheats;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Sectors;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Special.Specials;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class ZeroTag
{
    private readonly SinglePlayerWorld World;

    public ZeroTag()
    {
        World = WorldAllocator.LoadMap("Resources/zerotag.zip", "zerotag.WAD", "MAP01", GetType().Name, WorldInit, IWadType.Doom2);
    }

    private void WorldInit(SinglePlayerWorld world)
    {

    }

    [Fact(DisplayName = "Specials are set to sectors with zero tag")]
    public void TestSpecials()
    {
        var sector = GameActions.GetSector(World, 0);
        sector.TransferHeights.Should().NotBeNull();
        sector.TransferHeights!.ControlSector.Id.Should().Be(1);
        sector.TransferFloorLightSector.Id.Should().Be(2);
        var scroll = World.SpecialManager.GetSpecials().FirstOrDefault(x => x is ScrollSpecial scrollSpecial &&
            scrollSpecial.SectorPlane != null && scrollSpecial.SectorPlane.Sector.Id == sector.Id) as ScrollSpecial;
        scroll.Should().NotBeNull();
        scroll!.SectorPlane.Should().NotBeNull();
        scroll!.SectorPlane.Should().Be(sector.Floor);
    }

    [Fact(DisplayName = "Player teleports to sector with zero tag")]
    public void TestTeleport()
    {
        World.Player.Sector.Id.Should().Be(4);
        GameActions.EntityCrossLine(World, World.Player, 19, moveOutofBounds: false, forceFrozen: false).Should().BeTrue();
        World.Player.Sector.Id.Should().Be(0);
    }
}
