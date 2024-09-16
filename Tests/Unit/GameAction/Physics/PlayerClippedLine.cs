using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.Util;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using System;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class PlayerClippedLine
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public PlayerClippedLine()
    {
        World = WorldAllocator.LoadMap("Resources/box.zip", "box.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact(DisplayName = "Player can mouve out of single clipped line")]
    public void PlayerCanMoveOutOfSingleClippedLine()
    {
        var startPos = new Vec3D(-320, -632, 0);
        GameActions.SetEntityPosition(World, Player, startPos);

        Player.AngleRadians = GameActions.GetAngle(Bearing.North);

        int startTick = World.Gametick;
        GameActions.PlayerRunBackward(World, Player.AngleRadians, () => { return World.Gametick - startTick < 5; }, TimeSpan.FromSeconds(5));
        Player.BlockingLine.Should().NotBeNull();
        Player.BlockingLine!.Id.Should().Be(2);
        Player.Position.Should().Be(startPos);

        GameActions.PlayerRunForward(World, Player.AngleRadians, () => { return Player.Position.Y < -600; }, TimeSpan.FromSeconds(5));
    }

    [Fact(DisplayName = "Player can move out clipped corner")]
    public void PlayerCanMoveOutOfClippedCorner()
    {
        // This is the maximum tested against chocolate doom that the player can move out of
        // This is 5 units away from both lines from player center
        var startPos = new Vec3D(-5, -635, 0);
        GameActions.SetEntityPosition(World, Player, startPos);

        Player.AngleRadians = GameActions.GetAngle(Bearing.NorthWest);

        int startTick = World.Gametick;
        GameActions.PlayerRunBackward(World, Player.AngleRadians, () => { return World.Gametick - startTick < 5; }, TimeSpan.FromSeconds(5));
        Player.Position.Should().Be(startPos);

        GameActions.PlayerRunForward(World, Player.AngleRadians, () => { return Player.Position.Y < -624 && Player.Position.X > -18; }, TimeSpan.FromSeconds(5));
    }

    [Fact(DisplayName = "Player can't move out clipped corner")]
    public void PlayerCantMoveOutOfClippedCorner()
    {
        // Player can't move out of this line with normal forward movement
        var startPos = new Vec3D(-4, -636, 0);
        GameActions.SetEntityPosition(World, Player, startPos);

        Player.AngleRadians = GameActions.GetAngle(Bearing.NorthWest);

        int startTick = World.Gametick;
        GameActions.PlayerRunForward(World, Player.AngleRadians, () => { return World.Gametick - startTick < 35; }, TimeSpan.FromSeconds(5));
        Player.Position.Should().Be(startPos);

        // Can move out with SR40
        startTick = World.Gametick;
        Player.AngleRadians = MathHelper.ToRadians(95);
        GameActions.RunPlayerCommands(World, Player.AngleRadians, [TickCommands.Forward, TickCommands.Left], 35 * 3);
        Player.Position.Should().NotBe(startPos);
    }
}