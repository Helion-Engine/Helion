using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.World.Physics;
using Helion.World.Special.SectorMovement;
using System;
using Xunit;

namespace Helion.Tests.Unit.GameAction
{
    [Collection("GameActions")]
    public partial class Physics
    {
        [Fact(DisplayName = "Lift movement slow")]
        public void LiftMovementSlow()
        {
            var sector = GameActions.GetSectorByTag(World, 1);
            var monster = GameActions.CreateEntity(World, Zombieman, LiftCenter1.To3D(0));
            GameActions.ActivateLine(World, Player, LiftLine1, ActivationContext.UseLine).Should().BeTrue();
            sector.ActiveFloorMove.Should().NotBeNull();

            GameActions.TickWorld(World, () => { return sector.ActiveFloorMove != null; }, () =>
            {
                monster.Position.Z.Should().Be(sector.Floor.Z);
            });
        }

        [Fact(DisplayName = "Lift movement fast")]
        public void LiftMovementFast()
        {
            var sector = GameActions.GetSectorByTag(World, 2);
            var monster = GameActions.CreateEntity(World, Zombieman, LiftCenter2.To3D(0));
            GameActions.ActivateLine(World, Player, LiftLine2, ActivationContext.UseLine).Should().BeTrue();
            sector.ActiveFloorMove.Should().NotBeNull();

            GameActions.TickWorld(World, () => { return sector.ActiveFloorMove != null; }, () =>
            {
                monster.Position.Z.Should().Be(sector.Floor.Z);
            });
        }

        [Fact(DisplayName = "Lift movement turbo")]
        public void LiftMovementTurbo()
        {
            var sector = GameActions.GetSectorByTag(World, 3);
            var monster = GameActions.CreateEntity(World, Zombieman, LiftCenter3.To3D(0));
            GameActions.ActivateLine(World, Player, LiftLine3, ActivationContext.UseLine).Should().BeTrue();
            sector.ActiveFloorMove.Should().NotBeNull();
            var floorMove = sector.ActiveFloorMove!;

            // This speed is too fast and entities should no longer stick to the floor when moving down.
            GameActions.TickWorld(World, () => { return sector.ActiveFloorMove != null; }, () =>
            {
                if (floorMove.MoveDirection == MoveDirection.Down)
                    monster.Position.Z.Should().NotBe(sector.Floor.Z);
                
                // Entities should hit floor after the delay and keep with the floor z on the ride back up.
                if (floorMove.DelayTics == 0 && floorMove.MoveDirection == MoveDirection.Up)
                    monster.Position.Z.Should().Be(sector.Floor.Z);
            });
        }

        [Fact(DisplayName = "Lift movement slow clipped with other entities")]
        public void LiftMovementClipped()
        {
            // Being clipped with other entities should not prevent lift movement.
            // They should also not stack.
            var sector = GameActions.GetSectorByTag(World, 1);
            var monster1 = GameActions.CreateEntity(World, Zombieman, LiftCenter1.To3D(0));
            var monster2 = GameActions.CreateEntity(World, Zombieman, LiftCenter1.To3D(0) + new Vec3D(16, 0, 0));
            GameActions.ActivateLine(World, Player, LiftLine1, ActivationContext.UseLine).Should().BeTrue();
            sector.ActiveFloorMove.Should().NotBeNull();

            GameActions.TickWorld(World, () => { return sector.ActiveFloorMove != null; }, () =>
            {
                monster1.Position.Z.Should().Be(sector.Floor.Z);
                monster2.Position.Z.Should().Be(sector.Floor.Z);
            });
        }

        [Fact(DisplayName = "Lift movement blocked")]
        public void LiftMovementBlocked()
        {
            // Being clipped with other entities should not prevent lift movement.
            // They should also not stack.
            var sector = GameActions.GetSectorByTag(World, 1);
            var monster1 = GameActions.CreateEntity(World, Zombieman, LiftBlock1.To3D(0));
            GameActions.ActivateLine(World, Player, LiftLine1, ActivationContext.UseLine).Should().BeTrue();
            sector.ActiveFloorMove.Should().NotBeNull();
            var floorMove = sector.ActiveFloorMove!;

            bool hitBlock = false;
            bool hitCheckComplete = false;
            bool moveBlocked = false;

            // The lift can move down even with the entity clipped in the wall. It will not be blocked until it retuns.
            GameActions.TickWorld(World, () => { return sector.ActiveFloorMove != null; }, () =>
            {
                if (hitBlock)
                {
                    floorMove.MoveDirection.Should().Be(MoveDirection.Down);
                    hitBlock = false;
                    hitCheckComplete = true;
                    // Remove the blocking monster when lift goes back down
                    GameActions.SetEntityOutOfBounds(World, monster1);
                }

                if (floorMove.MoveStatus != SectorMoveStatus.Success)
                {
                    sector.Floor.Z.Should().Be(-56);
                    hitBlock = true;
                    moveBlocked = true;
                }

                if (!hitCheckComplete)
                    monster1.Position.Z.Should().Be(sector.Floor.Z);
            });

            moveBlocked.Should().BeTrue();
            hitCheckComplete.Should().BeTrue();
        }

