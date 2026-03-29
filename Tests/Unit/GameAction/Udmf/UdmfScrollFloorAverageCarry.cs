using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using System;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Udmf;

[Collection("GameActions")]
public class UdmfScrollFloorAverageCarry : IDisposable
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public UdmfScrollFloorAverageCarry()
    {
        World = WorldAllocator.LoadMap("Resources/udmfscrollfloor.zip", "udmfscrollfloor.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
        World.UseAverageScrollCarry().Should().BeTrue();
    }

    public void Dispose()
    {
        Player.Velocity = Vec3D.Zero;
    }

    [Fact(DisplayName = "Scroll floor average same Y")]
    public void ScrollAverageSameY()
    {
        GameActions.SetEntityPosition(World, Player, (-256, -96));
        GameActions.TickWorld(World, 70);
        Player.Velocity.Y.Should().BeApproximately(7.99, 2);
        Player.Velocity.X.Should().Be(0);
        Player.Velocity.Z.Should().Be(0);
    }

    [Fact(DisplayName = "Scroll floor average different Y")]
    public void ScrollAverageDifferentY()
    {
        GameActions.SetEntityPosition(World, Player, (-64, -96));
        GameActions.TickWorld(World, 70);
        Player.Velocity.Y.Should().BeApproximately(5.99, 2);
        Player.Velocity.X.Should().Be(0);
        Player.Velocity.Z.Should().Be(0);
    }

    [Fact(DisplayName = "Scroll floor average opposite Y")]
    public void ScrollAverageOppositeY()
    {
        GameActions.SetEntityPosition(World, Player, (128, -96));
        GameActions.TickWorld(World, 70);
        Player.Velocity.Y.Should().Be(0);
        Player.Velocity.X.Should().Be(0);
        Player.Velocity.Z.Should().Be(0);
    }

    [Fact(DisplayName = "Scroll floor average same X")]
    public void ScrollAverageSameX()
    {
        GameActions.SetEntityPosition(World, Player, (384, 0));
        GameActions.TickWorld(World, 70);
        Player.Velocity.X.Should().BeApproximately(7.99, 2);
        Player.Velocity.Y.Should().Be(0);
        Player.Velocity.Z.Should().Be(0);
    }

    [Fact(DisplayName = "Scroll floor average different X and Y")]
    public void ScrollAverageDifferentXAndY()
    {
        GameActions.SetEntityPosition(World, Player, (736, 192));
        GameActions.TickWorld(World, 5);
        Player.Velocity.X.Should().BeApproximately(1.95, 2);
        Player.Velocity.Y.Should().BeApproximately(0.97, 2);
        Player.Velocity.Z.Should().Be(0);
    }
}
