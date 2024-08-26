using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World;
using Helion.World.Entities.Definition.States;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Xunit;
using static Helion.Dehacked.DehackedDefinition;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class BossAction
{
    private readonly SinglePlayerWorld World;

    public BossAction()
    {
        World = WorldAllocator.LoadMap("Resources/bossaction.zip", "bossaction.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact(DisplayName = "BossAction by actor name")]
    public void BossActionName()
    {
        var sector = GameActions.GetSectorByTag(World, 69);
        sector.Ceiling.Z.Should().Be(0);
        var monster = GameActions.GetEntity(World, "DoomImp");
        monster.Kill(null);
        World.BossDeath(monster);
        GameActions.TickWorld(World, 1);
        GameActions.RunSectorPlaneSpecial(World, sector);
        sector.Ceiling.Z.Should().Be(124);
    }

    [Fact(DisplayName = "BossAction by editor number")]
    public void BossActionEditorNumber()
    {
        var sector = GameActions.GetSectorByTag(World, 420);
        sector.Floor.Z.Should().Be(128);
        var monster = GameActions.GetEntity(World, "ChaingunGuy");
        monster.Kill(null);
        World.BossDeath(monster);
        GameActions.TickWorld(World, 1);
        GameActions.RunSectorPlaneSpecial(World, sector);
        sector.Floor.Z.Should().Be(0);
    }
}
