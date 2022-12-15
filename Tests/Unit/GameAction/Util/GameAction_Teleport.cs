using FluentAssertions;
using Helion.World;
using Helion.World.Entities;
using Helion.World.Geometry.Sectors;

namespace Helion.Tests.Unit.GameAction;

public static partial class GameActions
{
    public static void RunTeleport(WorldBase world, Entity entity, Sector sector, int teleportLandingId)
    {
        Entity teleportLanding = GetEntity(world, teleportLandingId);
        world.Tick();
        entity.Sector.Id.Should().Be(sector.Id);
        entity.Position.XY.Should().Be(teleportLanding.Position.XY);
        entity.Position.Z.Should().Be(sector.Floor.Z);
    }

    public static void CheckNoTeleport(WorldBase world, Entity entity, Sector sector, int teleportLandingId)
    {
        Entity teleportLanding = GetEntity(world, teleportLandingId);
        world.Tick();
        entity.Sector.Id.Should().NotBe(sector.Id);
        entity.Position.XY.Should().NotBe(teleportLanding.Position.XY);
    }
}
