using FluentAssertions;
using Helion.Models;
using Helion.Resources.IWad;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using System;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class MonsterCloset
{
    private SinglePlayerWorld World;
    private Player Player => World.Player;

    public MonsterCloset()
    {
        World = LoadMap(null, disposeExistingWorld: true, sameAsPreviousMap: false);
    }

    [Fact(DisplayName = "Monster closets")]
    public void MonsterClosets()
    {
        World.Config.Game.MonsterCloset.Value.Should().BeTrue();
        var imp1 = GameActions.GetEntity(World, 1);
        var imp2 = GameActions.GetEntity(World, 3);

        imp1.ClosetFlags.Should().Be(ClosetFlags.MonsterCloset);
        imp2.ClosetFlags.Should().Be(ClosetFlags.None);

        GameActions.TickWorld(World, 35);
        imp1.ClosetFlags.Should().Be(ClosetFlags.MonsterCloset | ClosetFlags.ClosetLook);
        imp2.ClosetFlags.Should().Be(ClosetFlags.None);
        imp1.FrameState.Frame.ActionFunction!.Method.Name.Should().Be("A_ClosetLook");
        imp2.FrameState.Frame.ActionFunction!.Method.Name.Should().Be("A_Look");

        imp1.FrameState.Frame.Sprite.Should().Be("TNT1");
        imp2.FrameState.Frame.Sprite.Should().NotBe("TNT1");

        GameActions.PlayerFirePistol(World, Player).Should().BeTrue();
        imp1.ClosetFlags.Should().Be(ClosetFlags.MonsterCloset | ClosetFlags.ClosetChase);
        imp2.ClosetFlags.Should().Be(ClosetFlags.None);
        imp1.FrameState.Frame.ActionFunction!.Method.Name.Should().Be("A_ClosetChase");
        imp2.FrameState.Frame.ActionFunction!.Method.Name.Should().Be("A_Chase");

        GameActions.TickWorld(World, () => imp1.Position.X != -256 && imp1.Position.Y != -64, () => { });
        imp1.ClosetFlags.Should().Be(ClosetFlags.None);
        imp1.FrameState.Frame.Sprite.Should().NotBe("TNT1");
        imp1.FrameState.Frame.ActionFunction!.Method.Name.Should().Be("A_Chase");
        // Should not play sight sound
        GameActions.AssertNoSound(World, imp1);
    }

    [Fact(DisplayName = "Monster closet serialize")]
    public void MonsterClosetSerialize()
    {
        var imp1 = GameActions.GetEntity(World, 1);
        var imp2 = GameActions.GetEntity(World, 3);

        imp1.ClosetFlags.Should().Be(ClosetFlags.MonsterCloset);
        imp2.ClosetFlags.Should().Be(ClosetFlags.None);

        var model = World.ToWorldModel();
        World = LoadMap(model, disposeExistingWorld: true, sameAsPreviousMap: true);

        imp1 = GameActions.GetEntity(World, 1);
        imp2 = GameActions.GetEntity(World, 3);

        imp1.ClosetFlags.Should().Be(ClosetFlags.MonsterCloset);
        imp2.ClosetFlags.Should().Be(ClosetFlags.None);

        GameActions.SetEntityPosition(World, imp1, (-256, -256));
        imp1.ClearMonsterCloset();
        imp1.ClosetFlags.Should().Be(ClosetFlags.None);

        model = World.ToWorldModel();
        World = LoadMap(model, disposeExistingWorld: true, sameAsPreviousMap: true);

        imp1 = GameActions.GetEntity(World, 1);
        imp2 = GameActions.GetEntity(World, 3);

        imp1.ClosetFlags.Should().Be(ClosetFlags.None);
        imp2.ClosetFlags.Should().Be(ClosetFlags.None);
    }

    [Fact(DisplayName = "Monster reload same map")]
    public void MonsterClosetReloadSameMap()
    {
        var imp1 = GameActions.GetEntity(World, 1);
        var imp2 = GameActions.GetEntity(World, 3);

        imp1.ClosetFlags.Should().Be(ClosetFlags.MonsterCloset);
        imp2.ClosetFlags.Should().Be(ClosetFlags.None);

        GameActions.SetEntityPosition(World, imp1, (-256, -256));
        imp1.ClearMonsterCloset();
        imp1.ClosetFlags.Should().Be(ClosetFlags.None);

        World = LoadMap(null, disposeExistingWorld: true, sameAsPreviousMap: false);

        imp1 = GameActions.GetEntity(World, 1);
        imp2 = GameActions.GetEntity(World, 3);

        imp1.ClosetFlags.Should().Be(ClosetFlags.MonsterCloset);
        imp2.ClosetFlags.Should().Be(ClosetFlags.None);
    }

    private SinglePlayerWorld LoadMap(WorldModel? worldModel, bool disposeExistingWorld, bool sameAsPreviousMap)
    {
        // Need to dispose the first world's archive collection because it locks the file
        if (worldModel != null && !World.ArchiveCollection.IsDisposed)
            World.ArchiveCollection.Dispose();

        return WorldAllocator.LoadMap("Resources/monstercloset.zip", "monstercloset.WAD", "MAP01", Guid.NewGuid().ToString(), (world) => { }, IWadType.Doom2,
            worldModel: worldModel, disposeExistingWorld: disposeExistingWorld, sameAsPreviousMap: sameAsPreviousMap);
    }
}
