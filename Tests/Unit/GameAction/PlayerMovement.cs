using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.Util;
using Helion.World.Cheats;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using System;
using Xunit;

namespace Helion.Tests.Unit.GameAction
{
    [Collection("GameActions")]
    public class PlayerMovement : IDisposable
    {
        private static Vec3D PlayerSpeedTestPos = new(-640, -544, 0);
        private const int SpeedTestTicks = 35 * 5;

        private readonly SinglePlayerWorld World;
        private Player Player => World.Player;

        public PlayerMovement()
        {
            World = WorldAllocator.LoadMap("Resources/playermovement.zip", "playermovement.WAD", "MAP01", GetType().Name, WorldInit, IWadType.Doom2);
            World.Config.Hud.MoveBob.Set(0);
        }

        private void WorldInit(SinglePlayerWorld world)
        {
            world.CheatManager.ActivateCheat(world.Player, CheatType.God);
        }

        public void Dispose()
        {
            Player.Velocity = Vec3D.Zero;
            Player.Position = Vec3D.Zero;
            GameActions.DestroyCreatedEntities(World);
            GameActions.TickWorld(World, 8);
            GC.SuppressFinalize(this);
        }

        [Fact(DisplayName = "Player walk forward speed")]
        public void PlayerWalkForwardSpeed()
        {
            World.Config.Game.AlwaysRun.Set(false);
            Player.Velocity.Should().Be(Vec3D.Zero);
            GameActions.SetEntityPosition(World, Player, PlayerSpeedTestPos);
            GameActions.RunPlayerCommands(World, GameActions.GetAngle(Bearing.North), new TickCommands[] { TickCommands.Forward }, SpeedTestTicks);

            // Vanilla is 8.33302
            Player.Velocity.ApproxEquals(new(0, 8.333324712653916, 0)).Should().BeTrue();
            World.Config.Game.AlwaysRun.Set(true);
        }

        [Fact(DisplayName = "Player walk side speed")]
        public void PlayerWalkSideSpeed()
        {
            World.Config.Game.AlwaysRun.Set(false);
            Player.Velocity.Should().Be(Vec3D.Zero);
            GameActions.SetEntityPosition(World, Player, PlayerSpeedTestPos);
            GameActions.RunPlayerCommands(World, GameActions.GetAngle(Bearing.North), new TickCommands[] { TickCommands.Right }, SpeedTestTicks);

            // Vanilla is 7.99968
            Player.Velocity.ApproxEquals(new(7.999999736067171, 0, 0)).Should().BeTrue();
            World.Config.Game.AlwaysRun.Set(true);
        }

        [Fact(DisplayName = "Player forward run speed")]
        public void PlayerRunSpeed()
        {
            Player.Velocity.Should().Be(Vec3D.Zero);
            GameActions.SetEntityPosition(World, Player, PlayerSpeedTestPos);
            GameActions.RunPlayerCommands(World, GameActions.GetAngle(Bearing.North), new TickCommands[] { TickCommands.Forward }, SpeedTestTicks);

            // Vanilla is 16.6662
            Player.Velocity.ApproxEquals(new(0, 16.66666611680661, 0)).Should().BeTrue();
        }

        [Fact(DisplayName = "Player run side speed")]
        public void PlayerRunSideSpeed()
        {
            Player.Velocity.Should().Be(Vec3D.Zero);
            GameActions.SetEntityPosition(World, Player, PlayerSpeedTestPos);
            GameActions.RunPlayerCommands(World, GameActions.GetAngle(Bearing.North), new TickCommands[] { TickCommands.Right }, SpeedTestTicks);

            // Vanilla is 13.3329
            Player.Velocity.ApproxEquals(new(13.333332893445284, 0, 0)).Should().BeTrue();
        }

        [Fact(DisplayName = "Player strafe run 40")]
        public void PlayerStrafeRun40()
        {
            Player.Velocity.Should().Be(Vec3D.Zero);
            GameActions.SetEntityPosition(World, Player, PlayerSpeedTestPos);
            GameActions.RunPlayerCommands(World, GameActions.GetAngle(Bearing.North), new TickCommands[] { TickCommands.Forward, TickCommands.Right }, SpeedTestTicks);

            // Vanilla is 13.3263 16.6712
            Player.Velocity.ApproxEquals(new(13.333332893445284, 16.66666611680661, 0)).Should().BeTrue();
        }

        [Fact(DisplayName = "Player strafe run 50")]
        public void PlayerStrafeRun50()
        {
            Player.Velocity.Should().Be(Vec3D.Zero);
            double angle = GameActions.GetAngle(Bearing.North);
            GameActions.SetEntityPosition(World, Player, PlayerSpeedTestPos);
            GameActions.RunPlayerCommands(World, angle,
                new TickCommands[] { TickCommands.Forward, TickCommands.Right, TickCommands.Strafe, TickCommands.TurnRight }, SpeedTestTicks);

            // TickCommands.Strafe locks turning
            Player.AngleRadians.Should().Be(angle);

            // Vanilla is 16.6597 16.6725
            Player.Velocity.ApproxEquals(new(16.66666611680661, 16.66666611680661, 0)).Should().BeTrue();
        }

        [Fact(DisplayName = "Player strafe run 50 with mouse movement")]
        public void PlayerStrafeRun50Mouse()
        {
            Player.Velocity.Should().Be(Vec3D.Zero);
            double angle = GameActions.GetAngle(Bearing.North);
            GameActions.SetEntityPosition(World, Player, PlayerSpeedTestPos);
            GameActions.RunPlayerCommands(World, angle,
                new TickCommands[] { TickCommands.Forward, TickCommands.Strafe, TickCommands.Right }, MathHelper.QuarterPi, SpeedTestTicks);

            // TickCommands.Strafe locks turning
            Player.AngleRadians.Should().Be(angle);

            // Vanilla is 16.6597 16.6725
            Player.Velocity.ApproxEquals(new(16.66666611680661, 16.66666611680661, 0)).Should().BeTrue();
        }

        [Fact(DisplayName = "Player strafe with mouse movement")]
        public void PlayerMouseStrafe()
        {
            Player.Velocity.Should().Be(Vec3D.Zero);
            double angle = GameActions.GetAngle(Bearing.North);
            GameActions.SetEntityPosition(World, Player, PlayerSpeedTestPos);
            GameActions.RunPlayerCommands(World, angle, new TickCommands[] { TickCommands.Strafe }, MathHelper.QuarterPi, SpeedTestTicks);

            // TickCommands.Strafe locks turning
            Player.AngleRadians.Should().Be(angle);

            // Vanilla is 16.6597
            Player.Velocity.ApproxEquals(new(16.66666611680661, 0, 0)).Should().BeTrue();
        }
    }
}
