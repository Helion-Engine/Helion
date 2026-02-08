using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.Util;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class PlayerSlideWall
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public PlayerSlideWall()
    {
        World = WorldAllocator.LoadMap("Resources/clipline.zip", "clipline.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact(DisplayName = "Player slides on one-sided wall going east/west")]
    public void PlayerSlideOneSidedEastWest()
    {
        var startPos = new Vec3D(-320, -16, 0);
        GameActions.SetEntityPosition(World, Player, startPos);

        Player.Velocity = Vec3D.Zero;
        Player.AngleRadians = GameActions.GetAngle(Bearing.NorthEast);

        var lastVelocity = Player.Velocity;
        GameActions.RunPlayerCommands(World, Player.AngleRadians, [TickCommands.Forward, TickCommands.Right], 16, () =>
        {
            Player.Velocity.Y.Should().BeLessThan(1);
            Player.Velocity.Y.Should().Be(0);
            Player.Velocity.X.Should().BeGreaterThan(lastVelocity.X);
            lastVelocity = Player.Velocity;
        });
    }

    [Fact(DisplayName = "Player slides on one-sided wall going north/south")]
    public void PlayerSlideOneSidedNorthSouth()
    {
        var startPos = new Vec3D(-624, -320, 0);
        GameActions.SetEntityPosition(World, Player, startPos);

        Player.Velocity = Vec3D.Zero;
        Player.AngleRadians = GameActions.GetAngle(Bearing.NorthWest);

        var lastVelocity = Player.Velocity;
        GameActions.RunPlayerCommands(World, Player.AngleRadians, [TickCommands.Forward, TickCommands.Right], 16, () =>
        {
            Player.Velocity.X.Should().Be(0);
            Player.Velocity.Y.Should().BeGreaterThan(lastVelocity.Y);
            lastVelocity = Player.Velocity;
        });
    }

    [Fact(DisplayName = "Player slides on two-sided wall going north/west")]
    public void PlayerSlideTwoSidedEastWest()
    {
        var startPos = new Vec3D(-376, -400, 0);
        GameActions.SetEntityPosition(World, Player, startPos);

        Player.Velocity = Vec3D.Zero;
        Player.AngleRadians = GameActions.GetAngle(Bearing.NorthEast);

        var lastVelocity = Player.Velocity;
        GameActions.RunPlayerCommands(World, Player.AngleRadians, [TickCommands.Forward, TickCommands.Right], 14, () =>
        {
            Player.Velocity.Y.Should().Be(0);
            Player.Velocity.X.Should().BeGreaterThan(lastVelocity.X);
            lastVelocity = Player.Velocity;
        });
    }

    [Fact(DisplayName = "Player slides on two-sided wall going north/south")]
    public void PlayerSlideTwoSidedNorthSouth()
    {
        var startPos = new Vec3D(-240, -384, 0);
        GameActions.SetEntityPosition(World, Player, startPos);

        Player.Velocity = Vec3D.Zero;
        Player.AngleRadians = GameActions.GetAngle(Bearing.NorthWest);

        var lastVelocity = Player.Velocity;
        GameActions.RunPlayerCommands(World, Player.AngleRadians, [TickCommands.Forward, TickCommands.Right], 14, () =>
        {
            Player.Velocity.X.Should().Be(0);
            Player.Velocity.Y.Should().BeGreaterThan(lastVelocity.Y);
            lastVelocity = Player.Velocity;
        });
    }

    [Fact(DisplayName = "Player slides on two-sided wall going east/west slow")]
    public void PlayerSlideTwoSidedEastWestSlow()
    {
        var startPos = new Vec3D(-376, -400, 0);
        GameActions.SetEntityPosition(World, Player, startPos);

        Player.Velocity = Vec3D.Zero;
        Player.AngleRadians = MathHelper.ToRadians(88.657);

        var lastVelocity = Player.Velocity;
        GameActions.RunPlayerCommands(World, Player.AngleRadians, [TickCommands.Forward], 16, () =>
        {
            Player.Velocity.Y.Should().BeApproximately(1.562, 3);
            Player.Velocity.Y.Should().Be(0);
            Player.Velocity.X.Should().BeLessThan(0.31);
            lastVelocity = Player.Velocity;
        });
    }
}
