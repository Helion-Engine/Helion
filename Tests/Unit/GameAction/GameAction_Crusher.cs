using FluentAssertions;
using Helion.World;
using Helion.World.Geometry.Sectors;
using Helion.World.Special.SectorMovement;

namespace Helion.Tests.Unit.GameAction
{
    public static partial class GameActions
    {
        public static void RunCrusherCeiling(WorldBase world, Sector sector, int speed, bool slowDownOnCrush)
        {
            var moveSpecial = sector.ActiveCeilingMove!;
            moveSpecial.MoveDirection.Should().Be(MoveDirection.Down);
            double floorZ = sector.Floor.Z + 8;
            double move = GetMovementPerTick(speed);
            bool isCrushing = false;

            TickWorld(world, () => { return sector.Ceiling.Z != floorZ; }, () =>
            {
                // Crusher will slow down once hitting a thing until it returns.
                if (slowDownOnCrush && !isCrushing)
                {
                    var node = sector.Entities.Head;
                    while (node != null)
                    {
                        var entity = node.Value;
                        if (!entity.IsDead && entity.IsCrushing())
                        {
                            isCrushing = true;
                            break;
                        }

                        node = node.Next;
                    }
                }

                if (isCrushing)
                    moveSpecial.MoveSpeed.Should().Be(-0.1);
                else
                    moveSpecial.MoveSpeed.Should().Be(-move);
            });

            var node = sector.Entities.Head;
            while (node != null)
            {
                node.Value.IsDead.Should().BeTrue();
                node = node.Next;
            }

            // Crushers keep moving
            moveSpecial.Should().NotBeNull();
            moveSpecial.MoveDirection.Should().Be(MoveDirection.Up);
            moveSpecial.MoveSpeed.Should().Be(move);

            // Check alternating movement
            TickWorld(world, () => { return moveSpecial.MoveDirection == MoveDirection.Up; }, 
                () => { });

            moveSpecial.Should().NotBeNull();
            moveSpecial.MoveDirection.Should().Be(MoveDirection.Down);
        }
    }
}