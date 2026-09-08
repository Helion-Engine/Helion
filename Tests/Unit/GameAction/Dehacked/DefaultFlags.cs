using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Dehacked;

[Collection("GameActions")]
public class DefaultFlags
{
    private readonly SinglePlayerWorld World;

    public DefaultFlags()
    {
        World = WorldAllocator.LoadMap("Resources/box.zip", "box.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2,
            dehackedPatch: Dehacked);
    }

    [Fact(DisplayName = "New Dehacked thing defaults have randomize flag")]
    public void NewDehackedThingDefaultRandomize()
    {
        var newDef1 = World.EntityManager.DefinitionComposer.GetByName("*deh/entity190");
        var newDef2 = World.EntityManager.DefinitionComposer.GetByName("*deh/entity191");

        newDef1.Should().NotBeNull();
        newDef2.Should().NotBeNull();

        newDef1.Flags.RandomizeProjectile().Should().BeTrue();
        newDef2.Flags.RandomizeProjectile().Should().BeTrue();
    }

    [Fact(DisplayName = "New Dehacked thing has default properties")]
    public void NewDehackedThingDefaultProperties()
    {
        var newDef = World.EntityManager.DefinitionComposer.GetByName("*deh/entity192");
        newDef.Should().NotBeNull();
        newDef.Properties.Height.Should().Be(0);
        newDef.Properties.Radius.Should().Be(0);
        newDef.Properties.Health.Should().Be(0);
        newDef.Properties.Mass.Should().Be(0);
        newDef.Properties.MissileMovementSpeed.Should().Be(0);
        newDef.Properties.MonsterMovementSpeed.Should().Be(0);
    }

    private static readonly string Dehacked =
@"
Thing 191 (NewThing)
ID # = 191
Initial frame = 42069

Thing 192 (NewThing with height)
ID # = 192
Height = 4521984
Initial frame = 42069"";

Thing 193 (DefaultThing)
ID # = 193";
}
