using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Sectors;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Special.SectorMovement;
using Helion.World.Special.Specials;
using System;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Boom;

[Collection("GameActions")]
public class Touchy : IDisposable
{
    const string ZombieMan = "ZombieMan";
    const string Imp = "DoomImp";
    const string PainElemental = "PainElemental";
    const string LostSoul = "LostSoul";

    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public Touchy()
    {
        World = WorldAllocator.LoadMap("Resources/box.zip", "box.wad", "MAP01", GetType().Name, WorldInit, IWadType.Doom2);
    }

    private void WorldInit(SinglePlayerWorld world)
    {
        var def = GameActions.GetEntityDefinition(world, ZombieMan);
        def.Flags.Touchy = true;
        def = GameActions.GetEntityDefinition(world, Imp);
        def.Flags.Touchy = true;
        def.Flags.Solid = false;
        def = GameActions.GetEntityDefinition(world, PainElemental);
        def.Flags.Touchy = true;
        def = GameActions.GetEntityDefinition(world, LostSoul);
        def.Flags.Touchy = true;
    }

    public void Dispose()
    {
        GameActions.SetEntityOutOfBounds(World, World.Player);
        GameActions.DestroyCreatedEntities(World);
        GC.SuppressFinalize(this);
    }

    [Fact(DisplayName = "Touchy does not die when hits thing")]
    public void TouchyHitsThing()
    {
        GameActions.SetEntityPosition(World, Player, (-320, -320, 0));
        var zombieman = GameActions.CreateEntity(World, ZombieMan, (-384, -320, 0));
        World.TryMoveXY(zombieman, (-320, -320));
        zombieman.IsDead.Should().BeFalse();
    }

    [Fact(DisplayName = "Touchy dies when hit by thing")]
    public void ThingHitsTouchy()
    {
        GameActions.SetEntityPosition(World, Player, (-320, -320, 0));
        var zombieman = GameActions.CreateEntity(World, ZombieMan, (-384, -320, 0));
        World.TryMoveXY(Player, (-384, -320));
        zombieman.IsDead.Should().BeTrue();
    }

    [Fact(DisplayName = "Touchy thing dies when hit by ceiling that moves")]
    public void TouchyThingHitsCeilingMovingCeiling()
    {
        var zombieman = GameActions.CreateEntity(World, ZombieMan, (-384, -320, 0));
        var sector = GameActions.GetSector(World, 0);
        var special = new SectorMoveSpecial(World, sector, sector.Ceiling.Z, 0, new SectorMoveData(SectorPlaneFace.Ceiling, MoveDirection.Down, MoveRepetition.None, 8, 0));
        World.MoveSectorZ(8, 0, special);
        zombieman.IsDead.Should().BeTrue();
    }

    [Fact(DisplayName = "Touchy thing dies when hit by ceiling from moving floor")]
    public void TouchyThingHitsCeilingMovingFloor()
    {
        var zombieman = GameActions.CreateEntity(World, ZombieMan, (-384, -320, 0));
        var sector = GameActions.GetSector(World, 0);
        var special = new SectorMoveSpecial(World, sector, sector.Floor.Z, 0, new SectorMoveData(SectorPlaneFace.Floor, MoveDirection.Up, MoveRepetition.None, 8, 0));
        World.MoveSectorZ(8, 128, special);
        zombieman.IsDead.Should().BeTrue();
    }

    [Fact(DisplayName = "Touchy dies when overlapped by thing non-solid")]
    public void TouchyNonSolidHit()
    {
        GameActions.SetEntityPosition(World, Player, (-320, -320, 0));
        var imp = GameActions.CreateEntity(World, Imp, (-384, -320, 0));
        World.TryMoveXY(Player, (-384, -320));
        imp.IsDead.Should().BeTrue();
    }

    [Fact(DisplayName = "Lost soul not killed by pain elemental with touchy")]
    public void LostSoulTouchy()
    {
        var pain = GameActions.CreateEntity(World, PainElemental, (-320, -320, 0));
        var soul = GameActions.CreateEntity(World, LostSoul, (-384, -320, 0));
        World.TryMoveXY(pain, (-384, -320));
        pain.IsDead.Should().BeFalse();
        soul.IsDead.Should().BeFalse();
        pain.BlockingEntity.Should().Be(soul);
    }

    [Fact(DisplayName = "Pain elemental not killed by lost soul with touchy")]
    public void PainElementalTouchy()
    {
        var pain = GameActions.CreateEntity(World, PainElemental, (-320, -320, 0));
        var soul = GameActions.CreateEntity(World, LostSoul, (-384, -320, 0));
        World.TryMoveXY(soul, (-320, -320));
        pain.IsDead.Should().BeFalse();
        soul.IsDead.Should().BeFalse();
        soul.BlockingEntity.Should().Be(pain);
    }
}