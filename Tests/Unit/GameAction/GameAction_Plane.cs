using FluentAssertions;
using Helion.World;
using Helion.World.Geometry.Sectors;
using Helion.World.Special.SectorMovement;
using System.Collections.Generic;
using System.Linq;

namespace Helion.Tests.Unit.GameAction
{
    public static partial class GameActions
    {
        public static void RunFloorRaise(WorldBase world, Sector floorSector, double destZ, int speed, bool isSpecDestroyed = true) =>
            RunPlane(world, floorSector, destZ, speed, MoveDirection.Up, floorSector.Floor, isSpecDestroyed);
        public static void RunFloorLower(WorldBase world, Sector floorSector, double destZ, int speed, bool isSpecDestroyed = true) =>
            RunPlane(world, floorSector, destZ, speed, MoveDirection.Down, floorSector.Floor, isSpecDestroyed);
        public static void RunCeilingRaise(WorldBase world, Sector ceilingSector, double destZ, int speed, bool isSpecDestroyed = true) =>
            RunPlane(world, ceilingSector, destZ, speed, MoveDirection.Up, ceilingSector.Ceiling, isSpecDestroyed);
        public static void RunCeilingLower(WorldBase world, Sector ceilingSector, double destZ, int speed, bool isSpecDestroyed = true) =>
            RunPlane(world, ceilingSector, destZ, speed, MoveDirection.Down, ceilingSector.Ceiling, isSpecDestroyed);

        // Does not verify speeds, returns when there are no ActiveFloorMove or ActiveCeilingMove attached to the sectors.
        public static void RunSectorPlaneSpecials(WorldBase world, IEnumerable<Sector> sectors)
        {
            TickWorld(world, () => { return sectors.Any(x => x.ActiveFloorMove != null || x.ActiveCeilingMove != null); },
                () => { });
        }

        public static void RunPerpetualMovingFloor(WorldBase world, Sector sector, double lowZ, double highZ, int speed, int delay)
        {
            sector.ActiveFloorMove.Should().NotBeNull();
            var special = sector.ActiveFloorMove!;

            for (int i = 0; i < 2; i++)
            {
                // Start direction is randomized
                MoveDirection dir = special.MoveDirection;
                if (dir == MoveDirection.Down)
                    RunFloorLower(world, sector, lowZ, speed, isSpecDestroyed: false);
                else
                    RunFloorRaise(world, sector, highZ, speed, isSpecDestroyed: false);

                double z = sector.Floor.Z;
                TickWorld(world, delay, () =>
                {
                    sector.Floor.Z.Should().Be(z);
                });
            }
        }

        private static void RunPlane(WorldBase world, Sector sector, double destZ, int speed, MoveDirection dir, 
            SectorPlane plane, bool isSpecDestroyed)
        {
            sector.GetActiveMoveSpecial(plane).Should().NotBeNull();

            double z = plane.Z;
            int moveTicks = CalculateMoveTicks(z, destZ, speed, 0);
            double move = GetMovementPerTick(speed);

            if (dir == MoveDirection.Down)
                move = -move;

            TickWorld(world, moveTicks, () =>
            {
                z += move;
                plane.Z.Should().Be(z);
            });

            // If we activate something that is already at it's dest then it needs to run a tick to complete.
            if (moveTicks == 0)
                world.Tick();

            if (isSpecDestroyed)
                sector.GetActiveMoveSpecial(plane).Should().BeNull();
            else
                sector.GetActiveMoveSpecial(plane).Should().NotBeNull();
        }
    }
}