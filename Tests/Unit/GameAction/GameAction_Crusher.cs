using FluentAssertions;
using Helion.World;
using Helion.World.Geometry.Sectors;
using Helion.World.Special.SectorMovement;
using Helion.World.Special.Specials;

namespace Helion.Tests.Unit.GameAction
{
    public static partial class GameActions
    {
        public static void RunCrusherCeiling(WorldBase world, Sector sector, int speed, bool slowDownOnCrush, double destZ = double.MaxValue, bool repeat = true) =>
            RunCrusherPlane(world, sector, sector.Ceiling, speed, slowDownOnCrush, destZ, repeat);

        public static void RunCrusherFloor(WorldBase world, Sector sector, int speed, bool slowDownOnCrush, double destZ = double.MaxValue, bool repeat = true) =>
            RunCrusherPlane(world, sector, sector.Floor, speed, slowDownOnCrush, destZ, repeat);

        public static void RunCrusherPlane(WorldBase world, Sector sector, SectorPlane plane, int speed, bool slowDownOnCrush,
            double setDestZ = double.MaxValue, bool repeat = true)
        {
            var moveSpecial = (sector.GetActiveMoveSpecial(plane)! as SectorMoveSpecial)!;
            MoveDirection startDir = sector.Ceiling.Equals(plane) ? MoveDirection.Down : MoveDirection.Up;
            MoveDirection altDir = startDir == MoveDirection.Down ? MoveDirection.Up : MoveDirection.Down;
            moveSpecial.MoveDirection.Should().Be(startDir);

            double destZ = moveSpecial.MoveDirection == MoveDirection.Down ? sector.Floor.Z + 8 : sector.Ceiling.Z - 8;
            if (setDestZ != double.MaxValue)
                destZ = setDestZ;
            double move = startDir == MoveDirection.Down ? -GetMovementPerTick(speed) : GetMovementPerTick(speed);
            double slowMove = startDir == MoveDirection.Down ? -0.1 : 0.1;

            bool isCrushing = false;

            TickWorld(world, () => { return plane.Z != destZ; }, () =>
            {
                // Crusher will slow down once hitting a thing until it returns.
                if (slowDownOnCrush && !isCrushing)
                {
                    var node = sector.Entities.Head;
                    while (node != null)
                    {
                        var entity = node.Value;
                        if (entity.Flags.Shootable && entity.IsCrushing())
                        {
                            isCrushing = true;
                            break;
                        }

                        node = node.Next;
                    }
                }

                if (isCrushing)
                    moveSpecial.MoveSpeed.Should().Be(slowMove);
                else
                    moveSpecial.MoveSpeed.Should().Be(move);
            });

            var node = sector.Entities.Head;
            while (node != null)
            {
                node.Value.IsDead.Should().BeTrue();
                node = node.Next;
            }

            if (!repeat)
            {
                sector.GetActiveMoveSpecial(plane).Should().BeNull();
                return;
            }

            // Crushers keep moving
            move = -move;
            moveSpecial.Should().NotBeNull();
            moveSpecial.MoveDirection.Should().Be(altDir);
            moveSpecial.MoveSpeed.Should().Be(move);

            // Check alternating movement
            TickWorld(world, () => { return moveSpecial.MoveDirection == altDir; }, 
                () => { });

            moveSpecial.Should().NotBeNull();
            moveSpecial.MoveDirection.Should().Be(startDir);
        }
    }
}