        [Fact(DisplayName = "Lift movement with stacked entities")]
        public void LiftMovementStacked()
        {
            var sector = GameActions.GetSectorByTag(World, 1);
            var monster1 = GameActions.CreateEntity(World, Zombieman, LiftCenter1.To3D(0));
            var monster2 = GameActions.CreateEntity(World, Zombieman, LiftCenter1.To3D(monster1.Height));
            var monster3 = GameActions.CreateEntity(World, Zombieman, LiftCenter1.To3D(monster1.Height * 2));

            monster1.Position.Z.Should().Be(0);
            monster1.OnEntity.Should().BeNull();
            monster1.OverEntity.Should().Be(monster2);

            monster2.Position.Z.Should().Be(monster1.Height);
            monster2.OnEntity.Should().Be(monster1);
            monster2.OverEntity.Should().Be(monster3);

            monster3.Position.Z.Should().Be(monster1.Height * 2);
            monster3.OnEntity.Should().Be(monster2);
            monster3.OverEntity.Should().BeNull();

            GameActions.ActivateLine(World, Player, LiftLine1, ActivationContext.UseLine).Should().BeTrue();
            sector.ActiveFloorMove.Should().NotBeNull();

            GameActions.TickWorld(World, () => { return sector.ActiveFloorMove != null; }, () =>
            {
                monster1.Position.Z.Should().Be(sector.Floor.Z);
                monster2.Position.Z.Should().Be(sector.Floor.Z + monster1.Height);
                monster3.Position.Z.Should().Be(sector.Floor.Z + monster1.Height * 2);
            });
        }

        [Fact(DisplayName = "Lift movement blocked by stacked entities")]
        public void LiftMovementStackedBlock()
        {
            var sector = GameActions.GetSectorByTag(World, 1);
            double ceilingZ = sector.Ceiling.Z;
            var monster1 = GameActions.CreateEntity(World, Zombieman, LiftCenter1.To3D(0));
            var monster2 = GameActions.CreateEntity(World, Zombieman, LiftCenter1.To3D(monster1.Height));
            var monster3 = GameActions.CreateEntity(World, Zombieman, LiftCenter1.To3D(monster1.Height * 2));

            monster1.Position.Z.Should().Be(0);
            monster1.OnEntity.Should().BeNull();
            monster1.OverEntity.Should().Be(monster2);

            monster2.Position.Z.Should().Be(monster1.Height);
            monster2.OnEntity.Should().Be(monster1);
            monster2.OverEntity.Should().Be(monster3);

            monster3.Position.Z.Should().Be(monster1.Height * 2);
            monster3.OnEntity.Should().Be(monster2);
            monster3.OverEntity.Should().BeNull();

            GameActions.ActivateLine(World, Player, LiftLine1, ActivationContext.UseLine).Should().BeTrue();
            sector.ActiveFloorMove.Should().NotBeNull();
            var floorMove = sector.ActiveFloorMove!;

            GameActions.TickWorld(World, () => { return sector.Floor.Z != -128; }, () =>
            {
                monster1.Position.Z.Should().Be(sector.Floor.Z);
                monster2.Position.Z.Should().Be(sector.Floor.Z + monster1.Height);
                monster3.Position.Z.Should().Be(sector.Floor.Z + monster1.Height * 2);
            });

            // Drop ceiling to block
            sector.Ceiling.SetZ(-128 + (monster1.Height * 3) + 16);
            monster3.UnlinkFromWorld();
            World.LinkClamped(monster3);

            GameActions.TickWorld(World, floorMove.DelayTics);
            GameActions.TickWorld(World, () => { return floorMove.MoveStatus != SectorMoveStatus.Blocked; }, () =>
            {
                monster1.Position.Z.Should().Be(sector.Floor.Z);
                monster2.Position.Z.Should().Be(sector.Floor.Z + monster1.Height);
                monster3.Position.Z.Should().Be(sector.Floor.Z + monster1.Height * 2);
            });

            // Should be blocked by the height of the stack
            sector.Floor.Z.Should().Be(sector.Ceiling.Z - (monster1.Height * 3));

            sector.Ceiling.SetZ(ceilingZ);
            GameActions.RunSectorPlaneSpecial(World, sector);
        }

