using FluentAssertions;
using Helion.Resources.IWad;
using Helion.Util.RandomGenerators;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class Ambush
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public Ambush()
    {
        World = WorldAllocator.LoadMap("Resources/box.zip", "box.WAD", "MAP01", GetType().Name, (world) => { world.SetRandom(new NoRandom()); }, IWadType.Doom2);
    }

    [Fact(DisplayName = "Ambush flag clears on A_FaceTarget")]
    public void AmbushClears()
    {
        var imp = GameActions.CreateEntity(World, "DoomImp", (-320, -64, 0), frozen: false);
        imp.Flags.SetAmbush();
        imp.AngleRadians = GameActions.GetAngle(Bearing.South);

        imp.Flags.Ambush().Should().BeTrue();

        GameActions.SetEntityPosition(World, Player, (-320, -320, 0));
        GameActions.TickWorld(World, () => { return Player.Health == 100; }, () => { });

        imp.Flags.Ambush().Should().BeFalse();
    }
}
