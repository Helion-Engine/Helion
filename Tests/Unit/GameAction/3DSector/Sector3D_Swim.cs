using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.Util;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using System;
using Xunit;

namespace Helion.Tests.Unit.GameAction._3DSector;

[Collection("GameActions")]
public class Sector3D_Swim : IDisposable
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public Sector3D_Swim()
    {
        World = WorldAllocator.LoadMap("Resources/sector3d-swim.zip", "sector3d-swim.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    public void Dispose()
    {
        Player.Velocity = Vec3D.Zero;
        Player.PitchRadians = 0;
        Player.ResetAirSupply();
        World.ResetGametick();
    }

    [Fact(DisplayName = "Player walk in swim sector")]
    public void WalkInSwimSector()
    {
        GameActions.SetEntityPosition(World, Player, (-64, 256, -24));
        Player.Sector.Sectors3D.Length.Should().Be(1);
        Player.Sector.Sectors3D[0].IsSwimmable.Should().BeTrue();
        Player.WaterSubmersionLevel.Should().Be(SubmersionLevel.LessThanHalf);
        var firstTick = true;

        GameActions.PlayerRunForward(World, GameActions.GetAngle(Bearing.North), () => { return Player.Position.Y < 352; }, onTick: () =>
        {
            var velocity = Player.Velocity.Y;
            // Movement speed is cut in half when in water
            if (firstTick)
            {
                Player.Velocity.Y.Should().Be(Player.ForwardMovementSpeedRun * 0.5 * Constants.DefaultFriction);
                firstTick = false;
            }
        });
    }

    [Fact(DisplayName = "Player swims out of swim sector less than half submersion")]
    public void SwimOutOfSwimSectorLessThanHalf()
    {
        GameActions.SetEntityPosition(World, Player, (128, 352, -32));
        Player.Sector.Sectors3D.Length.Should().Be(1);
        Player.Sector.Sectors3D[0].IsSwimmable.Should().BeTrue();
        Player.WaterSubmersionLevel.Should().Be(SubmersionLevel.LessThanHalf);

        GameActions.PlayerRunForward(World, GameActions.GetAngle(Bearing.North), () => { return Player.BlockingBlockLineIndex == -1; }, onTick: () =>
        {
            if (Player.BlockingBlockLineIndex == -1)
                Player.Velocity.Z.Should().Be(0);
        });

        var line = World.Blockmap.BlockLines[Player.BlockingBlockLineIndex];
        line.LineId.Should().Be(13);

        Player.Velocity.Z.Should().BeGreaterThan(0);

        GameActions.PlayerRunForward(World, GameActions.GetAngle(Bearing.North), () => { return Player.Position.Y < 352; });
    }

    [Fact(DisplayName = "Player swims out of swim sector more than half submersion")]
    public void SwimOutOfSwimSectorMoreThanHalf()
    {
        GameActions.SetEntityPosition(World, Player, (320, 352, -56));
        Player.Sector.Sectors3D.Length.Should().Be(1);
        Player.Sector.Sectors3D[0].IsSwimmable.Should().BeTrue();
        Player.WaterSubmersionLevel.Should().Be(SubmersionLevel.MoreThanHalf);

        GameActions.PlayerRunForward(World, GameActions.GetAngle(Bearing.North), () => { return Player.BlockingBlockLineIndex == -1; }, onTick: () =>
        {
            if (Player.BlockingBlockLineIndex == -1)
                Player.Velocity.Z.Should().Be(0);
        });

        var line = World.Blockmap.BlockLines[Player.BlockingBlockLineIndex];
        line.LineId.Should().Be(17);

        Player.Velocity.Z.Should().BeGreaterThan(0);

        GameActions.PlayerRunForward(World, GameActions.GetAngle(Bearing.North), () => { return Player.Position.Y < 352; });
    }

    [Fact(DisplayName = "Player swims out of swim sector full submersion")]
    public void SwimOutOfSwimSectorFull()
    {
        GameActions.SetEntityPosition(World, Player, (512, 352, -64));
        Player.Sector.Sectors3D.Length.Should().Be(1);
        Player.Sector.Sectors3D[0].IsSwimmable.Should().BeTrue();
        Player.WaterSubmersionLevel.Should().Be(SubmersionLevel.Full);

        GameActions.PlayerRunForward(World, GameActions.GetAngle(Bearing.North), () => { return Player.BlockingBlockLineIndex == -1; }, onTick: () =>
        {
            if (Player.BlockingBlockLineIndex == -1)
                Player.Velocity.Z.Should().Be(0);
        });

        var line = World.Blockmap.BlockLines[Player.BlockingBlockLineIndex];
        line.LineId.Should().Be(21);

        // Too low to leave
        Player.Velocity.Z.Should().Be(0);

        // A single jump should allow the player to leave
        Player.Jump();
        GameActions.PlayerRunForward(World, GameActions.GetAngle(Bearing.North), () => { return Player.Position.Y < 352; });
    }

    [Fact(DisplayName = "Player can't swim out of swim sector that's too high")]
    public void CantSwimOutOfSwimSector()
    {
        GameActions.SetEntityPosition(World, Player, (704, -32, -56));
        Player.Sector.Sectors3D.Length.Should().Be(1);
        Player.Sector.Sectors3D[0].IsSwimmable.Should().BeTrue();

        GameActions.PlayerRunForward(World, GameActions.GetAngle(Bearing.North), () => { return Player.BlockingBlockLineIndex == -1; }, onTick: () =>
        {
            if (Player.BlockingBlockLineIndex == -1)
                Player.Velocity.Z.Should().Be(0);
        });

        var line = World.Blockmap.BlockLines[Player.BlockingBlockLineIndex];
        line.LineId.Should().Be(29);

        Player.Velocity.Z.Should().BeGreaterThan(0);

        var startTick = World.Gametick;
        GameActions.PlayerRunForward(World, GameActions.GetAngle(Bearing.North), () => { return World.Gametick - startTick < 35 * 10; });
        Player.BlockingBlockLineIndex.Should().NotBe(-1);
        line = World.Blockmap.BlockLines[Player.BlockingBlockLineIndex];
        line.LineId.Should().Be(29);
    }

    [Fact(DisplayName = "Player sinks to bottom of swim sector")]
    public void SinksToBottom()
    {
        GameActions.SetEntityPosition(World, Player, (704, 352, -32));
        Player.Sector.Sectors3D.Length.Should().Be(1);
        Player.Sector.Sectors3D[0].IsSwimmable.Should().BeTrue();

        GameActions.TickWorld(World, () => { return Player.Position.Z > -256; }, () => { });
    }


    [Fact(DisplayName = "Player sinks in swim sector with high velocity")]
    public void SinksWithHighVelocity()
    {
        GameActions.SetEntityPosition(World, Player, (704, 256, -32));
        Player.Sector.Sectors3D.Length.Should().Be(1);
        Player.Sector.Sectors3D[0].IsSwimmable.Should().BeTrue();
        Player.Velocity.Z = -4;
        GameActions.TickWorld(World, 1);
        Player.Velocity.Z.Should().Be(-4.625);
        GameActions.TickWorld(World, 1);
        Player.Velocity.Z.Should().Be(-5.19140625);
        GameActions.TickWorld(World, 1);
        Player.Velocity.Z.Should().Be(-4.7047119140625);
        GameActions.TickWorld(World, 1);
        Player.Velocity.Z.Should().Be(-4.2636451721191406);
        GameActions.TickWorld(World, 1);
        Player.Velocity.Z.Should().Be(-3.8639284372329712);
        GameActions.TickWorld(World, 1);
        Player.Velocity.Z.Should().Be(-3.50168514624238);
        GameActions.TickWorld(World, 1);
        Player.Velocity.Z.Should().Be(-3.173402163782157);
        GameActions.TickWorld(World, 1);
        Player.Velocity.Z.Should().Be(-2.87589571092758);
        GameActions.TickWorld(World, 1);
        Player.Velocity.Z.Should().Be(-2.6062804880281192);
        GameActions.TickWorld(World, 1);
        Player.Velocity.Z.Should().Be(-2.361941692275483);
        GameActions.TickWorld(World, 1);
        Player.Velocity.Z.Should().Be(-2.1405096586246564);
        GameActions.TickWorld(World, 1);
        Player.Velocity.Z.Should().Be(-1.9398368781285948);
        GameActions.TickWorld(World, 1);
        Player.Velocity.Z.Should().Be(-1.757977170804039);
        GameActions.TickWorld(World, 1);
        Player.Velocity.Z.Should().Be(-1.5931668110411603);
        GameActions.TickWorld(World, 1);
        Player.Velocity.Z.Should().Be(-1.4438074225060515);
        GameActions.TickWorld(World, 1);
        Player.Velocity.Z.Should().Be(-1.3084504766461091);
    }

    [Fact(DisplayName = "Player sinks in swim sector with high no velocity")]
    public void SinksWithNoVelocity()
    {
        GameActions.SetEntityPosition(World, Player, (704, 256, -48));
        Player.Sector.Sectors3D.Length.Should().Be(1);
        Player.Sector.Sectors3D[0].IsSwimmable.Should().BeTrue();
        Player.Velocity.Z = 0;
        for (int i = 0; i < 10; i++)
        {
            GameActions.TickWorld(World, 1);
            Player.Velocity.Z.Should().Be(-0.5);
        }
    }

    [Fact(DisplayName = "Player swims up")]
    public void PlayerSwimsUp()
    {
        GameActions.SetEntityPosition(World, Player, (704, 256, -128));
        Player.Sector.Sectors3D.Length.Should().Be(1);
        Player.Sector.Sectors3D[0].IsSwimmable.Should().BeTrue();
        Player.AngleRadians = GameActions.GetAngle(Bearing.North);
        Player.PitchRadians = MathHelper.ToRadians(30);
        Player.Velocity.Should().Be(Vec3D.Zero);
        Player.TickCommand.Add(TickCommands.Forward);
        var prevVelocityZ = 0.0;

        GameActions.RunPlayerCommands(World, GameActions.GetAngle(Bearing.North), [TickCommands.Forward], () => { return Player.Position.Y < 320; }, onTick: () =>
        {
            Player.Velocity.Y.Should().BeGreaterThan(0);
            Player.Velocity.Z.Should().BeGreaterThan(0);
            Player.Velocity.Z.Should().BeGreaterThan(prevVelocityZ);
            prevVelocityZ = Player.Velocity.Z;
        });
    }

    [Fact(DisplayName = "Player swims down")]
    public void PlayerSwimsDown()
    {
        GameActions.SetEntityPosition(World, Player, (704, 256, -128));
        Player.Sector.Sectors3D.Length.Should().Be(1);
        Player.Sector.Sectors3D[0].IsSwimmable.Should().BeTrue();
        Player.AngleRadians = GameActions.GetAngle(Bearing.North);
        Player.PitchRadians = MathHelper.ToRadians(-30);
        Player.Velocity.Should().Be(Vec3D.Zero);
        Player.TickCommand.Add(TickCommands.Forward);
        var prevVelocityZ = 0.0;

        GameActions.RunPlayerCommands(World, GameActions.GetAngle(Bearing.North), [TickCommands.Forward], () => { return Player.Position.Y < 320; }, onTick: () =>
        {
            Player.Velocity.Y.Should().BeGreaterThan(0);
            Player.Velocity.Z.Should().BeLessThan(0);
            Player.Velocity.Z.Should().BeLessThan(prevVelocityZ);
            prevVelocityZ = Player.Velocity.Z;
        });
    }

    [Fact(DisplayName = "Missile in swim sector")]
    public void MissileInSwimSector()
    {
        GameActions.SetEntityPosition(World, Player, (704, 256, -128));
        Player.AngleRadians = GameActions.GetAngle(Bearing.North);
        Player.PitchRadians = 0;
        GameActions.PlayerFirePlasma(World, Player, out var plasma);
        plasma.Should().NotBeNull();
        plasma.Velocity.Y.Should().Be(25);
        GameActions.TickWorld(World, 1);
        // Should not slow down
        plasma.Velocity.Y.Should().Be(25);
    }

    [Fact(DisplayName = "Player air supply")]
    public void AirSupply()
    {
        GameActions.SetEntityPosition(World, Player, (704, 256, -256));
        Player.Health.Should().Be(100);
        Player.AirTicks.Should().Be(World.MapInfo.AirSupply);

        var startTick = World.Gametick;
        GameActions.TickWorld(World, () => { return Player.Health == 100; }, () => { });
        (World.Gametick - startTick).Should().BeGreaterThan(World.MapInfo.AirSupply);
        Player.Health.Should().Be(99);
        Player.AirTicks.Should().BeLessThan(0);

        startTick = World.Gametick;
        GameActions.TickWorld(World, () => { return Player.Health == 99; }, () => { });
        (World.Gametick - startTick).Should().BeGreaterThanOrEqualTo(31);
        Player.Health.Should().Be(98);
        Player.AirTicks.Should().BeLessThan(0);

        GameActions.TickWorld(World, 35 * 5);
        Player.Health.Should().Be(78);
        Player.AirTicks.Should().BeLessThan(0);

        // Air supply resets
        GameActions.SetEntityPosition(World, Player, (704, 256, -32));
        GameActions.TickWorld(World, 1);
        Player.Health.Should().Be(78);
        Player.AirTicks.Should().Be(World.MapInfo.AirSupply);

        GameActions.TickWorld(World, 35 * 5);
        Player.Health.Should().Be(78);

        Player.Health = 100;
    }

    [Fact(DisplayName = "Water submersion level")]
    public void WaterSubmersionLevel()
    {
        GameActions.SetEntityPosition(World, Player, (-96, -192, 0));
        Player.Sector.Sectors3D.Length.Should().Be(1);
        Player.Sector.Sectors3D[0].IsSwimmable.Should().BeTrue();
        Player.Sector.Sectors3D[0].ControlSector.Floor.Z.Should().Be(64);
        Player.Sector.Sectors3D[0].ControlSector.Ceiling.Z.Should().Be(128);

        Player.WaterSubmersionLevel.Should().Be(SubmersionLevel.None);

        // Submersion is determined by the center point. If player's center point isn't in the sector then it's not submerged.
        GameActions.SetEntityPosition(World, Player, (-96, -192, 8));
        Player.WaterSubmersionLevel.Should().Be(SubmersionLevel.None);
        GameActions.SetEntityPosition(World, Player, (-96, -192, 32));
        Player.WaterSubmersionLevel.Should().Be(SubmersionLevel.None);
        
        // Damage is determined by the submersion level. Even though the player's head is in the water they can still breathe.
        GameActions.TickWorld(World, World.MapInfo.AirSupply + 35);
        Player.AirTicks.Should().Be(World.MapInfo.AirSupply);
        Player.Health.Should().Be(100);

        GameActions.SetEntityPosition(World, Player, (-96, -192, 40));
        Player.WaterSubmersionLevel.Should().Be(SubmersionLevel.Full);
        GameActions.SetEntityPosition(World, Player, (-96, -192, 72));
        Player.WaterSubmersionLevel.Should().Be(SubmersionLevel.Full);
        GameActions.SetEntityPosition(World, Player, (-96, -192, 88));
        Player.WaterSubmersionLevel.Should().Be(SubmersionLevel.MoreThanHalf);
        GameActions.SetEntityPosition(World, Player, (-96, -192, 104));
        Player.WaterSubmersionLevel.Should().Be(SubmersionLevel.LessThanHalf);
        GameActions.SetEntityPosition(World, Player, (-96, -192, 128));
        Player.WaterSubmersionLevel.Should().Be(SubmersionLevel.None);
        GameActions.SetEntityPosition(World, Player, (-96, -192, 136));
        Player.WaterSubmersionLevel.Should().Be(SubmersionLevel.None);
    }

    [Fact(DisplayName = "Player doesn't swim when submersion level is less than half")]
    public void PlayerDoesntSwimWhenSubmersionLevelLessThanHalf()
    {
        GameActions.SetEntityPosition(World, Player, (960, 192, -44));
        Player.WaterSubmersionLevel.Should().Be(SubmersionLevel.LessThanHalf);
        Player.PitchRadians = MathHelper.ToRadians(30);
        Player.Velocity.Should().Be(Vec3D.Zero);
        Player.TickCommand.Add(TickCommands.Forward);

        GameActions.RunPlayerCommands(World, GameActions.GetAngle(Bearing.North), [TickCommands.Forward], () => { return Player.Position.Y < 320; }, onTick: () =>
        {
            Player.Velocity.Y.Should().BeGreaterThan(0);
            Player.Velocity.Z.Should().Be(0);
        });

        Player.Velocity.Y.Should().BeGreaterThan(0);
    }

    [Fact(DisplayName = "Player swims when submersion level is greater than half")]
    public void PlayerSwimsWhenSubmersionLevelGreaterThanHalf()
    {
        GameActions.SetEntityPosition(World, Player, (1152, 192, -45));
        Player.WaterSubmersionLevel.Should().Be(SubmersionLevel.MoreThanHalf);
        Player.PitchRadians = MathHelper.ToRadians(30);
        Player.Velocity.Should().Be(Vec3D.Zero);
        Player.TickCommand.Add(TickCommands.Forward);

        GameActions.RunPlayerCommands(World, GameActions.GetAngle(Bearing.North), [TickCommands.Forward], () => { return Player.Position.Y < 320; }, onTick: () =>
        {
            Player.Velocity.Y.Should().BeGreaterThan(0);
            Player.Velocity.Z.Should().BeGreaterThan(0);
        });

        Player.Velocity.Y.Should().BeGreaterThan(0);
        Player.Velocity.Z.Should().BeGreaterThan(0);
    }
}
