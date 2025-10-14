using FluentAssertions;
using Helion.Geometry.Vectors;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Boom;

public partial class BoomActions
{

    [Fact(DisplayName = "Scrolling floor moves barrel and not teleport dest")]
    public void ScrollingFloorMovesBarrel()
    {
        var scrollSector = GameActions.GetSectorByTag(World, 9);
        var teleportDest = GameActions.GetSectorEntity(World, 47, "TeleportDest");
        var barrel = GameActions.GetSectorEntity(World, 47, "ExplosiveBarrel");

        teleportDest.Sector.Should().Be(scrollSector);
        barrel.Sector.Should().Be(scrollSector);

        GameActions.TickWorld(World, 1);

        teleportDest.Velocity.Should().Be(Vec3D.Zero);
        barrel.Velocity.Should().NotBe(Vec3D.Zero);
    }

    [Fact(DisplayName = "Scrolling floor doesn't move entity in air")]
    public void ScrollingFloorEntityInAir()
    {
        var scrollSector = GameActions.GetSectorByTag(World, 9);
        var clip = GameActions.CreateEntity(World, "Clip", (-192, 928, 32));
        clip.Sector.Should().Be(scrollSector);

        GameActions.TickWorld(World, 1);

        clip.Velocity.XY.Should().Be(Vec2D.Zero);

        clip.UnlinkFromWorld();
        clip.Position = (-192, 928, 0);
        World.Link(clip);

        GameActions.TickWorld(World, 1);
        clip.Velocity.XY.Should().NotBe(Vec2D.Zero);

        World.EntityManager.Destroy(clip);
    }

    [Fact(DisplayName = "Scrolling floor doesn't move rocket")]
    public void ScrollingFloorRocket()
    {
        var scrollSector = GameActions.GetSectorByTag(World, 9);
        var rocket = GameActions.CreateEntity(World, "Rocket", (-192, 928, 0));
        rocket.Sector.Should().Be(scrollSector);

        GameActions.TickWorld(World, 1);

        rocket.Velocity.Should().Be(Vec3D.Zero);
        World.EntityManager.Destroy(rocket);
    }

    [Fact(DisplayName = "Scrolling floor moves no blockmap entity")]
    public void ScrollingFloorNoBlockmapThing()
    {
        var scrollSector = GameActions.GetSectorByTag(World, 9);
        var entity = GameActions.CreateEntity(World, "ZombieMan", (-192, 928, 0));
        entity.Flags.NoBlockmap = true;
        entity.Sector.Should().Be(scrollSector);

        GameActions.TickWorld(World, 1);

        entity.Velocity.Should().NotBe(Vec3D.Zero);
        World.EntityManager.Destroy(entity);
    }

    [Fact(DisplayName = "Thing that is considered underwater should move with scrolling floor")]
    public void ScrollingFloorUnderwater()
    {
        var scrollSector = GameActions.GetSectorByTag(World, 10);
        var rocket = GameActions.CreateEntity(World, "LostSoul", (-192, 736, 0));
        rocket.Sector.Should().Be(scrollSector);

        GameActions.TickWorld(World, 1);

        rocket.Velocity.XY.Should().Be(Vec2D.Zero);

        rocket.UnlinkFromWorld();
        rocket.Position = (-192, 736, -48);
        World.Link(rocket);

        GameActions.TickWorld(World, 1);

        rocket.OnGround.Should().BeFalse();
        rocket.Velocity.XY.Should().NotBe(Vec2D.Zero);

        World.EntityManager.Destroy(rocket);
    }

    [Fact(DisplayName = "NoBlockmap thing that is considered underwater should move with scrolling floor")]
    public void ScrollingFloorUnderwaterNoBlockmap()
    {
        var scrollSector = GameActions.GetSectorByTag(World, 10);
        var teleport = GameActions.CreateEntity(World, "Rocket", (-192, 736, 0));
        teleport.Flags.NoBlockmap.Should().BeTrue();
        teleport.Flags.NoGravity.Should().BeTrue();
        teleport.Sector.Should().Be(scrollSector);

        GameActions.TickWorld(World, 1);

        teleport.Velocity.XY.Should().Be(Vec2D.Zero);

        teleport.UnlinkFromWorld();
        teleport.Position = (-192, 736, -48);
        World.Link(teleport);

        GameActions.TickWorld(World, 1);

        teleport.OnGround.Should().BeFalse();
        teleport.Velocity.XY.Should().NotBe(Vec2D.Zero);

        World.EntityManager.Destroy(teleport);
    }

    [Fact(DisplayName = "Scrolling floor that pushes the velocity past MaxMoveXY")]
    public void ScrollingFloorHitsMaxMove()
    {
        Player.Velocity = Vec3D.Zero;
        GameActions.SetEntityPosition(World, Player, (-832, -192));
        GameActions.TickWorld(World, 35);
        Player.Velocity.Y.Should().Be(31.3125);
        GameActions.TickWorld(World, 35);
        Player.Velocity.Y.Should().Be(31.3125);
    }
}
