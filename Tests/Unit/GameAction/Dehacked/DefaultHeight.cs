using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Dehacked;

[Collection("GameActions")]
public class DefaultHeight
{
    private readonly SinglePlayerWorld World;

    public DefaultHeight()
    {
        World = WorldAllocator.LoadMap("Resources/box.zip", "box.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2,
            dehackedPatch: Dehacked);
    }

    [Fact(DisplayName = "New Dehacked thing defaults height to 0")]
    public void NewDehackedThingDefaultHeight()
    {
        var zombieDef = World.EntityManager.DefinitionComposer.GetByName("ZombieMan");
        var newDef1 = World.EntityManager.DefinitionComposer.GetByName("*deh/entity190");
        var newDef2 = World.EntityManager.DefinitionComposer.GetByName("*deh/entity191");

        zombieDef.Should().NotBeNull();
        newDef1.Should().NotBeNull();
        newDef2.Should().NotBeNull();

        zombieDef.Properties.Height.Should().Be(56);
        newDef1.Properties.Height.Should().Be(0);
        newDef2.Properties.Height.Should().Be(69);
    }

    private static readonly string Dehacked =
@"
Thing 2 (ZombieMan)
Health = 1000
Initial frame = 42069

Thing 191 (NewThing)
ID # = 191
Initial frame = 42069

Thing 192 (NewThing with height)
ID # = 192
Height = 4521984
Initial frame = 42069"";";
}
