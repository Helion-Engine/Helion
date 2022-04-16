using FluentAssertions;
using Helion.World;
using Helion.World.Entities.Players;

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
    }
}
