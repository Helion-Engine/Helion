using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Boom;

[Collection("GameActions")]
public class MonsterDoor
{
    private readonly SinglePlayerWorld World;

    public MonsterDoor()
    {
        World = WorldAllocator.LoadMap("Resources/monsterdoor.zip", "monsterdoor.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact(DisplayName = "Monster opens normal door line")]
    public void MonsterOpensNormalDoor()
    {
        var door = GameActions.GetSector(World, 2);
        var imp = GameActions.CreateEntity(World, "DoomImp", (-832, 16, 0), frozen: false);
        imp.AngleRadians = GameActions.GetAngle(Bearing.South);
        imp.SetMoveDirection(Helion.World.Entities.Entity.MoveDir.South);
        imp.SetTarget(World.Player);
        imp.TryWalk();
        door.ActiveCeilingMove.Should().NotBeNull();
    }

    [Fact(DisplayName = "Monster opens two-sided switch door line ")]
    public void MonsterOpensTwoSidedDoorSwitchLine()
    {
        // If a monster is blocked by an impassible line and they cross a switch door line then they will attempt to activate it.
        var door = GameActions.GetSectorByTag(World, 2);
        var imp = GameActions.CreateEntity(World, "DoomImp", (-576, 96, 0), frozen: false);
        imp.AngleRadians = GameActions.GetAngle(Bearing.South);
        imp.SetMoveDirection(Helion.World.Entities.Entity.MoveDir.South);
        imp.SetTarget(World.Player);
        imp.TryWalk();
        door.ActiveCeilingMove.Should().NotBeNull();
    }
}