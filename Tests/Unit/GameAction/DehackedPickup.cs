using Helion.Dehacked;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.Tests.Unit.GameAction;
using Helion.Tests.Unit.GameAction.Util;
using Helion.Util;
using Helion.World.Entities.Definition.Composer;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using System;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class DehackedPickup : IDisposable
{
    const string Chaingun = "Chaingun";

    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public DehackedPickup()
    {
        World = WorldAllocator.LoadMap("Resources/box.zip", "box.wad", "MAP01", GetType().Name, WorldInit, IWadType.Doom2);
    }

    private void WorldInit(SinglePlayerWorld world)
    {
        world.ArchiveCollection.Definitions.DehackedDefinition = new();
        DehackedApplier applier = new(world.ArchiveCollection.Definitions, world.ArchiveCollection.Definitions.DehackedDefinition);
        applier.Apply(world.ArchiveCollection.Definitions.DehackedDefinition, world.ArchiveCollection.Definitions, world.EntityManager.DefinitionComposer);

        // Modify shotgun sprite to blue key.
        // This should cause the player to pickup a blue key instead of the shotgun
        var def = GameActions.GetEntityDefinition(world, Chaingun);
        var frameIndex = DefinitionStateApplier.GetEntityFrame(world.ArchiveCollection.EntityFrameTable, def, Constants.FrameStates.Spawn)!.Value;
        var frame = world.ArchiveCollection.EntityFrameTable.Frames[frameIndex];
        frame.Sprite = "BKEY";
    }

    [Fact(DisplayName = "Item modified with blue key frame pickups up blue key instead")]
    public void PickupItemWithModifiedSprite()
    {
        InventoryUtil.AssertInventoryDoesNotContain(Player, "BlueCard");
        var shotgun = GameActions.CreateEntity(World, Chaingun, Vec3D.Zero);
        World.PerformItemPickup(Player, shotgun);
        InventoryUtil.AssertInventoryContains(Player, "BlueCard");
    }

    [Fact(DisplayName = "Modified dehacked pickup carries over dropped")]
    public void ModifiedDehackedPickupCarriesOverDropped()
    {
        InventoryUtil.AssertAmount(Player, "Shell", 0);
        var shotgun = GameActions.CreateEntity(World, "Shotgun", Vec3D.Zero);
        World.PerformItemPickup(Player, shotgun);
        InventoryUtil.AssertAmount(Player, "Shell", 8);

        shotgun = GameActions.CreateEntity(World, "Shotgun", Vec3D.Zero);
        // Dropped shotguns add 4 instead of 8
        shotgun.Flags.Dropped = true;
        World.PerformItemPickup(Player, shotgun);
        InventoryUtil.AssertAmount(Player, "Shell", 12);
    }

    public void Dispose()
    {
        GameActions.DestroyCreatedEntities(World);
        GC.SuppressFinalize(this);
    }
}