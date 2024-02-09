using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class MonsterCloset
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public MonsterCloset()
    {
        World = WorldAllocator.LoadMap("Resources/monstercloset.zip", "monstercloset.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
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
        imp1.Frame.ActionFunction!.Method.Name.Should().Be("A_ClosetLook");
        imp2.Frame.ActionFunction!.Method.Name.Should().Be("A_Look");

        imp1.Frame.Sprite.Should().Be("TNT1");
        imp2.Frame.Sprite.Should().NotBe("TNT1");

        GameActions.PlayerFirePistol(World, Player).Should().BeTrue();
        imp1.ClosetFlags.Should().Be(ClosetFlags.MonsterCloset | ClosetFlags.ClosetChase);
        imp2.ClosetFlags.Should().Be(ClosetFlags.None);
        imp1.Frame.ActionFunction!.Method.Name.Should().Be("A_ClosetChase");
        imp2.Frame.ActionFunction!.Method.Name.Should().Be("A_Chase");

        GameActions.TickWorld(World, () => imp1.Position.X != -256 && imp1.Position.Y != -64, () => { });
        imp1.ClosetFlags.Should().Be(ClosetFlags.None);
        imp1.Frame.Sprite.Should().NotBe("TNT1");
        imp1.Frame.ActionFunction!.Method.Name.Should().Be("A_Chase");
    }
}