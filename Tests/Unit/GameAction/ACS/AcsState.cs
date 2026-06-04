using FluentAssertions;
using Helion.Models;
using Helion.Resources.IWad;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using System;
using Xunit;

namespace Helion.Tests.Unit.GameAction.ACS;

[Collection("GameActions")]
public class AcsState : IDisposable
{
    private SinglePlayerWorld PreviousWorld;
    private SinglePlayerWorld NewWorld;

    public AcsState()
    {
        PreviousWorld = LoadMap(null, "MAP01", (world) => { }, disposeExistingWorld: true, disposeArchiveCollection: false);
        NewWorld = PreviousWorld;
    }

    public void Dispose()
    {
        GC.SuppressFinalize(this);
        PreviousWorld.ArchiveCollection.Dispose();
        PreviousWorld.Dispose();
    }

    [Fact(DisplayName = "Save and load ACS state")]
    public void SaveAndLoadState()
    {
        RunThingCountScript(PreviousWorld);

        var model = PreviousWorld.ToWorldModel();
        NewWorld = LoadMap(model, "MAP01", (world) => { }, disposeArchiveCollection: true);

        CompleteThingCountScript(NewWorld);
    }

    [Fact(DisplayName = "Save and load ACS state from different map")]
    public void SaveAndLoadStateDifferentMap()
    {
        RunThingCountScript(PreviousWorld);

        var model = PreviousWorld.ToWorldModel();
        NewWorld = LoadMap(null, "MAP02", (world) => { }, disposeArchiveCollection: true);
        PreviousWorld = NewWorld;
        NewWorld = LoadMap(model, "MAP01", (world) => { }, disposeArchiveCollection: true);

        CompleteThingCountScript(NewWorld);

        // Ensure that scripts can still execute and not crash. This should just print that you already completed it.
        GameActions.WithPlayerMessages(NewWorld, (messages) =>
        {
            ActivateThingCountScript(NewWorld);
            GameActions.TickWorld(NewWorld, 1);
            messages.Count.Should().Be(1);
            messages[0].Args.Message.Should().Be("Objective complete");
        });
    }

    private static void ActivateThingCountScript(SinglePlayerWorld world)
    {
        GameActions.ActivateLine(world, world.Player, 23, ActivationContext.UseLine).Should().BeTrue();
    }

    // Starts the thing count script waiting for all zombiemen to die. Kills one and leaves the other alive.
    private static void RunThingCountScript(SinglePlayerWorld world)
    {
        var sector = GameActions.GetSectorByTag(world, 2);
        var zombies = GameActions.GetEntities(world, "Zombieman");
        zombies.Count.Should().Be(2);
        sector.Floor.Z.Should().Be(64);
        ActivateThingCountScript(world);
        GameActions.TickWorld(world, 23);
        zombies[0].Kill(null);

        GameActions.TickWorld(world, 10);
        sector.Floor.Z.Should().Be(64);
    }

    // Kills the final zombieman and asserts the platforms lowers.
    private static void CompleteThingCountScript(SinglePlayerWorld world)
    {
        var sector = GameActions.GetSectorByTag(world, 2);
        var zombies = GameActions.GetEntities(world, "Zombieman");
        zombies.Count.Should().Be(2);
        sector.Floor.Z.Should().Be(64);
        zombies[0].IsDead().Should().BeTrue();
        zombies[1].IsDead().Should().BeFalse();

        GameActions.TickWorld(world, 10);
        sector.Floor.Z.Should().Be(64);

        zombies[1].Kill(null);
        GameActions.TickWorld(world, 10);
        sector.ActiveFloorMove.Should().NotBeNull();
        GameActions.RunSectorPlaneSpecial(world, sector);
        sector.Floor.Z.Should().Be(8);
    }

    private SinglePlayerWorld LoadMap(WorldModel? worldModel, string mapName, Action<SinglePlayerWorld> onInit, bool disposeExistingWorld = false, bool disposeArchiveCollection = false)
    {
        // Need to dispose the first world's archive collection because it locks the file
        if (disposeArchiveCollection && !PreviousWorld.ArchiveCollection.IsDisposed)
            PreviousWorld.ArchiveCollection.Dispose();

        return WorldAllocator.LoadMap("Resources/acs-scripts.zip", "acs-scripts.wad", mapName, Guid.NewGuid().ToString(), onInit, IWadType.Doom2,
            worldModel: worldModel, disposeExistingWorld: disposeExistingWorld);
    }
}
