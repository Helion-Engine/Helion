using FluentAssertions;
using Helion.Geometry.Vectors;
using Xunit;

namespace Helion.Tests.Unit.GameAction
{
    public partial class Inventory
    {
        [Fact(DisplayName = "Give health")]
        public void GiveHealth()
        {
            var medikit = GameActions.CreateEntity(World, "Medikit", Vec3D.Zero);
            Player.Health = 74;
            Player.GiveItem(medikit.Definition, medikit.Flags).Should().BeTrue();
            Player.BonusCount.Should().Be(6);
            Player.Health.Should().Be(99);

            Player.GiveItem(medikit.Definition, medikit.Flags).Should().BeTrue();
            Player.Health.Should().Be(100);
            GameActions.TickWorld(World, 6);
            Player.BonusCount.Should().Be(0);

            // At max health, should not pickup
            Player.GiveItem(medikit.Definition, medikit.Flags).Should().BeFalse();

            Player.Inventory.HasItem("Medikit").Should().BeFalse();
        }

        [Fact(DisplayName = "Give health bonus")]
        public void GiveHealthBonus()
        {
            var healthBonus = GameActions.CreateEntity(World, "HealthBonus", Vec3D.Zero);
            Player.GiveItem(healthBonus.Definition, healthBonus.Flags).Should().BeTrue();
            Player.Health.Should().Be(101);

            for (int i = 0; i < 99; i++)
            {
                Player.GiveItem(healthBonus.Definition, healthBonus.Flags).Should().BeTrue();
                Player.Health.Should().Be(101 + i + 1);
            }

            Player.Health.Should().Be(200);
            // Has always pickup flag, should return true
            Player.GiveItem(healthBonus.Definition, healthBonus.Flags).Should().BeTrue();
            Player.Health.Should().Be(200);
            Player.Inventory.HasItem("HealthBonus").Should().BeFalse();
        }
    }
}
