using FluentAssertions;
using Helion.Maps.Specials.Vanilla;
using Helion.World;
using Helion.World.Geometry.Sectors;
using Helion.World.Special;

namespace Helion.Tests.Unit.GameAction
{
    public static partial class GameActions
    {
        public static void RunDoorOpenClose(WorldBase world, Sector doorSector, double startZ, double destZ, int speed) =>
            RunDoor(world, doorSector, startZ, destZ, speed, delay: VanillaConstants.DoorDelay, close: true);

        public static void RunDoorOpenStay(WorldBase world, Sector doorSector, double destZ, int speed) =>
            RunDoor(world, doorSector, double.MaxValue, destZ, speed, delay: 0, close: false);

        public static void RunDoor(WorldBase world, Sector doorSector, double startZ, double destZ, int speed, int delay, bool close)
        {
            double z = doorSector.Ceiling.Z;
            destZ -= VanillaConstants.DoorDestOffset;
            int moveTicks = CalculateDoorMoveTicks(z, destZ, speed);
            double move = GetMovementPerTick(speed);

            doorSector.ActiveCeilingMove.Should().NotBeNull();

            // Open
            TickWorld(world, moveTicks, () =>
            {
                z = MoveZ(z, move, destZ);
                doorSector.Ceiling.Z.Should().Be(z);
            });

            if (delay > 0)
            {
                doorSector.ActiveCeilingMove.Should().NotBeNull();
                doorSector.ActiveCeilingMove!.DelayTics.Should().Be(delay);

                TickWorld(world, delay, () =>
                {
                    doorSector.Ceiling.Z.Should().Be(z);
                });
            }

            if (close)
            {
                move = -move;
                // Have to recalculate move ticks here. If we ran simulation for entity to cross the line the door can be opened slightly by a tick.
                moveTicks = CalculateMoveTicks(doorSector.Ceiling.Z, doorSector.Floor.Z, speed);
                TickWorld(world, moveTicks, () =>
                {
                    z = MoveZ(z, move, startZ);
                    doorSector.Ceiling.Z.Should().Be(z);
                });
            }

            doorSector.ActiveCeilingMove.Should().BeNull();
        }

        public static void RunDoorOpen(WorldBase world, Sector doorSector, double destZ, int speed, bool includeDoorLip)
        {
            double z = doorSector.Ceiling.Z;
            int moveTicks;

            doorSector.ActiveCeilingMove.Should().NotBeNull();

            if (includeDoorLip)
            {
                moveTicks = CalculateDoorMoveTicks(z, destZ, speed);
                destZ -= VanillaConstants.DoorDestOffset;
            }
            else
            {
                moveTicks = CalculateMoveTicks(z, destZ, speed);
            }

            double move = GetMovementPerTick(speed);

            TickWorld(world, moveTicks, () =>
            {
                z = MoveZ(z, move, destZ);
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
            double move = -GetMovementPerTick(speed);

            doorSector.ActiveCeilingMove.Should().NotBeNull();

            TickWorld(world, moveTicks, () =>
            {
                z = MoveZ(z, move, destZ);
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
                return CalculateDiff(start, end, move);

            return CalculateDiff(end, start, move);
        }

        private static int CalculateDiff(double start, double end, double move)
        {
            double diff = end - start;
            double ret = diff / move;
            if (diff % move != 0)
                ret += 1;
            return (int)ret;
        }

        private static double GetMovementPerTick(int speed) => speed * SpecialManager.SpeedFactor;
    }
}
