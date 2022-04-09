using FluentAssertions;
using Helion.World;
using Helion.World.Geometry.Sectors;
using MoreLinq;
using System.Collections.Generic;
using System.Linq;

namespace Helion.Tests.Unit.GameAction
{
    public static partial class GameActions
    {
        public static void RunLift(WorldBase world, Sector sector, double startZ, double destZ, int speed, int delay)
        {
            double z = sector.Floor.Z;

            int moveTicks = CalculateMoveTicks(z, destZ, speed);
            double move = -GetMovementPerTick(speed);

            TickWorld(world, moveTicks, () =>
            {
                z = MoveZ(z, move, destZ);
                sector.Floor.Z.Should().Be(z);
            });

            if (delay > 0)
            {
                sector.ActiveFloorMove.Should().NotBeNull();
                sector.ActiveFloorMove!.DelayTics.Should().Be(delay);

                TickWorld(world, delay, () =>
                {
                    sector.Floor.Z.Should().Be(z);
                });
            }

            move = -move;
            moveTicks = CalculateMoveTicks(sector.Floor.Z, startZ, speed);
            TickWorld(world, moveTicks, () =>
            {
                z = MoveZ(z, move, startZ);
                sector.Floor.Z.Should().Be(z);
            });

            sector.ActiveFloorMove.Should().BeNull();
        }
    }
}