        [Fact(DisplayName = "Lift movement with non-solid items")]
        public void LiftNonSolidItems()
        {
            // Dropped things are destroyed. Non-solid items should not block.
            var sector = GameActions.GetSectorByTag(World, 1);
            var regularClip = GameActions.CreateEntity(World, "CLIP", LiftBlock1.To3D(sector.Floor.Z));
            var droppedClip = GameActions.CreateEntity(World, "CLIP", LiftBlock1.To3D(sector.Floor.Z));
            droppedClip.Flags.Dropped = true;

            GameActions.ActivateLine(World, Player, LiftLine1, ActivationContext.UseLine).Should().BeTrue();
            sector.ActiveFloorMove.Should().NotBeNull();
            var floorMove = sector.ActiveFloorMove!;

            GameActions.TickWorld(World, () => { return sector.ActiveFloorMove != null; }, () =>
            {
                // The dropped clipped should be destroyed at this point.
                if (floorMove.MoveDirection == MoveDirection.Up && sector.Floor.Z > -16)
                    droppedClip.IsDisposed.Should().BeTrue();

                if (!droppedClip.IsDisposed)
                    droppedClip.Position.Z.Should().Be(sector.Floor.Z);

                regularClip.Position.Z.Should().Be(sector.Floor.Z);
                regularClip.IsDisposed.Should().BeFalse();
            });

            droppedClip.IsDisposed.Should().BeTrue();
            regularClip.IsDisposed.Should().BeFalse();
        }

        [Fact(DisplayName = "Lift movement with dead monsters")]
        public void LiftCrushDeadEntity()
        {
            // Dropped things are destroyed. Non-solid items should not block.
            var sector = GameActions.GetSectorByTag(World, 1);
            var monster = GameActions.CreateEntity(World, "ZOMBIEMAN", LiftBlock1.To3D(sector.Floor.Z));
            monster.Kill(null);
            // Run through death state.
            GameActions.TickWorld(World, 35);

            GameActions.ActivateLine(World, Player, LiftLine1, ActivationContext.UseLine).Should().BeTrue();
            sector.ActiveFloorMove.Should().NotBeNull();
            var floorMove = sector.ActiveFloorMove!;

            GameActions.TickWorld(World, () => { return sector.ActiveFloorMove != null; }, () =>
            {
                // The dropped clipped should be destroyed at this point.
                if (floorMove.MoveDirection == MoveDirection.Up && sector.Floor.Z > -14)
                {
                    monster.Height.Should().Be(0);
                    monster.Flags.DontGib.Should().BeTrue();
                    monster.Flags.Solid.Should().BeFalse();
                    monster.Frame.Sprite.Should().Be("POL5");
                }

                monster.Position.Z.Should().Be(sector.Floor.Z);
            });
        }

        [Fact(DisplayName = "Lift movement with init linked monster")]
        public void TestInitLinking()
        {
            // The monster will start at -128 clipped into the lift.
            // Because doom initialized things forced to the floor on their center when the lift activates it will be forced to the lift height.
            var sector = GameActions.GetSectorByTag(World, 1);
            var monster = GameActions.CreateEntity(World, Zombieman, new Vec3D(64, 456, -128));
            monster.Position.Z.Should().Be(-128);
            GameActions.ActivateLine(World, Player, LiftLine1, ActivationContext.UseLine).Should().BeTrue();
            sector.ActiveFloorMove.Should().NotBeNull();
            var floorMove = sector.ActiveFloorMove!;

            GameActions.TickWorld(World, () => { return floorMove.MoveStatus != SectorMoveStatus.Blocked; }, () =>
            {
                monster.Position.Z.Should().Be(sector.Floor.Z);
            });

            // Need to destroy the monster for the lift to complete.
            GameActions.DestroyCreatedEntities(World);
            GameActions.RunSectorPlaneSpecial(World, sector);
        }
    }
}
