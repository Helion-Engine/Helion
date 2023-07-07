using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.World.Physics;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

public partial class Physics
{
    [Fact(DisplayName = "Things don't link to sector with impassible line")]
    public void ImpassibleMoveLineCheck()
    {
        var sector = GameActions.GetSectorByTag(World, 18);
        var monster = GameActions.GetEntity(World, 58);
        var item = GameActions.GetEntity(World, 61);
        monster.Position.Z.Should().Be(0);
        item.Position.Z.Should().Be(0);

        GameActions.ActivateLine(World, Player, 348, ActivationContext.UseLine).Should().BeTrue();
        sector.ActiveFloorMove.Should().NotBeNull();

        GameActions.RunSectorPlaneSpecial(World, sector, () =>
        {
            monster.Position.Z.Should().Be(0);
            item.Position.Z.Should().Be(0);
        });
    }

    [Fact(DisplayName = "Things don't link to sector with monster block line")]
    public void MonsterBlockMoveLineCheck()
    {
        var sector = GameActions.GetSectorByTag(World, 19);
        var monster = GameActions.GetEntity(World, 59);
        var item = GameActions.GetEntity(World, 62);
        monster.Position.Z.Should().Be(0);
        item.Position.Z.Should().Be(0);

        GameActions.ActivateLine(World, Player, 353, ActivationContext.UseLine).Should().BeTrue();
        sector.ActiveFloorMove.Should().NotBeNull();

        GameActions.RunSectorPlaneSpecial(World, sector, () =>
        {
            monster.Position.Z.Should().Be(0);
            item.Position.Z.Should().Be(0);
        });
    }

    [Fact(DisplayName = "Things link to sector even though it doesn't fit")]
    public void NoFitMoveLineCheck()
    {
        var sector = GameActions.GetSectorByTag(World, 20);
        var monster = GameActions.GetEntity(World, 60);
        var item = GameActions.GetEntity(World, 63);
        monster.Position.Z.Should().Be(0);
        item.Position.Z.Should().Be(0);

        GameActions.ActivateLine(World, Player, 358, ActivationContext.UseLine).Should().BeTrue();
        sector.ActiveFloorMove.Should().NotBeNull();

        GameActions.RunSectorPlaneSpecial(World, sector, () =>
        {
            monster.Position.Z.Should().Be(sector.Floor.Z);
            item.Position.Z.Should().Be(sector.Floor.Z);
        });
    }

    [Fact(DisplayName = "Player doesn't link to sector with impassible line")]
    public void ImpassibleMoveLineCheckPlayer()
    {
        var sector = GameActions.GetSectorByTag(World, 21);
        GameActions.SetEntityPositionInit(World, Player, (-160, 2104, 0));
        Player.Position.Z.Should().Be(0);

        GameActions.ActivateLine(World, Player, 363, ActivationContext.UseLine).Should().BeTrue();
        sector.ActiveFloorMove.Should().NotBeNull();

        GameActions.RunSectorPlaneSpecial(World, sector, () =>
        {
            Player.Position.Z.Should().Be(0);
        });
    }

    [Fact(DisplayName = "Player links to sector with monster block flag")]
    public void PassibleMoveLineCheckPlayer()
    {
        var sector = GameActions.GetSectorByTag(World, 22);
        GameActions.SetEntityPosition(World, Player, (32, 2104, 0));
        World.SetEntityPosition(Player, (32, 2104, 0));
        Player.Position.Z.Should().Be(0);

        GameActions.ActivateLine(World, Player, 368, ActivationContext.UseLine).Should().BeTrue();
        sector.ActiveFloorMove.Should().NotBeNull();

        GameActions.RunSectorPlaneSpecial(World, sector, () =>
        {
            Player.Position.Z.Should().Be(sector.Floor.Z);
        });
    }
}
