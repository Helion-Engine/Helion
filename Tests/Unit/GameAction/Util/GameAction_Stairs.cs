using FluentAssertions;
using Helion.World;
using Helion.World.Geometry.Sectors;
using System.Collections.Generic;
using System.Linq;

namespace Helion.Tests.Unit.GameAction
{
    public static partial class GameActions
    {
        public static void RunStairs(WorldBase world, IEnumerable<int> sectorIds, double startZ, double stepHeight, int speed)
        {
            world.Tick();

            List<Sector> sectors = new();
            foreach (int sectorId in sectorIds)
                sectors.Add(GetSector(world, sectorId));
            foreach (var sector in sectors)
                sector.ActiveFloorMove.Should().NotBeNull();

            TickWorld(world, () => { return sectors.Any(x => x.ActiveFloorMove != null); }, 
                () => { });

            double z = startZ;
            foreach(var sector in sectors)
            {
                z += stepHeight;
                sector.Floor.Z.Should().Be(z);
            }
        }
    }
}
