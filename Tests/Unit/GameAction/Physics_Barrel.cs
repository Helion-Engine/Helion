using FluentAssertions;
using Helion.World;
using Helion.World.Physics;
using System.Linq;
using Xunit;

namespace Helion.Tests.Unit.GameAction
{
    public partial class Physics
    {
        [Fact(DisplayName = "Barrel explosion chain reaction")]
        public void BarrelChainExplosion()
        {
            var barrels = GameActions.GetSectorEntities(World, 38, "ExplosiveBarrel");
            barrels.Count.Should().Be(39);

            var startBarrel = GameActions.GetEntity(World, 40);
            startBarrel.Damage(Player, startBarrel.Health, false, DamageType.AlwaysApply);

            GameActions.TickWorld(World, () => { return barrels.Any(x => !x.IsDead); }, () => { });
        }

        [Fact(DisplayName = "Barrel explosion from crush")]
        public void BarrelCrush()
        {
            var barrels = GameActions.GetSectorEntities(World, 40, "ExplosiveBarrel");
            barrels.Count.Should().Be(3);

            var zombieMan = GameActions.GetEntity(World, 49);
            zombieMan.IsDead.Should().BeFalse();
            zombieMan.Position.Z.Should().Be(192);

            var sector = GameActions.GetSectorByTag(World, 9);
            GameActions.ActivateLine(World, Player, 178, ActivationContext.UseLine).Should().BeTrue();
            sector.ActiveCeilingMove.Should().NotBeNull();
            GameActions.TickWorld(World, () => { return !zombieMan.IsDead; }, () => { });

            // Explosion didn't do z-checking, so the barrel from below should kill this monster
            zombieMan.IsDead.Should().BeTrue();
            barrels.All(x => x.IsDead).Should().BeTrue();

            World.SpecialManager.RemoveSpecial(sector.ActiveCeilingMove!).Should().BeTrue();
        }
    }
}
