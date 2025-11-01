using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.Tests.Unit.GameAction.Util;
using Helion.World;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Dehacked;

[Collection("GameActions")]
public class NullState
{
    private readonly SinglePlayerWorld World;

    public NullState()
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
}
