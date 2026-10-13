using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class CrushCompat
{
    private readonly SinglePlayerWorld World;

    public CrushCompat()
    {
        World = WorldAllocator.LoadMap("Resources/crushcompat.zip", "crushcompat.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact(DisplayName = "Crusher with compatibility doesn't clamp to floor when anything is blocking in x/y dimension")]
    public void CrushCompatibility()
    {
        // This tests not clamping when a crusher moves and anything blocks in x/y directions.
        // This broke resurgence MAP29 because it popped the romero head up and crushed to giblets which prevents the A_BrainDie from exiting the level.
        var crusher = GameActions.GetSectorByTag(World, 1);
        var baron = GameActions.GetEntity(World, "BaronOfHell");
        baron.Position.Z.Should().Be(-32);
        crusher.Ceiling.Z.Should().Be(24);
        World.CrusherCompat().Should().BeTrue();
        GameActions.ActivateLine(World, World.Player, 8, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, 10);
        baron.Position.Z.Should().Be(-32);
        crusher.Ceiling.Z.Should().Be(21.875);
    }
}
