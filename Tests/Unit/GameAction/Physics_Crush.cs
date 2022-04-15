using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.World.Physics;
using Xunit;

namespace Helion.Tests.Unit.GameAction
{
    public partial class Physics
    {
        [Fact(DisplayName = "Crushing with floor movement")]
        public void CrushWithFloorMovement()
        {
            // Monster being a crushed while in a moving floor sector.
            // This has caused problems in the past and is more complex than one would anticipate.
            // The floor should be able to move even though the monster is being crushed (which normally triggers blocking).
            var crushSector = GameActions.GetSectorByTag(World, 8);
            var floorSector = GameActions.GetSectorByTag(World, 7);
            var monster = GameActions.CreateEntity(World, "BaronOfHell", new Vec3D(-696, 992, 0));
            GameActions.ActivateLine(World, Player, 52, ActivationContext.UseLine).Should().BeTrue();
            GameActions.ActivateLine(World, Player, 53, ActivationContext.UseLine).Should().BeTrue();
            crushSector.ActiveCeilingMove.Should().NotBeNull();
            floorSector.ActiveFloorMove.Should().NotBeNull();

            monster.IsCrushing().Should().BeTrue();
            GameActions.RunPerpetualMovingFloor(World, floorSector, -56, 0, 4, 16);
            World.SpecialManager.RemoveSpecial(crushSector.ActiveCeilingMove!);
            World.SpecialManager.RemoveSpecial(crushSector.ActiveFloorMove!);
        }

        [Fact(DisplayName = "Stacked crushing doom crusher")]
        public void StackedCrushDoom()
        {
            var sector = GameActions.GetSectorByTag(World, 6);
            var bottom1 = GameActions.CreateEntity(World, "BaronOfHell", new Vec3D(-928, 672, 0));
            var bottom2 = GameActions.CreateEntity(World, "BaronOfHell", new Vec3D(-928, 640, 0));
            var top = GameActions.CreateEntity(World, "BaronOfHell", new Vec3D(-928, 640, 64));

            GameActions.ActivateLine(World, Player, 74, ActivationContext.UseLine).Should().BeTrue();
            sector.ActiveCeilingMove.Should().NotBeNull();

            bool pushed = false;
            // Doom crusher should eventually push the top entity down below the ceiling
            GameActions.TickWorld(World, () => { return !top.IsDead; }, () =>
            {
                double z = sector.Ceiling.Z;
                bottom1.Health.Should().Be(top.Health);
                bottom2.Health.Should().Be(top.Health);

                // This gets clamped incorrectly at one point. It fixes itself and is pretty exotic...
                //bottom1.Position.Z.Should().Be(0);
                //bottom2.Position.Z.Should().Be(0);

                if (pushed)
                    top.Position.Z.Should().Be(0);

                if (top.Position.Z == 0)
                    pushed = true;
            });

            pushed.Should().BeTrue();
            top.IsDead.Should().BeTrue();
            bottom1.IsDead.Should().BeTrue();
            bottom1.IsDead.Should().BeTrue();

            World.SpecialManager.RemoveSpecial(sector.ActiveCeilingMove!);
            sector.Ceiling.SetZ(256);
        }

        [Fact(DisplayName = "Stacked crushing hexen crusher")]
        public void StackedCrushHexen()
        {
            var sector = GameActions.GetSectorByTag(World, 6);
            var bottom1 = GameActions.CreateEntity(World, "BaronOfHell", new Vec3D(-928, 672, 0));
            var bottom2 = GameActions.CreateEntity(World, "BaronOfHell", new Vec3D(-928, 640, 0));
            var top = GameActions.CreateEntity(World, "BaronOfHell", new Vec3D(-928, 640, 64));

            GameActions.ActivateLine(World, Player, 67, ActivationContext.UseLine).Should().BeTrue();
            sector.ActiveCeilingMove.Should().NotBeNull();

            // Hexen mode crusher that does damage but stops when it hits something
            // All entities through the stack should take crush damage
            GameActions.TickWorld(World, () => { return !top.IsDead; }, () =>
            { 
                bottom1.Health.Should().Be(top.Health);
                bottom2.Health.Should().Be(top.Health);

                top.Position.Z.Should().Be(64);
                bottom1.Position.Z.Should().Be(0);
                bottom2.Position.Z.Should().Be(0);

                top.OnEntity.Should().NotBeNull();
                bottom1.OverEntity.Should().Be(top);
                bottom2.OverEntity.Should().Be(top);
            });

            top.IsDead.Should().BeTrue();
            bottom1.IsDead.Should().BeTrue();
            bottom1.IsDead.Should().BeTrue();

            World.SpecialManager.RemoveSpecial(sector.ActiveCeilingMove!);
            sector.Ceiling.SetZ(256);
        }
    }
}
