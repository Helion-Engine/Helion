using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.Util;
using Helion.Util.Configs.Components;
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
    private Player VooDooDoll => World.EntityManager.VoodooDolls[0];

    public PlayerClippedLine()
    {
        World = WorldAllocator.LoadMap("Resources/clipline.zip", "clipline.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact(DisplayName = "Player can move out of single clipped line MBF")]
    public void PlayerCanMoveOutOfSingleClippedLineMbf()
    {
        World.Config.Compatibility.MbfPlayerMovement.Set(CompatSetting.True);
        PlayerCanMoveOutOfSingleClippedLine();
    }

    [Fact(DisplayName = "Player can move out of single clipped line Vanilla")]
    public void PlayerCanMoveOutOfSingleClippedLineVanilla()
    {
        World.Config.Compatibility.MbfPlayerMovement.Set(CompatSetting.True);
        PlayerCanMoveOutOfSingleClippedLine();
    }

    [Fact(DisplayName = "Player can move out clipped corner MBF")]
    public void PlayerCanMoveOutOfClippedCornerMbf()
    {
        World.Config.Compatibility.MbfPlayerMovement.Set(CompatSetting.True);
        PlayerCanMoveOutOfClippedCorner();
    }

    [Fact(DisplayName = "Player can move out clipped corner Vanilla")]
    public void PlayerCanMoveOutOfClippedCornerVanilla()
    {
        World.Config.Compatibility.MbfPlayerMovement.Set(CompatSetting.False);
        PlayerCanMoveOutOfClippedCorner();
    }

    [Fact(DisplayName = "Player can move out extremely clipped corner MBF")]
    public void PlayerCanMoveOutOfExtremelyClippedCornerSR40Mbf()
    {
        World.Config.Compatibility.MbfPlayerMovement.Set(CompatSetting.True);
        PlayerCanMoveOutOfExtremelyClippedCornerSR40();
    }

    [Fact(DisplayName = "Player can move out extremely clipped corner Vanilla")]
    public void PlayerCanMoveOutOfExtremelyClippedCornerSR40Vanilla()
    {
        World.Config.Compatibility.MbfPlayerMovement.Set(CompatSetting.False);
        PlayerCanMoveOutOfExtremelyClippedCornerSR40();
    }

    [Fact(DisplayName = "Player can move out extremely clipped corner forward MBF")]
    public void PlayerCanMoveOutOfExtremelyClippedCornerForwardMbf()
    {
        World.Config.Compatibility.MbfPlayerMovement.Set(CompatSetting.True);
        PlayerCanMoveOutOfExtremelyClippedCornerForward(true);
    }

    [Fact(DisplayName = "Player can move out extremely clipped corner forward Vanilla")]
    public void PlayerCanMoveOutOfExtremelyClippedCornerForwardVanilla()
    {
        World.Config.Compatibility.MbfPlayerMovement.Set(CompatSetting.False);
        PlayerCanMoveOutOfExtremelyClippedCornerForward(false);
    }

    [Fact(DisplayName = "Player can move with clipped line in north/south direction MBF")]
    public void PlayerCanMoveWithClippedLineNorthSouthMbf()
    {
        World.Config.Compatibility.MbfPlayerMovement.Set(CompatSetting.True);
        PlayerCanMoveWithClippedLineNorthSouth();
    }

    [Fact(DisplayName = "Player can move with clipped line in east/west direction MBF")]
    public void PlayerCanMoveWithClippedLineEastWestMbf()
    {
        World.Config.Compatibility.MbfPlayerMovement.Set(CompatSetting.True);
        PlayerCanMoveWithClippedLineEastWest();
    }

    [Fact(DisplayName = "Player can move out of two-sided clipped line MBF")]
    public void PlayerCanMoveOutOfTwoSidedClippedLineMbf()
    {
        World.Config.Compatibility.MbfPlayerMovement.Set(CompatSetting.True);
        PlayerCanMoveOutOfTwoSidedClippedLine();
    }

    [Fact(DisplayName = "Player can move out of two-sided clipped line Vanilla")]
    public void PlayerCanMoveOutOfTwoSidedClippedLineVanilla()
    {
        World.Config.Compatibility.MbfPlayerMovement.Set(CompatSetting.False);
        PlayerCanMoveOutOfTwoSidedClippedLine();
    }

    [Fact(DisplayName = "Player can't move out of two-sided clipped line MBF")]
    public void PlayerCantMoveOutOfTwoSidedClippedLineMbf()
    {
        World.Config.Compatibility.MbfPlayerMovement.Set(CompatSetting.True);
        PlayerCantMoveOutOfTwoSidedClippedLine();
    }

    [Fact(DisplayName = "Player can't move out of two-sided clipped line Vanilla")]
    public void PlayerCantMoveOutOfTwoSidedClippedLineVanilla()
    {
        World.Config.Compatibility.MbfPlayerMovement.Set(CompatSetting.False);
        PlayerCantMoveOutOfTwoSidedClippedLine();
    }

    [Fact(DisplayName = "Player can move out of two-sided clipped line while being crushed MBF")]
    public void PlayerCanMoveOutTwoSidedClippedLineCrusherMbf()
    {
        World.Config.Compatibility.MbfPlayerMovement.Set(CompatSetting.True);
        PlayerCanMoveOutTwoSidedClippedLineCrusher();
    }

    [Fact(DisplayName = "Player can move out of two-sided clipped line while being crushed Vanilla")]
    public void PlayerCanMoveOutTwoSidedClippedLineCrusherVanilla()
    {
        World.Config.Compatibility.MbfPlayerMovement.Set(CompatSetting.False);
        PlayerCanMoveOutTwoSidedClippedLineCrusher();
    }

    [Fact(DisplayName = "Player can move of clipped line because of momentum vanilla")]
    public void PlayerCanMoveOutOfSingleClippedLineMomentumVanilla()
    {
        World.Config.Compatibility.MbfPlayerMovement.Set(CompatSetting.False);
        PlayerCanMoveOutOfSingleClippedLineMomentum(Player);
    }

    [Fact(DisplayName = "Voodoo Player can move of clipped line because of momentum vanilla")]
    public void VoodooPlayerCanMoveOutOfSingleClippedLineMomentumVanilla()
    {
        World.Config.Compatibility.MbfPlayerMovement.Set(CompatSetting.False);
        PlayerCanMoveOutOfSingleClippedLineMomentum(VooDooDoll);
        // The Mbf compatibility setting does not affect the voodoo doll movement, so this test should pass regardless of the setting
        World.Config.Compatibility.MbfPlayerMovement.Set(CompatSetting.True);
        PlayerCanMoveOutOfSingleClippedLineMomentum(VooDooDoll);
    }

    private void PlayerCanMoveOutOfSingleClippedLineMomentum(Player player)
    {
        var startPos = new Vec3D(-320, -632, 0);
        GameActions.SetEntityPosition(World, player, startPos);

        player.Velocity = Vec3D.Zero;
        player.AngleRadians = GameActions.GetAngle(Bearing.North);

        for (int i = 0; i < 3; i++)
        {
            for (int j = 0; j < i + 1; j++)
            {
                player.Velocity.Y += Player.ForwardMovementSpeedRun;
                World.Tick();
            }

            player.BlockingBlockLineIndex.Should().NotBe(-1);
            World.Blockmap.BlockLines[player.BlockingBlockLineIndex].LineId.Should().Be(2);
            player.Position.Should().Be(startPos);
        }

        // After 4 movement increases, the player should be able to move out of the clipped line because of momentum
        for (int j = 0; j <= 4; j++)
        {
            player.Velocity.Y += Player.ForwardMovementSpeedRun;
            World.Tick();
        }
        player.BlockingBlockLineIndex.Should().Be(-1);
        player.Position.Should().NotBe(startPos);
    }

    private void PlayerCanMoveOutOfSingleClippedLine()
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

    private void PlayerCanMoveOutOfClippedCorner()
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

    private void PlayerCanMoveOutOfExtremelyClippedCornerSR40()
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

    private void PlayerCanMoveOutOfExtremelyClippedCornerForward(bool canMove)
    {
        // Player can't move out of this line with normal forward movement
        var startPos = new Vec3D(-4, -636, 0);
        GameActions.SetEntityPosition(World, Player, startPos);

        Player.Velocity = Vec3D.Zero;
        Player.AngleRadians = GameActions.GetAngle(Bearing.NorthWest);

        // Original doom behavior did not allow this section to pass. Boom behavior does...
        int startTick = World.Gametick;
        GameActions.PlayerRunForward(World, Player.AngleRadians, () => { return World.Gametick - startTick < 35; }, TimeSpan.FromSeconds(5));

        if (canMove)
            Player.Position.Should().NotBe(startPos);
        else
            Player.Position.Should().Be(startPos);
    }

    private void PlayerCanMoveWithClippedLineNorthSouth()
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

    private void PlayerCanMoveWithClippedLineEastWest()
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

    private void PlayerCanMoveOutOfTwoSidedClippedLine()
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

    private void PlayerCantMoveOutOfTwoSidedClippedLine()
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

    private void PlayerCanMoveOutTwoSidedClippedLineCrusher()
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