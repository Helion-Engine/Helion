using FluentAssertions;
using Helion.World;
using Helion.World.Entities;
using Helion.World.Geometry.Sectors;

namespace Helion.Tests.Unit.GameAction
{
    public static partial class GameActions
    {
        public static void RunTeleport(WorldBase world, Entity entity, Sector sector, int teleportLandingId)
        {
            Entity teleportLanding = GetEntity(world, teleportLandingId);
            world.Tick();
            entity.Sector.Id.Should().Be(sector.Id);
            entity.Position.Should().Be(teleportLanding.Position);
        }
    }
}
