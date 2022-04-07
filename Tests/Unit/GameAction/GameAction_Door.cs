using FluentAssertions;
using Helion.Maps.Specials.Vanilla;
using Helion.World;
using Helion.World.Geometry.Sectors;
using Helion.World.Special;

namespace Helion.Tests.Unit.GameAction
{
    public static partial class GameActions
    {
        public static void RunDoorOpenClose(WorldBase world, Sector doorSector, double destZ, int speed) =>
            RunDoor(world, doorSector, destZ, speed, delay: VanillaConstants.DoorDelay, close: true);

        public static void RunDoorOpenStay(WorldBase world, Sector doorSector, double destZ, int speed) =>
            RunDoor(world, doorSector, destZ, speed, delay: 0, close: false);

        public static void RunDoor(WorldBase world, Sector doorSector, double destZ, int speed, int delay, bool close)
        {
            double z = doorSector.Ceiling.Z;
            int moveTicks = CalculateDoorMoveTicks(z, destZ, speed);
            double move = GetMovementPerTick(speed);

            // Open
            TickWorld(world, moveTicks, () =>
            {
                z += move;
                doorSector.Ceiling.Z.Should().Be(z);
            });

            if (delay > 0)
            {
                doorSector.ActiveCeilingMove.DelayTics.Should().Be(delay);

                TickWorld(world, delay, () =>
                {
                    doorSector.Ceiling.Z.Should().Be(z);
                });
            }

            if (close)
            {
                // Have to recalculate move ticks here. If we ran simulation for entity to cross the line the door can be opened slightly by a tick.
                moveTicks = CalculateMoveTicks(doorSector.Ceiling.Z, doorSector.Floor.Z, speed);
                TickWorld(world, moveTicks, () =>
                {
                    z -= move;
                    doorSector.Ceiling.Z.Should().Be(z);
                });
            }

            doorSector.ActiveCeilingMove.Should().BeNull();
        }

        public static void RunDoorOpen(WorldBase world, Sector doorSector, double destZ, int speed, bool includeDoorLip)
        {
            double z = doorSector.Ceiling.Z;
            int moveTicks;

            if (includeDoorLip)
                moveTicks = CalculateDoorMoveTicks(z, destZ, speed);
            else
                moveTicks = CalculateMoveTicks(z, destZ, speed);

            double move = GetMovementPerTick(speed);

            TickWorld(world, moveTicks, () =>
            {
                z += move;
                doorSector.Ceiling.Z.Should().Be(z);
            });

            if (moveTicks == 0)
                world.Tick();

            doorSector.ActiveCeilingMove.Should().BeNull();
        }

        public static void RunDoorClose(WorldBase world, Sector doorSector, double destZ, int speed, bool checkCeilingMove = true)
        {
            double z = doorSector.Ceiling.Z;
            int moveTicks = CalculateMoveTicks(z, destZ, speed);
            double move = GetMovementPerTick(speed);

            TickWorld(world, moveTicks, () =>
            {
                z -= move;
                doorSector.Ceiling.Z.Should().Be(z);
            });

            if (checkCeilingMove)
                doorSector.ActiveCeilingMove.Should().BeNull();
        }

        private static int CalculateDoorMoveTicks(double start, double end, int speed) =>
            CalculateMoveTicks(start, end, speed, VanillaConstants.DoorDestOffset);

        private static int CalculateMoveTicks(double start, double end, int speed, int offset = 0)
        {
            double move = GetMovementPerTick(speed);
            if (start < end)
                return (int)((end - start - offset) / move);

            return (int)((start - end - offset) / move);
        }

        private static double GetMovementPerTick(int speed) => speed * SpecialManager.SpeedFactor;
    }
}
