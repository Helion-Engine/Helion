using FluentAssertions;
using Helion.Resources.Archives.Collection;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class SpawnCeiling
{
    private readonly SinglePlayerWorld World;

    public SpawnCeiling()
    {
        World = WorldAllocator.LoadMap("Resources/spawnceiling.zip", "spawnceiling.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2,
            cacheWorld: false, onBeforeInit: BeforeInit);
    }

    private void BeforeInit(ArchiveCollection archiveCollection)
    {
        // Modify the third entity to have a projectile pass height. This should be the height used for spawning from ceiling / clamping.
        var def = archiveCollection.EntityDefinitionComposer.GetByName("MEAT4");
        def.Should().NotBeNull();
        def!.Properties.ProjectilePassHeight = 16;
    }

    [Fact(DisplayName="Ceiling spawn init")]
    public void SpawnCeilingInit()
    {
        var entity1 = GameActions.GetEntity(World, 0);
        var entity2 = GameActions.GetEntity(World, 1);
        var entity3 = GameActions.GetEntity(World, 2);

        entity1.Position.Z.Should().Be(12);
        entity2.Position.Z.Should().Be(12);
        entity3.Position.Z.Should().Be(80);
    }

    [Fact(DisplayName = "Spawn ceiling and move ceiling up should not move")]
    public void SpawnCeilingMoveCeilingUp()
    {
        var sector1 = GameActions.GetSectorByTag(World, 1);
        var entity1 = GameActions.GetEntity(World, 0);
        GameActions.ActivateLine(World, World.Player, 12, ActivationContext.UseLine);
        GameActions.RunSectorPlaneSpecial(World, sector1);

        sector1.Ceiling.Z.Should().Be(128);
        entity1.Position.Z.Should().Be(12);
    }

    [Fact(DisplayName = "Spawn ceiling and move ceiling down should")]
    public void SpawnCeilingMoveCeilingDown()
    {
        var sector2 = GameActions.GetSectorByTag(World, 2);
        var entity2 = GameActions.GetEntity(World, 1);
        GameActions.ActivateLine(World, World.Player, 13, ActivationContext.UseLine);
        GameActions.RunSectorPlaneSpecial(World, sector2);

        sector2.Ceiling.Z.Should().Be(64);
        entity2.Position.Z.Should().Be(0);
    }

    [Fact(DisplayName = "Spawn ceiling and move ceiling down should move using projectile pass height")]
    public void SpawnCeilingMoveCeilingDownWithPassHeight()
    {
        var sector3 = GameActions.GetSectorByTag(World, 3);
        var entity3 = GameActions.GetEntity(World, 2);
        GameActions.ActivateLine(World, World.Player, 18, ActivationContext.UseLine);
        GameActions.RunSectorPlaneSpecial(World, sector3);

        sector3.Ceiling.Z.Should().Be(64);
        entity3.Position.Z.Should().Be(48);
    }

    [Fact(DisplayName = "Spawn ceiling and move floor down should should not move entity with projectile pass height")]
    public void SpawnCeilingMoveFloorWithPassHeight()
    {
        var sector3 = GameActions.GetSectorByTag(World, 3);
        var entity3 = GameActions.GetEntity(World, 2);
        GameActions.ActivateLine(World, World.Player, 19, ActivationContext.UseLine);
        GameActions.RunSectorPlaneSpecial(World, sector3);

        sector3.Floor.Z.Should().Be(-32);
        entity3.Position.Z.Should().Be(80);
    }
}
