using FluentAssertions;
using Helion.Maps.Specials.Vanilla;
using Helion.World;
using Helion.World.Geometry.Sectors;
using Helion.World.Special.SectorMovement;

namespace Helion.Tests.Unit.GameAction
{
    public static partial class GameActions
    {
        public static void RunFloorRaise(WorldBase world, Sector floorSector, double destZ, int speed) =>
            RunFloor(world, floorSector, destZ, speed, MoveDirection.Up);
        public static void RunFloorLower(WorldBase world, Sector floorSector, double destZ, int speed) =>
            RunFloor(world, floorSector, destZ, speed, MoveDirection.Down);

        private static void RunFloor(WorldBase world, Sector floorSector, double destZ, int speed, MoveDirection dir)
        {
            double z = floorSector.Floor.Z;
            int moveTicks = CalculateMoveTicks(z, destZ, speed, 0);
            double move = GetMovementPerTick(speed);

            if (dir == MoveDirection.Down)
                move = -move;

            TickWorld(world, moveTicks, () =>
            {
                z += move;
                floorSector.Floor.Z.Should().Be(z);
            });

            floorSector.ActiveFloorMove.Should().BeNull();
        }
    }
}