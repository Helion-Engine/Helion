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
        World = WorldAllocator.LoadMap("Resources/clipline.zip", "clipline.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact(DisplayName = "Player can move out of single clipped line")]
    public void PlayerCanMoveOutOfSingleClippedLine()
    {
        var startPos = new Vec3D(-320, -632, 0);
        GameActions.SetEntityPosition(World, Player, startPos);

        Player.Velocity = Vec3D.Zero;
        Player.AngleRadians = GameActions.GetAngle(Bearing.North);

        int startTick = World.Gametick;
        GameActions.PlayerRunBackward(World, Player.AngleRadians, () => { return World.Gametick - startTick < 5; }, TimeSpan.FromSeconds(5));
        World.Blockmap.BlockLines[Player.BlockingBlockLineIndex].LineId.Should().Be(2);
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

        Player.Velocity = Vec3D.Zero;
        Player.AngleRadians = GameActions.GetAngle(Bearing.NorthWest);

        int startTick = World.Gametick;
        GameActions.PlayerRunBackward(World, Player.AngleRadians, () => { return World.Gametick - startTick < 5; }, TimeSpan.FromSeconds(5));
        Player.Position.Should().Be(startPos);

        GameActions.PlayerRunForward(World, Player.AngleRadians, () => { return Player.Position.Y < -624 && Player.Position.X > -18; }, TimeSpan.FromSeconds(5));
    }

    [Fact(DisplayName = "Player can move out extremely clipped corner")]
    public void PlayerCanMoveOutOfExtremelyClippedCornerSR40()
    {
        // Player can't move out of this line with normal forward movement
        var startPos = new Vec3D(-4, -636, 0);
        GameActions.SetEntityPosition(World, Player, startPos);

        Player.Velocity = Vec3D.Zero;
        Player.AngleRadians = GameActions.GetAngle(Bearing.NorthWest);

        // Can move out with SR40
        int startTick = World.Gametick;
        Player.AngleRadians = MathHelper.ToRadians(95);
        GameActions.RunPlayerCommands(World, Player.AngleRadians, [TickCommands.Forward, TickCommands.Left], 35 * 3);
        Player.Position.Should().NotBe(startPos);
    }

    [Fact(DisplayName = "Player can move out extremely clipped corner forward")]
    public void PlayerCanMoveOutOfExtremelyClippedCornerForward()
    {
        // Player can't move out of this line with normal forward movement
        var startPos = new Vec3D(-4, -636, 0);
        GameActions.SetEntityPosition(World, Player, startPos);

        Player.Velocity = Vec3D.Zero;
        Player.AngleRadians = GameActions.GetAngle(Bearing.NorthWest);

        // Original doom behavior did not allow this section to pass. Boom behavior does...
        int startTick = World.Gametick;
        GameActions.PlayerRunForward(World, Player.AngleRadians, () => { return World.Gametick - startTick < 35; }, TimeSpan.FromSeconds(5));
        Player.Position.Should().NotBe(startPos);
    }

    [Fact(DisplayName = "Player can move with clipped line in north/south direction")]
    public void PlayerCanMoveWithClippedLineNorthSouth()
    {
        var startPos = new Vec3D(0, -320, 0);
        GameActions.SetEntityPosition(World, Player, startPos);

        Player.Velocity = Vec3D.Zero;
        Player.AngleRadians = GameActions.GetAngle(Bearing.North);

        int startTick = World.Gametick;
        GameActions.PlayerRunForward(World, Player.AngleRadians, () => { return World.Gametick - startTick < 35; }, TimeSpan.FromSeconds(5));
        Player.Position.Should().NotBe(startPos);

        Player.Velocity = Vec3D.Zero;
        startPos = Player.Position;
        startTick = World.Gametick;
        GameActions.PlayerRunBackward(World, Player.AngleRadians, () => { return World.Gametick - startTick < 35; }, TimeSpan.FromSeconds(5));
        Player.Position.Should().NotBe(startPos);

        // Can't strafe right
        Player.Velocity = Vec3D.Zero;
        startPos = Player.Position;
        startTick = World.Gametick;
        GameActions.PlayerStrafeRight(World, Player.AngleRadians, () => { return World.Gametick - startTick < 35; }, TimeSpan.FromSeconds(5));
        Player.Position.Should().Be(startPos);

        // Can strafe left
        Player.Velocity = Vec3D.Zero;
        startPos = Player.Position;
        startTick = World.Gametick;
        GameActions.PlayerStrafeLeft(World, Player.AngleRadians, () => { return World.Gametick - startTick < 35; }, TimeSpan.FromSeconds(5));
        Player.Position.Should().NotBe(startPos);
    }

    [Fact(DisplayName = "Player can move with clipped line in east/west direction")]
    public void PlayerCanMoveWithClippedLineEastWest()
    {
        var startPos = new Vec3D(-320, -640, 0);
        GameActions.SetEntityPosition(World, Player, startPos);

        Player.Velocity = Vec3D.Zero;
        Player.AngleRadians = GameActions.GetAngle(Bearing.East);

        int startTick = World.Gametick;
        GameActions.PlayerRunForward(World, Player.AngleRadians, () => { return World.Gametick - startTick < 35; }, TimeSpan.FromSeconds(5));
        Player.Position.Should().NotBe(startPos);

        Player.Velocity = Vec3D.Zero;
        startPos = Player.Position;
        startTick = World.Gametick;
        GameActions.PlayerRunBackward(World, Player.AngleRadians, () => { return World.Gametick - startTick < 35; }, TimeSpan.FromSeconds(5));
        Player.Position.Should().NotBe(startPos);

        Player.Velocity = Vec3D.Zero;
        startPos = Player.Position;
        startTick = World.Gametick;
        GameActions.PlayerStrafeRight(World, Player.AngleRadians, () => { return World.Gametick - startTick < 35; }, TimeSpan.FromSeconds(5));
        Player.Position.Should().Be(startPos);

        Player.Velocity = Vec3D.Zero;
        startPos = Player.Position;
        startTick = World.Gametick;
        GameActions.PlayerStrafeLeft(World, Player.AngleRadians, () => { return World.Gametick - startTick < 35; }, TimeSpan.FromSeconds(5));
        Player.Position.Should().NotBe(startPos);
    }

    [Fact(DisplayName = "Player can move out of two-sided clipped line")]
    public void PlayerCanMoveOutOfTwoSidedClippedLine()
    {
        var startPos = new Vec3D(-320, -386, 0);
        GameActions.SetEntityPositionInit(World, Player, startPos);

        Player.Velocity = Vec3D.Zero;
        Player.AngleRadians = GameActions.GetAngle(Bearing.South);

        int startTick = World.Gametick;
        GameActions.PlayerRunBackward(World, Player.AngleRadians, () => { return World.Gametick - startTick < 35; }, TimeSpan.FromSeconds(5));
        Player.Position.Should().Be(startPos);

        startTick = World.Gametick;
        GameActions.PlayerRunForward(World, Player.AngleRadians, () => { return World.Gametick - startTick < 35; }, TimeSpan.FromSeconds(5));
        Player.Position.Should().NotBe(startPos);
    }

    [Fact(DisplayName = "Player can't move out of two-sided clipped line")]
    public void PlayerCantMoveOutOfTwoSidedClippedLine()
    {
        var startPos = new Vec3D(-320, -384, 0);
        GameActions.SetEntityPositionInit(World, Player, startPos);

        Player.Velocity = Vec3D.Zero;
        Player.AngleRadians = GameActions.GetAngle(Bearing.South);

        int startTick = World.Gametick;
        GameActions.PlayerRunBackward(World, Player.AngleRadians, () => { return World.Gametick - startTick < 35; }, TimeSpan.FromSeconds(5));
        Player.Position.Should().Be(startPos);

        startTick = World.Gametick;
        GameActions.PlayerRunForward(World, Player.AngleRadians, () => { return World.Gametick - startTick < 35; }, TimeSpan.FromSeconds(5));
        Player.Position.Should().Be(startPos);
    }

    [Fact(DisplayName = "Player can move out of two-sided clipped line while being crushed")]
    public void PlayerCanMoveOutTwoSidedClippedLineCrusher()
    {
        var sector = GameActions.GetSector(World, 1);
        var saveFloor = sector.Floor.Z;
        var saveCeiling = sector.Ceiling.Z;
        sector.Floor.Z = 0;
        sector.Ceiling.Z = 48;

        var startPos = new Vec3D(-320, -386, 0);
        GameActions.SetEntityPosition(World, Player, startPos);
        Player.IsCrushing().Should().BeTrue();

        Player.Velocity = Vec3D.Zero;
        Player.AngleRadians = GameActions.GetAngle(Bearing.South);

        int startTick = World.Gametick;
        GameActions.PlayerRunForward(World, Player.AngleRadians, () => { return World.Gametick - startTick < 35; }, TimeSpan.FromSeconds(5));
        Player.Position.Should().NotBe(startPos);

        sector.Floor.Z = saveFloor;
        sector.Ceiling.Z = saveCeiling;
    }
}