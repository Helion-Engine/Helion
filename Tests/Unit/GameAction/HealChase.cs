using FluentAssertions;
using Helion.Dehacked;
using Helion.Resources.IWad;
using Helion.World.Entities.Definition.States;
using Helion.World.Impl.SinglePlayer;
using Xunit;
using static Helion.World.Entities.Entity;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class HealChase
{
    private readonly SinglePlayerWorld World;

    public HealChase()
    {
        World = WorldAllocator.LoadMap("Resources/box.zip", "box.wad", "MAP01", GetType().Name, WorldInit, IWadType.Doom2, cacheWorld: false);
    }

    private void WorldInit(SinglePlayerWorld world)
    {
        world.ArchiveCollection.Definitions.DehackedDefinition = new();
        DehackedApplier applier = new(world.ArchiveCollection.Definitions, world.ArchiveCollection.Definitions.DehackedDefinition);
        applier.Apply(world.ArchiveCollection.Definitions.DehackedDefinition, world.ArchiveCollection.Definitions, world.EntityManager.DefinitionComposer);
    }

    [Fact(DisplayName = "Vile chase raises monster")]
    public void VileChaseRaise()
    {
        var imp = GameActions.CreateEntity(World, "DoomImp", (-320, -320, 0));
        imp.Kill(null);
        imp.IsDead.Should().BeTrue();
        GameActions.TickWorld(World, 35);
        var archvile = GameActions.CreateEntity(World, "Archvile", (-272, -320, 0));
        archvile.SetTarget(World.Player);
        archvile.SetMoveDirection(MoveDir.West);

        EntityActionFunctions.A_VileChase(archvile);
        GameActions.AssertSound(World, imp, "dsslop");
        GameActions.TickWorld(World, 35);
        imp.IsDead.Should().BeFalse();
        imp.Flags.Solid.Should().BeTrue();
        imp.Height.Should().Be(56);
        archvile.Target.Entity.Should().Be(World.Player);
    }

    [Fact(DisplayName = "Heal chase raises monster")]
    public void HealChaseRaise()
    {
        var imp = GameActions.CreateEntity(World, "DoomImp", (-320, -320, 0));
        imp.Kill(null);
        imp.IsDead.Should().BeTrue();
        GameActions.TickWorld(World, 35);
        var archvile = GameActions.CreateEntity(World, "Archvile", (-272, -320, 0));
        archvile.SetTarget(World.Player);
        archvile.SetMoveDirection(MoveDir.West);

        // Skeleton state
        archvile.FrameState.Frame.DehackedArgs1 = 337;
        // Pistol sound
        archvile.FrameState.Frame.DehackedArgs2 = 1;

        EntityActionFunctions.A_HealChase(archvile);
        GameActions.AssertSound(World, imp, "dspistol");
        archvile.FrameState.Frame.Sprite.Should().Be("SKEL");
        GameActions.TickWorld(World, 35);
        imp.IsDead.Should().BeFalse();
        imp.Flags.Solid.Should().BeTrue();
        imp.Height.Should().Be(56);
        archvile.Target.Entity.Should().Be(World.Player);
    }

    [Fact(DisplayName = "Vile chase does not raise monster because it overlaps with another monster")]
    public void VileChaseFailesWithMonsterOverlap()
    {
        var imp = GameActions.CreateEntity(World, "DoomImp", (-320, -320, 0));
        imp.Kill(null);
        imp.IsDead.Should().BeTrue();
        GameActions.TickWorld(World, 35);
        var archvile = GameActions.CreateEntity(World, "Archvile", (-256, -320, 0));
        GameActions.CreateEntity(World, "ZombieMan", (-332, -320, 0));
        archvile.SetTarget(World.Player);
        archvile.SetMoveDirection(MoveDir.West);

        EntityActionFunctions.A_VileChase(archvile);
        GameActions.TickWorld(World, 35);
        imp.IsDead.Should().BeTrue();
    }

    [Fact(DisplayName = "Vile chase does not raise monster because it overlaps with archvile")]
    public void VileChaseFailesWithArchvileOverlap()
    {
        var imp = GameActions.CreateEntity(World, "DoomImp", (-320, -320, 0));
        imp.Kill(null);
        imp.IsDead.Should().BeTrue();
        GameActions.TickWorld(World, 35);
        var archvile = GameActions.CreateEntity(World, "Archvile", (-256, -320, 0));
        archvile.SetTarget(World.Player);
        archvile.SetMoveDirection(MoveDir.West);

        EntityActionFunctions.A_VileChase(archvile);
        GameActions.TickWorld(World, 35);
        imp.IsDead.Should().BeTrue();
    }
}
