using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.World.Physics;
using Xunit;

namespace Helion.Tests.Unit.GameAction
{
    public partial class Physics
    {
        private const int FloorLowerLine1 = 29;
        private const int FloorRaiseLine1 = 31;
        private static readonly Vec2D FloorCenter1 = new(576, 416);
        private static readonly Vec2D FloorBlock1 = new(576, 448);

        [Fact(DisplayName = "Instant floor lower")]
        public void InstantFloorLower()
        {
            var sector = GameActions.GetSectorByTag(World, 4);
            sector.Floor.SetZ(0);
            var monster = GameActions.CreateEntity(World, Zombieman, FloorCenter1.To3D(0));
            GameActions.ActivateLine(World, Player, FloorLowerLine1, ActivationContext.UseLine).Should().BeTrue();
            sector.ActiveFloorMove.Should().NotBeNull();
            World.Tick();

            // Entities should stick to the floor, even though it's instant.
            sector.Floor.Z.Should().Be(-256);
            monster.Position.Z.Should().Be(-256);

            GameActions.DestroyCreatedEntities(World);
        }

        [Fact(DisplayName = "Instant floor raise")]
        public void InstantFloorRaise()
        {
            var line = GameActions.GetLine(World, FloorRaiseLine1);
            line.Special.LineSpecialCompatibility.IsVanilla = false;
            var sector = GameActions.GetSectorByTag(World, 4);
            sector.Floor.SetZ(-256);
            var monster = GameActions.CreateEntity(World, Zombieman, FloorCenter1.To3D(-256));
            GameActions.ActivateLine(World, Player, FloorRaiseLine1, ActivationContext.UseLine).Should().BeTrue();
            sector.ActiveFloorMove.Should().NotBeNull();
            World.Tick();

            // This should go to zero but because ZDoom broke this
            sector.Floor.Z.Should().Be(-128);
            monster.Position.Z.Should().Be(-128);

            sector.Floor.SetZ(-256);

            line.Special.LineSpecialCompatibility.IsVanilla = true;

            GameActions.ActivateLine(World, Player, FloorRaiseLine1, ActivationContext.UseLine).Should().BeTrue();
            sector.ActiveFloorMove.Should().NotBeNull();
            World.Tick();

            // Vanilla should raise properly to 0
            sector.Floor.Z.Should().Be(0);
            monster.Position.Z.Should().Be(0);

            GameActions.DestroyCreatedEntities(World);
            sector.Floor.SetZ(0);
            line.Special.LineSpecialCompatibility.IsVanilla = false;
        }

        [Fact(DisplayName = "Instant floor raise blocked")]
        public void InstantFloorRaiseBlock()
        {
            // The floor actually does not raise at all in this case.
            // When triggering a lower past the destination any blocking stops it entirely.
            var sector = GameActions.GetSectorByTag(World, 4);
            sector.Floor.SetZ(-256);
            var line = GameActions.GetLine(World, FloorRaiseLine1);
            line.Special.LineSpecialCompatibility.IsVanilla = true;
            var monster = GameActions.CreateEntity(World, Zombieman, FloorBlock1.To3D(-256));
            GameActions.ActivateLine(World, Player, FloorRaiseLine1, ActivationContext.UseLine).Should().BeTrue();
            sector.ActiveFloorMove.Should().NotBeNull();
            World.Tick();

            sector.Floor.Z.Should().Be(-256);
            monster.Position.Z.Should().Be(-256);

            GameActions.DestroyCreatedEntities(World);
        }

        [Fact(DisplayName = "Floor raise past ceiling")]
        public void FloorRaisePastCeiling()
        {
            var sector = GameActions.GetSectorByTag(World, 5);
            GameActions.ActivateLine(World, Player, 46, ActivationContext.UseLine).Should().BeTrue();
            sector.ActiveFloorMove.Should().NotBeNull();

            World.Config.Compatibility.VanillaSectorPhysics.Set(false);
            GameActions.RunSectorPlaneSpecial(World, sector);
            sector.Floor.Z.Should().Be(sector.Ceiling.Z);

            // Vanilla allowed floors to run through ceiling
            sector.Floor.SetZ(-128);
            World.Config.Compatibility.VanillaSectorPhysics.Set(true);
            GameActions.ActivateLine(World, Player, 46, ActivationContext.UseLine).Should().BeTrue();
            GameActions.RunSectorPlaneSpecial(World, sector);
            sector.Floor.Z.Should().Be(64);
        }

