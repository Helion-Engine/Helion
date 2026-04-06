using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Udmf;

[Collection("GameActions")]
public class UdmfMoveSpecials
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public UdmfMoveSpecials()
    {
        World = WorldAllocator.LoadMap("Resources/udmfmovespecials.zip", "udmfmovespecials.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact(DisplayName = "Floor_LowerInstant")]
    public void FloorLowerInstant()
    {
        var sector = GameActions.GetSectorByTag(World, 1);
        sector.Floor.Z.Should().Be(1024);
        GameActions.ActivateLine(World, Player, 4, ActivationContext.UseLine).Should().BeTrue();
        sector.ActiveFloorMove.Should().NotBeNull();
        GameActions.TickWorld(World, 1);
        sector.Floor.Z.Should().Be(128);
        sector.Floor.PrevZ.Should().Be(128);
        sector.ActiveFloorMove.Should().BeNull();
    }

    [Fact(DisplayName = "Floor_RaiseInstant")]
    public void FloorRaiseInstant()
    {
        var sector = GameActions.GetSectorByTag(World, 2);
        sector.Floor.Z.Should().Be(0);
        GameActions.ActivateLine(World, Player, 8, ActivationContext.UseLine).Should().BeTrue();
        sector.ActiveFloorMove.Should().NotBeNull();
        GameActions.TickWorld(World, 1);
        sector.Floor.Z.Should().Be(896);
        sector.Floor.PrevZ.Should().Be(896);
        sector.ActiveFloorMove.Should().BeNull();
    }

    [Fact(DisplayName = "Ceiling_LowerInstant")]
    public void CeilingLowerInstant()
    {
        var sector = GameActions.GetSectorByTag(World, 3);
        sector.Ceiling.Z.Should().Be(1024);
        GameActions.ActivateLine(World, Player, 12, ActivationContext.UseLine).Should().BeTrue();
        sector.ActiveCeilingMove.Should().NotBeNull();
        GameActions.TickWorld(World, 1);
        sector.Ceiling.Z.Should().Be(128);
        sector.Ceiling.PrevZ.Should().Be(128);
        sector.ActiveCeilingMove.Should().BeNull();
    }

    [Fact(DisplayName = "Ceiling_RaiseInstant")]
    public void CeilingRaiseInstant()
    {
        var sector = GameActions.GetSectorByTag(World, 4);
        sector.Ceiling.Z.Should().Be(0);
        GameActions.ActivateLine(World, Player, 16, ActivationContext.UseLine).Should().BeTrue();
        sector.ActiveCeilingMove.Should().NotBeNull();
        GameActions.TickWorld(World, 1);
        sector.Ceiling.Z.Should().Be(896);
        sector.Ceiling.PrevZ.Should().Be(896);
        sector.ActiveCeilingMove.Should().BeNull();
    }
}
