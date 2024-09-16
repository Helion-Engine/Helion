using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Tests.Unit.GameAction.Util;
using Helion.World;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using System;

namespace Helion.Tests.Unit.GameAction
{
    public static partial class GameActions
    {
        public static void RunPlayerJump(WorldBase world, Player player)
        {
            double z = player.Position.Z;
            player.Velocity.Z.Should().Be(player.Properties.Player.JumpZ);
            player.OnGround.Should().BeTrue();
            world.Tick();
            player.OnGround.Should().BeFalse();
            int[] jumpOffsets = new[] { 15, 21, 26, 30, 33, 35, 36, 36, 36, 34, 31, 27, 22, 16, 9, 1, 0 };
            int index = 0;

            TickWorld(world, jumpOffsets.Length, () =>
            {
                player.Position.Z.Should().Be(z + jumpOffsets[index]);
                index++;
            });
        }

        public static void PlayerRunForward(SinglePlayerWorld world, double angle, Func<bool> runWhile, TimeSpan? timeout = null, Action? onTick = null) =>
            RunPlayerCommands(world, angle, [TickCommands.Forward], runWhile, timeout, onTick, stopTicks: null, tickAngleTurn: null);

        public static void PlayerRunBackward(SinglePlayerWorld world, double angle, Func<bool> runWhile, TimeSpan? timeout = null, Action? onTick = null) =>
            RunPlayerCommands(world, angle, [TickCommands.Backward], runWhile, timeout, onTick, stopTicks: null, tickAngleTurn: null);

        public static void RunPlayerCommands(SinglePlayerWorld world, double angle, TickCommands[] commands, Func<bool> runWhile, TimeSpan? timeout = null, Action? onTick = null) =>
            RunPlayerCommands(world, angle, commands, runWhile, timeout, onTick);

        public static void RunPlayerCommands(SinglePlayerWorld world, double angle, TickCommands[] commands, int stopTicks, Action? onTick = null) =>
            RunPlayerCommands(world, angle, commands, () => { return true; }, onTick: onTick, stopTicks: stopTicks);

        public static void RunPlayerCommands(SinglePlayerWorld world, double angle, TickCommands[] commands, double tickAngleTurn, int stopTicks, Action? onTick = null) =>
            RunPlayerCommands(world, angle, commands, () => { return true; }, onTick: onTick, stopTicks: stopTicks, tickAngleTurn: tickAngleTurn);

        private static void RunPlayerCommands(SinglePlayerWorld world, double angle, TickCommands[] commands, Func<bool> runWhile, TimeSpan? timeout = null, Action? onTick = null,
            int? stopTicks = null, double? tickAngleTurn = null)
        {
            if (!timeout.HasValue)
                timeout = TimeSpan.FromSeconds(60);

            world.Player.AngleRadians = angle;
            world.Player.Velocity = Vec3D.Zero;
            TestTickCommand cmd = new();
            int runTicks = 0;
            while (runWhile())
            {
                Array.ForEach(commands, x => cmd.Add(x));
                if (tickAngleTurn.HasValue)
                    cmd.MouseAngle -= tickAngleTurn.Value;
                world.SetTickCommand(world.Player, cmd);
                world.Tick();
                runTicks++;

                onTick?.Invoke();

                if (stopTicks.HasValue && runTicks >= stopTicks)
                    break;

                if (runTicks > 35 * timeout.Value.TotalSeconds)
                    throw new Exception($"Tick world ran for more than {timeout.Value.TotalSeconds} seconds");
            }
        }
    }
}