        [Fact(DisplayName = "Floor raise blocked by entity hitting a different ceiling")]
        public void FloorRaiseBlockedByDifferentCeiling()
        {
            var ceilingEntity = GameActions.GetEntity(World, 54);            
            var sector = GameActions.GetSectorByTag(World, 11);
            var monster = GameActions.CreateEntity(World, Zombieman, new Vec3D(120, 1200, -256));
            GameActions.ActivateLine(World, Player, 249, ActivationContext.UseLine).Should().BeTrue();
            sector.ActiveFloorMove.Should().NotBeNull();

            GameActions.TickWorld(World, 200);
            sector.Floor.Z.Should().Be(-152);

            sector.ActiveFloorMove.Should().NotBeNull();
            World.SpecialManager.RemoveSpecial(sector.ActiveFloorMove!);
            monster.Dispose();
            GameActions.RunSectorPlaneSpecial(World, sector);
        }

        [Fact(DisplayName = "Floor raise blocked by ceiling entity hanging from a different ceiling")]
        public void FloorRaiseBlockedByCeilingEntity()
        {
            var sector = GameActions.GetSectorByTag(World, 11);
            var monster = GameActions.CreateEntity(World, Zombieman, new Vec3D(120, 1136, -256));
            GameActions.ActivateLine(World, Player, 249, ActivationContext.UseLine).Should().BeTrue();
            sector.ActiveFloorMove.Should().NotBeNull();

            GameActions.TickWorld(World, 200);
            sector.Floor.Z.Should().Be(-236);

            sector.ActiveFloorMove.Should().NotBeNull();
            World.SpecialManager.RemoveSpecial(sector.ActiveFloorMove!);
            monster.Dispose();
            GameActions.RunSectorPlaneSpecial(World, sector);
        }

        [Fact(DisplayName = "Floor raise blocked by entity hitting a spawn ceiling entity")]
        public void FloorRaiseBlockedBySpawnCeilingEntity()
        {
            var sector = GameActions.GetSectorByTag(World, 11);
            var monster = GameActions.CreateEntity(World, Zombieman, new Vec3D(120, 1184, -256));
            var meat = GameActions.CreateEntity(World, "Meat2", new Vec3D(144, 1184, 0));
            meat.Position.Z.Should().Be(-96 - meat.Height);
            GameActions.ActivateLine(World, Player, 249, ActivationContext.UseLine).Should().BeTrue();
            sector.ActiveFloorMove.Should().NotBeNull();

            GameActions.TickWorld(World, 200);
            sector.Floor.Z.Should().Be(meat.Position.Z - monster.Height);

            sector.ActiveFloorMove.Should().NotBeNull();
            World.SpecialManager.RemoveSpecial(sector.ActiveFloorMove!);
            monster.Dispose();
            meat.Dispose();
            GameActions.RunSectorPlaneSpecial(World, sector);
        }

        [Fact(DisplayName = "Floor movement clamps entity to highest sector")]
        public void FloorMovementClampsEntityToHighestSector()
        {
            var sector = GameActions.GetSectorByTag(World, 10);
            var armor = GameActions.GetEntity(World, 53);
            sector.Floor.Z.Should().Be(-160);
            armor.Position.Z.Should().Be(-128);
            GameActions.ActivateLine(World, Player, 240, ActivationContext.UseLine).Should().BeTrue();
            sector.ActiveFloorMove.Should().NotBeNull();

            GameActions.RunSectorPlaneSpecial(World, sector);
            sector.Floor.Z.Should().Be(-128);
            sector.ActiveFloorMove.Should().BeNull();
            armor.Position.Z.Should().Be(-96);
        }
    }
}
