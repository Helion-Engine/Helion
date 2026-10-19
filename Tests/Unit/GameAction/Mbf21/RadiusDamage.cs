using FluentAssertions;
using Helion.Dehacked;
using Helion.Resources.IWad;
using Helion.World.Entities.Definition.States;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using System;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Mbf21;

[Collection("GameActions")]
public class RadiusDamage : IDisposable
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public RadiusDamage()
    {
        World = WorldAllocator.LoadMap("Resources/box.zip", "box.WAD", "MAP01", GetType().Name, WorldInit, IWadType.Doom2);
    }

    private void WorldInit(SinglePlayerWorld world)
    {
        world.ArchiveCollection.Definitions.DehackedDefinition = new();
        DehackedApplier applier = new(world.ArchiveCollection.Definitions, world.ArchiveCollection.Definitions.DehackedDefinition);
        applier.Apply(world.ArchiveCollection.Definitions.DehackedDefinition, world.ArchiveCollection.Definitions, world.EntityManager.DefinitionComposer);
    }

    public void Dispose()
    {
        GameActions.DestroyCreatedEntities(World);
        GC.SuppressFinalize(this);
    }

    [Fact(DisplayName = "RadiusDamage without owner doesn't change target")]
    public void RadiusDamageDoesntChangeTarget()
    {
        var baron = GameActions.CreateEntity(World, "BaronOfHell", (-320, -320, 0));
        var thing = GameActions.CreateEntity(World, "Column", (-356, -320, 0));
        var health = baron.Health;
        baron.SetTarget(Player);
        thing.FrameState.Frame.DehackedArgs1 = 64;
        thing.FrameState.Frame.DehackedArgs2 = 256;
        EntityActionFunctions.A_RadiusDamage(thing);
        baron.Health.Should().Be(health - 64);
        baron.Target().Should().Be(Player);
    }

    [Fact(DisplayName = "RadiusDamage with owner doesn't change target")]
    public void RadiusDamageChangesTarget()
    {
        var baron = GameActions.CreateEntity(World, "BaronOfHell", (-320, -320, 0));
        var thing = GameActions.CreateEntity(World, "Column", (-356, -320, 0));
        var owner = GameActions.CreateEntity(World, "Column", (0, 0, 0));
        var health = baron.Health;
        thing.SetOwner(owner);
        baron.SetTarget(Player);
        thing.FrameState.Frame.DehackedArgs1 = 64;
        thing.FrameState.Frame.DehackedArgs2 = 256;
        EntityActionFunctions.A_RadiusDamage(thing);
        baron.Health.Should().Be(health - 64);
        baron.Target().Should().Be(owner);
    }

    [Fact(DisplayName = "RadiusDamage not affected by Z")]
    public void RadiusDamageNotAffectedByZ()
    {
        var baron = GameActions.CreateEntity(World, "BaronOfHell", (-320, -320, 0));
        var thing = GameActions.CreateEntity(World, "Column", (-356, -320, 128));
        var health = baron.Health;
        baron.SetTarget(Player);
        thing.FrameState.Frame.DehackedArgs1 = 64;
        thing.FrameState.Frame.DehackedArgs2 = 256;
        EntityActionFunctions.A_RadiusDamage(thing);
        baron.Health.Should().Be(health - 64);
        baron.Target().Should().Be(Player);
    }

    [Fact(DisplayName = "RadiusDamage large radius close")]
    public void RadiusDamageLargeRadiusClose()
    {
        var baron = GameActions.CreateEntity(World, "BaronOfHell", (-320, -320, 0));
        var thing = GameActions.CreateEntity(World, "Column", (-320, -256, 0));
        var health = baron.Health;
        baron.SetTarget(Player);
        thing.FrameState.Frame.DehackedArgs1 = 128;
        thing.FrameState.Frame.DehackedArgs2 = 256;
        EntityActionFunctions.A_RadiusDamage(thing);
        baron.Health.Should().Be(health - 128);
        baron.Target().Should().Be(Player);
    }

    [Fact(DisplayName = "RadiusDamage large radius mid")]
    public void RadiusDamageLargeRadiusMid()
    {
        var baron = GameActions.CreateEntity(World, "BaronOfHell", (-320, -320, 0));
        var thing = GameActions.CreateEntity(World, "Column", (-320, -192, 0));
        var health = baron.Health;
        baron.SetTarget(Player);
        thing.FrameState.Frame.DehackedArgs1 = 128;
        thing.FrameState.Frame.DehackedArgs2 = 256;
        EntityActionFunctions.A_RadiusDamage(thing);
        baron.Health.Should().Be(health - 128);
        baron.Target().Should().Be(Player);
    }

    [Fact(DisplayName = "RadiusDamage large radius far")]
    public void RadiusDamageLargeRadiusFar()
    {
        var baron = GameActions.CreateEntity(World, "BaronOfHell", (-320, -320, 0));
        var thing = GameActions.CreateEntity(World, "Column", (-320, -128, 0));
        var health = baron.Health;
        baron.SetTarget(Player);
        thing.FrameState.Frame.DehackedArgs1 = 128;
        thing.FrameState.Frame.DehackedArgs2 = 256;
        EntityActionFunctions.A_RadiusDamage(thing);
        baron.Health.Should().Be(health - 88);
        baron.Target().Should().Be(Player);
    }

    [Fact(DisplayName = "RadiusDamage large radius too far")]
    public void RadiusDamageLargeRadiusTooFar()
    {
        var baron = GameActions.CreateEntity(World, "BaronOfHell", (-320, -320, 0));
        var thing = GameActions.CreateEntity(World, "Column", (-320, -32, 0));
        var health = baron.Health;
        baron.SetTarget(Player);
        thing.FrameState.Frame.DehackedArgs1 = 128;
        thing.FrameState.Frame.DehackedArgs2 = 256;
        EntityActionFunctions.A_RadiusDamage(thing);
        baron.Health.Should().Be(health);
        baron.Target().Should().Be(Player);
    }
}
