using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class MonsterTarget
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public MonsterTarget()
    {
        World = WorldAllocator.LoadMap("Resources/box.zip", "box.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2, cacheWorld: false);
        World.Sectors[0].SoundTarget = new WeakEntity(null);
    }

    [Fact(DisplayName = "Monster kills monster and goes to sleep with no sound target")]
    public void AttackingMonsterSleepsWithNoSoundTarget()
    {
        var sector = World.Sectors[0];
        sector.SoundTarget.IsNull().Should().BeTrue();
        var imp = GameActions.CreateEntity(World, "DoomImp", Vec3D.Zero, frozen: false);
        var zombie = GameActions.CreateEntity(World, "ZombieMan", Vec3D.Zero, frozen: false);
        imp.SetTarget(zombie);

        GameActions.TickWorld(World, 1);
        zombie.Kill(null);

        imp.Target().Should().Be(zombie);

        GameActions.TickWorld(World, 8);
        imp.Target().Should().Be(zombie);
        imp.FrameState.Frame.ActionFunction!.Method.Name.Should().Be("A_Look");
    }

    [Fact(DisplayName = "Monster kills monster and goes to sleep and targets player on fire")]
    public void AttackingMonsterSleepsAndTargetsPlayer()
    {
        var sector = World.Sectors[0];
        sector.SoundTarget.IsNull().Should().BeTrue();
        var imp = GameActions.CreateEntity(World, "DoomImp", Vec3D.Zero, frozen: false);
        var zombie = GameActions.CreateEntity(World, "ZombieMan", Vec3D.Zero, frozen: false);
        imp.SetTarget(zombie);

        GameActions.TickWorld(World, 1);
        zombie.Kill(null);

        imp.Target().Should().Be(zombie);

        GameActions.TickWorld(World, 35);
        imp.Target().Should().Be(zombie);
        imp.FrameState.Frame.ActionFunction!.Method.Name.Should().Be("A_Look");

        sector.SoundTarget = new WeakEntity(Player);
        GameActions.TickWorld(World, 8);
        imp.Target().Should().Be(Player);
    }

    [Fact(DisplayName = "Friendly Monster kills monster and targets LOS player")]
    public void AttackingFriendlyMonsterSleepsAndTargetsLOSPlayer()
    {
        var sector = World.Sectors[0];
        sector.SoundTarget.IsNull().Should().BeTrue();
        var imp = GameActions.CreateEntity(World, "DoomImp", Vec3D.Zero, frozen: false);
        var zombie = GameActions.CreateEntity(World, "ZombieMan", Vec3D.Zero, frozen: false);
        imp.Flags.SetFriendly();
        imp.SetTarget(zombie);

        GameActions.TickWorld(World, 1);
        zombie.Kill(null);

        imp.Target().Should().Be(zombie);

        GameActions.SetEntityPosition(World, Player, (0, 0));
        World.CheckLineOfSight(imp, Player).Should().BeTrue();

        GameActions.TickWorld(World, 35);
        imp.Target().Should().Be(Player);
    }

    [Fact(DisplayName = "Friendly Monster kills monster and targets first player")]
    public void AttackingFriendlyMonsterSleepsAndTargetsFirstPlayer()
    {
        var sector = World.Sectors[0];
        sector.SoundTarget.IsNull().Should().BeTrue();
        var imp = GameActions.CreateEntity(World, "DoomImp", Vec3D.Zero, frozen: false);
        var zombie = GameActions.CreateEntity(World, "ZombieMan", Vec3D.Zero, frozen: false);
        imp.Flags.SetFriendly();
        imp.SetTarget(zombie);

        GameActions.TickWorld(World, 1);
        zombie.Kill(null);

        imp.Target().Should().Be(zombie);

        GameActions.TickWorld(World, 35);
        imp.Target().Should().Be(Player);
    }
}
