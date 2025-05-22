using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Boom;


[Collection("GameActions")]
public partial class PassUse
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public PassUse()
    {
        World = WorldAllocator.LoadMap("Resources/pass_use.zip", "pass_use.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact(DisplayName = "PassUse with non-special lines in between")]
    public void PassUseFlag()
    {
        var sector1 = GameActions.GetSectorByTag(World, 1);
        var sector2 = GameActions.GetSectorByTag(World, 2);
        sector1.ActiveCeilingMove.Should().BeNull();
        sector2.ActiveCeilingMove.Should().BeNull();
        GameActions.EntityUseLine(World, Player, 16).Should().BeTrue();
        sector1.ActiveCeilingMove.Should().NotBeNull();
        sector2.ActiveCeilingMove.Should().NotBeNull();
    }
}
