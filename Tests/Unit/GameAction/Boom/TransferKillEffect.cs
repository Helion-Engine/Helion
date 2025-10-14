using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Boom;

[Collection("GameActions")]
public class TransferKillEffect
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public TransferKillEffect()
    {
        World = WorldAllocator.LoadMap("Resources/transferkilleffect.zip", "transferkilleffect.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact(DisplayName = "Transfer kill effect kills monsters")]
    public void TransferKillEffectMonster()
    {
        var monster = GameActions.GetEntity(World, 1);
        monster.IsDead.Should().BeFalse();

        GameActions.ActivateLine(World, Player, 13, ActivationContext.CrossLine).Should().BeTrue();
        GameActions.TickWorld(World, 10);
        monster.IsDead.Should().BeTrue();
    }
}
