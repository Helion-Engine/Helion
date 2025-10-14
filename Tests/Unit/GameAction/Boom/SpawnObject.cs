using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Boom;

[Collection("GameActions")]
public class SpawnObject
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public SpawnObject()
    {
        World = WorldAllocator.LoadMap("Resources/spawn_object.zip", "spawn_object.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2, cacheWorld: false);
    }

    [Fact(DisplayName = "A_SpawnObject 1")]
    public void SpawnObject1()
    {
        GameActions.ActivateLine(World, Player, 41, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, 35);
        var imp = GameActions.GetSectorEntity(World, 1, "DoomImp");
        var teleport = GameActions.GetSectorEntity(World, 1, "TeleportDest");

        World.EntityManager.TeleportSpots.Find(teleport).Should().NotBeNull();  

        imp.Position.ApproxEquals((-130, -184, 0)).Should().BeTrue();
        teleport.Position.ApproxEquals((-130, -184, 0)).Should().BeTrue();
    }

    [Fact(DisplayName = "A_SpawnObject 2")]
    public void SpawnObject2()
    {
        GameActions.ActivateLine(World, Player, 38, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, 35);
        var imp = GameActions.GetSectorEntity(World, 1, "DoomImp");
        var teleport = GameActions.GetSectorEntity(World, 1, "TeleportDest");

        World.EntityManager.TeleportSpots.Find(teleport).Should().NotBeNull();

        imp.Position.ApproxEquals((-130, -218, 0)).Should().BeTrue();
        teleport.Position.ApproxEquals((-130, -218, 0)).Should().BeTrue();
    }

    [Fact(DisplayName = "A_SpawnObject 3")]
    public void SpawnObject3()
    {
        GameActions.ActivateLine(World, Player, 39, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, 35);
        var revenant = GameActions.GetSectorEntity(World, 2, "Revenant");
        var teleport = GameActions.GetSectorEntity(World, 2, "TeleportDest");

        World.EntityManager.TeleportSpots.Find(teleport).Should().NotBeNull();

        revenant.Position.ApproxEquals((142, -202, 0)).Should().BeTrue();
        teleport.Position.ApproxEquals((142, -202, 0)).Should().BeTrue();
    }
}
