using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Dehacked;

[Collection("GameActions")]
public class FrameStates
{
    private readonly SinglePlayerWorld World;

    public FrameStates()
    {
        World = WorldAllocator.LoadMap("Resources/box.zip", "box.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact(DisplayName = "Setting null death state through missile removes entity")]
    public void NullDeathStateRemovesEntity()
    {
        var def = World.EntityManager.DefinitionComposer.GetByName("DoomImp")!;
        def.DeathState = null;
        def.Flags.SetSpawnCeiling();
        def.Flags.SetMissile();
        
        var entity = GameActions.CreateEntity(World, "DoomImp", Vec3D.Zero, initSpawn: true);
        entity.IsDisposed.Should().BeFalse();
        GameActions.TickWorld(World, 35);
        entity.IsDisposed.Should().BeTrue();
    }

    [Fact(DisplayName = "Zero tick duration spawn state set -1 and loops")]
    public void ZeroTickDurationSpawnState()
    {
        // The tick function didn't have the same check and wouldn't let a zero duration frame immediately go to the next.
        // It would decrement and check if it didn't equal zero and this causes it to be infinitely in a -1 frame.
        var def = World.EntityManager.DefinitionComposer.GetByName("DoomImp")!;
        var frame = World.ArchiveCollection.EntityFrameTable.Frames[def.SpawnState!.Value];
        frame.Ticks = 0;
        frame.NextFrameIndex = frame.MasterFrameIndex;

        var entity = GameActions.CreateEntity(World, "DoomImp", Vec3D.Zero, initSpawn: true);
        entity.FrameState.Frame.Should().Be(frame);
        entity.FrameState.CurrentTick.Should().Be(0);
        entity.Tick();
        entity.FrameState.Frame.Should().Be(frame);
        entity.FrameState.CurrentTick.Should().Be(-1);
        entity.Tick();
        entity.FrameState.Frame.Should().Be(frame);
        entity.FrameState.CurrentTick.Should().Be(-1);
    }

    [Fact(DisplayName = "Zero tick duration after spawn state automatically goes to next frame")]
    public void ZeroTickDurationAfterSpawnState()
    {
        var def = World.EntityManager.DefinitionComposer.GetByName("DoomImp")!;
        var frame = World.ArchiveCollection.EntityFrameTable.Frames[def.SpawnState!.Value];
        frame.ActionFunction = null;
        frame.Ticks = 1;
        frame.NextFrameIndex = frame.MasterFrameIndex + 1;
        var nextFrame = frame.NextFrame;
        nextFrame.ActionFunction = null;
        nextFrame.Ticks = 0;
        nextFrame.NextFrameIndex = nextFrame.MasterFrameIndex + 1;
        var lastFrame = nextFrame.NextFrame;
        lastFrame.ActionFunction = null;
        lastFrame.Ticks = 69;

        var entity = GameActions.CreateEntity(World, "DoomImp", Vec3D.Zero, initSpawn: true);
        entity.FrameState.Frame.Should().Be(frame);
        entity.FrameState.CurrentTick.Should().Be(1);
        entity.Tick();
        entity.FrameState.Frame.Should().Be(lastFrame);
        entity.FrameState.CurrentTick.Should().Be(69);
    }
}
