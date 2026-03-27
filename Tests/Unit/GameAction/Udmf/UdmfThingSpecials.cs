using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Maps.Specials.ZDoom;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using System;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Udmf;

[Collection("GameActions")]
public class UdmfThingSpecials : IDisposable
{
    private static readonly string ResourceZip = "Resources/udmfthingspecials.zip";
    private static readonly string MapName = "MAP01";

    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public UdmfThingSpecials()
    {
        World = WorldAllocator.LoadMap(ResourceZip, "udmfthingspecials.wad", MapName, GetType().Name, (world) => { }, IWadType.Doom2);
    }
    public void Dispose()
    {
        GameActions.DestroyCreatedEntities(World);
    }

    [Fact(DisplayName = "Thing spawn")]
    public void ThingSpawn()
    {
        GameActions.ActivateLine(World, Player, 66, ActivationContext.UseLine).Should().BeTrue();
        var monster = GameActions.GetEntity(World, "ShotgunGuy");
        monster.ThingId.Should().Be(70);
        monster.AngleRadians.Should().BeApproximately(1.57, 2);
        monster.Position.Should().Be(GameActions.GetEntityByTid(World, 69).Position);

        var fog = GameActions.GetEntities(World, "TeleportFog");
        fog.Count.Should().Be(1);

        // Blocking spawn rules apply
        GameActions.ActivateLine(World, Player, 66, ActivationContext.UseLine).Should().BeFalse();
        var monsters = GameActions.GetEntities(World, "ShotgunGuy");
        monsters.Count.Should().Be(1);

        World.EntityManager.Destroy(monster);
        World.EntityManager.Destroy(fog[0]);
    }

    [Fact(DisplayName = "Thing spawn blocked")]
    public void ThingSpawnBlocked()
    {
        var spot = GameActions.GetEntityByTid(World, 69);
        GameActions.CreateEntity(World, "DoomImp", spot.Position);
        GameActions.ActivateLine(World, Player, 66, ActivationContext.UseLine).Should().BeFalse();
        var monsters = GameActions.GetEntities(World, "ShotgunGuy");
        monsters.Count.Should().Be(0);
    }

    [Fact(DisplayName = "Thing spawn facing")]
    public void ThingSpawnFacing()
    {
        GameActions.ActivateLine(World, Player, 70, ActivationContext.UseLine).Should().BeTrue();
        var monster = GameActions.GetEntity(World, "DoomImp");
        monster.ThingId.Should().Be(170);
        monster.AngleRadians.Should().BeApproximately(4.71, 2);
        monster.Position.Should().Be(GameActions.GetEntityByTid(World, 169).Position);

        var fog = GameActions.GetEntities(World, "TeleportFog");
        fog.Count.Should().Be(1);

        // Blocking spawn rules apply
        GameActions.ActivateLine(World, Player, 70, ActivationContext.UseLine).Should().BeFalse();
        var monsters = GameActions.GetEntities(World, "DoomImp");
        monsters.Count.Should().Be(1);

        World.EntityManager.Destroy(monster);
        World.EntityManager.Destroy(fog[0]);
    }

    [Fact(DisplayName = "Thing spawn facing no fog")]
    public void ThingSpawnFacingNoFog()
    {
        GameActions.ActivateLine(World, Player, 74, ActivationContext.UseLine).Should().BeTrue();
        var monster = GameActions.GetEntity(World, "DoomImp");
        monster.ThingId.Should().Be(170);
        monster.AngleRadians.Should().BeApproximately(4.71, 2);

        var fog = GameActions.GetEntities(World, "TeleportFog");
        fog.Count.Should().Be(0);

        // Blocking spawn rules apply
        GameActions.ActivateLine(World, Player, 74, ActivationContext.UseLine).Should().BeFalse();
        var monsters = GameActions.GetEntities(World, "DoomImp");
        monsters.Count.Should().Be(1);

        World.EntityManager.Destroy(monster);
    }

    [Fact(DisplayName = "Thing projectile")]
    public void ThingProjectile()
    {
        GameActions.ActivateLine(World, Player, 78, ActivationContext.UseLine).Should().BeTrue();
        var ball = GameActions.GetEntity(World, "DoomImpBall");
        ball.AngleRadians.Should().BeApproximately(1.57, 2);
        ball.Velocity.X.Should().BeApproximately(0, 2);
        ball.Velocity.Y.Should().Be(0.125);
        ball.Velocity.Z.Should().Be(0.25);
        ball.Flags.NoGravity().Should().BeTrue();
        World.EntityManager.Destroy(ball);
    }

    [Fact(DisplayName = "Thing projectile gravity")]
    public void ThingProjectileGravity()
    {
        GameActions.ActivateLine(World, Player, 82, ActivationContext.UseLine).Should().BeTrue();
        var ball = GameActions.GetEntity(World, "DoomImpBall");
        ball.AngleRadians.Should().BeApproximately(1.57, 2);
        ball.Velocity.X.Should().BeApproximately(0, 2);
        ball.Velocity.Y.Should().Be(0.125);
        ball.Velocity.Z.Should().Be(0.25);
        ball.Flags.NoGravity().Should().BeFalse();
        World.EntityManager.Destroy(ball);
    }

    [Fact(DisplayName = "Thing projectile aimed")]
    public void ThingProjectileAimed()
    {
        GameActions.CreateEntity(World, "DoomImp", (416, 336, 96), tid: 200);
        GameActions.ActivateLine(World, Player, 86, ActivationContext.UseLine).Should().BeTrue();
        var ball = GameActions.GetEntity(World, "DoomImpBall");
        ball.ThingId.Should().Be(201);
        ball.AngleRadians.Should().BeApproximately(2.35, 2);
        ball.Velocity.X.Should().BeApproximately(-0.52, 2);
        ball.Velocity.Y.Should().BeApproximately(0.52, 2);
        ball.Velocity.Z.Should().BeApproximately(0.66, 2);
        ball.Flags.NoGravity().Should().BeTrue();
        World.EntityManager.Destroy(ball);
    }

    [Fact(DisplayName = "Thing destroy")]
    public void ThingDestroy()
    {
        var imp = GameActions.CreateEntity(World, "DoomImp", (544, 144, 0), tid: 200);
        GameActions.ActivateLine(World, Player, 90, ActivationContext.UseLine).Should().BeTrue();
        imp.IsDead().Should().BeTrue();
        imp.Health.Should().Be(0);
    }

    [Fact(DisplayName = "Thing destroy gib")]
    public void ThingDestroyGib()
    {
        var imp = GameActions.CreateEntity(World, "DoomImp", (544, 144, 0), tid: 200);
        GameActions.ActivateLine(World, Player, 98, ActivationContext.UseLine).Should().BeTrue();
        imp.IsDead().Should().BeTrue();
        imp.Health.Should().Be(-10000);
    }

    [Fact(DisplayName = "Thing destroy gib")]
    public void ThingDestroyAll()
    {
        var imp1 = GameActions.CreateEntity(World, "DoomImp", (544, 144, 0));
        var imp2 = GameActions.CreateEntity(World, "DoomImp", (544, 144, 0));
        GameActions.ActivateLine(World, Player, 106, ActivationContext.UseLine).Should().BeTrue();
        imp1.IsDead().Should().BeTrue();
        imp2.IsDead().Should().BeTrue();
    }

    [Fact(DisplayName = "Thing remove")]
    public void ThingRemove()
    {
        var imp = GameActions.CreateEntity(World, "DoomImp", (544, 144, 0), tid: 200);
        GameActions.ActivateLine(World, Player, 102, ActivationContext.UseLine).Should().BeTrue();
        imp.IsDisposed.Should().BeTrue();
    }

    [Fact(DisplayName = "Noise Alert")]
    public void NoiseAlert()
    {
        var imp = GameActions.CreateEntity(World, "DoomImp", (-32, 448, 0), frozen: false);
        imp.Target().Should().BeNull();
        GameActions.ActivateLine(World, Player, 4, ActivationContext.UseLine).Should().BeTrue();
        GameActions.TickWorld(World, () => { return imp.Target() == null; }, () => { });
        imp.Target().Should().Be(Player);
    }

    [Fact(DisplayName = "Thing Deactivate by tid")]
    public void ThingDeactivate()
    {
        var imp1 = GameActions.CreateEntity(World, "DoomImp", (-32, 448, 0), frozen: false, tid: 1);
        imp1.Flags.Dormant().Should().BeFalse();
        GameActions.ActivateLine(World, imp1, 16, ActivationContext.CrossLine).Should().BeTrue();
        imp1.Flags.Dormant().Should().BeTrue();
        // Tick function is not run
        imp1.FrozenTics = 1;
        World.Tick();
        imp1.FrozenTics.Should().Be(1);
    }

    [Fact(DisplayName = "Thing Deactivate by tid")]
    public void ThingDeactivateByTid()
    {
        var imp1 = GameActions.CreateEntity(World, "DoomImp", (-32, 448, 0), frozen: false, tid: 1);
        var imp2 = GameActions.CreateEntity(World, "DoomImp", (-32, 448, 0), frozen: false, tid: 1);
        imp1.Flags.Dormant().Should().BeFalse();
        imp2.Flags.Dormant().Should().BeFalse();
        GameActions.ActivateLine(World, Player, 8, ActivationContext.UseLine).Should().BeTrue();
        imp1.Flags.Dormant().Should().BeTrue();
        imp2.Flags.Dormant().Should().BeTrue();
        // Tick function is not run
        imp1.FrozenTics = 1;
        imp2.FrozenTics = 1;
        World.Tick();
        imp1.FrozenTics.Should().Be(1);
        imp2.FrozenTics.Should().Be(1);
    }

    [Fact(DisplayName = "Thing Activate")]
    public void ThingActivate()
    {
        var imp1 = GameActions.CreateEntity(World, "DoomImp", (-32, 448, 0), frozen: false, tid: 1);
        imp1.Flags.SetDormant();
        imp1.Flags.Dormant().Should().BeTrue();
        GameActions.ActivateLine(World, imp1, 17, ActivationContext.CrossLine).Should().BeTrue();
        imp1.Flags.Dormant().Should().BeFalse();
        // Tick function is run
        imp1.FrozenTics = 1;
        World.Tick();
        imp1.FrozenTics.Should().Be(0);
    }

    [Fact(DisplayName = "Thing Activate by tid")]
    public void ThingActivateByTid()
    {
        var imp1 = GameActions.CreateEntity(World, "DoomImp", (-32, 448, 0), frozen: false, tid: 1);
        var imp2 = GameActions.CreateEntity(World, "DoomImp", (-32, 448, 0), frozen: false, tid: 1);
        imp1.Flags.SetDormant();
        imp2.Flags.SetDormant();
        imp1.Flags.Dormant().Should().BeTrue();
        imp2.Flags.Dormant().Should().BeTrue();
        GameActions.ActivateLine(World, Player, 12, ActivationContext.UseLine).Should().BeTrue();
        imp1.Flags.Dormant().Should().BeFalse();
        // Tick function is run
        imp1.FrozenTics = 1;
        imp2.FrozenTics = 1;
        World.Tick();
        imp1.FrozenTics.Should().Be(0);
        imp2.FrozenTics.Should().Be(0);
    }

    [Fact(DisplayName = "Heal thing 50 with default max (Arg1 == 0)")]
    public void HealThing50DefaultMax()
    {
        Player.Health = 50;
        GameActions.ActivateLine(World, Player, 22, ActivationContext.UseLine).Should().BeTrue();
        Player.Health.Should().Be(100);
        Player.Health = 60;
        GameActions.ActivateLine(World, Player, 22, ActivationContext.UseLine).Should().BeTrue();
        Player.Health.Should().Be(100);
    }

    [Fact(DisplayName = "Heal thing 50 with 200 max (Arg1 == 200)")]
    public void HealThing50With200Max()
    {
        GameActions.ActivateLine(World, Player, 26, ActivationContext.UseLine).Should().BeTrue();
        Player.Health.Should().Be(150);
        GameActions.ActivateLine(World, Player, 26, ActivationContext.UseLine).Should().BeTrue();
        Player.Health.Should().Be(200);
        GameActions.ActivateLine(World, Player, 26, ActivationContext.UseLine).Should().BeTrue();
        Player.Health.Should().Be(200);
        Player.Health = 100;
    }

    [Fact(DisplayName = "Heal thing 50 with max soulsphere (Arg1 == 1)")]
    public void HealThing50WithMaxSoulSphere()
    {
        GameActions.ActivateLine(World, Player, 30, ActivationContext.UseLine).Should().BeTrue();
        Player.Health.Should().Be(150);
        GameActions.ActivateLine(World, Player, 30, ActivationContext.UseLine).Should().BeTrue();
        Player.Health.Should().Be(200);
        GameActions.ActivateLine(World, Player, 30, ActivationContext.UseLine).Should().BeTrue();
        Player.Health.Should().Be(200);
        Player.Health = 100;
    }

    [Fact(DisplayName = "Thing hate monster hates player")]
    public void ThingHatePlayer()
    {
        var imp = GameActions.CreateEntity(World, "DoomImp", (-32, 448, 0), frozen: false, tid: 1);
        imp.Target().Should().BeNull();

        GameActions.ActivateLine(World, Player, 38, ActivationContext.UseLine).Should().BeTrue();
        imp.Target().Should().Be(Player);
    }

    [Fact(DisplayName = "Thing hate monster hates other monster")]
    public void ThingHateOtherMonster()
    {
        var imp = GameActions.CreateEntity(World, "DoomImp", (-32, 448, 0), frozen: false, tid: 1);
        var zombieMan = GameActions.CreateEntity(World, "ZombieMan", (-32, 448, 0), frozen: false, tid: 2);
        var shotgunGuy = GameActions.CreateEntity(World, "ShotgunGuy", (-32, 448, 0), frozen: false, tid: 2);

        imp.Target().Should().BeNull();
        zombieMan.Target().Should().BeNull();
        shotgunGuy.Target().Should().BeNull();

        GameActions.ActivateLine(World, Player, 34, ActivationContext.UseLine).Should().BeTrue();
        imp.Target().Should().Be(shotgunGuy);
    }

    [Fact(DisplayName = "Thing raise")]
    public void ThingRaise()
    {
        var imp = GameActions.CreateEntity(World, "DoomImp", (-32, 448, 0), frozen: false, tid: 1);
        var zombieMan = GameActions.CreateEntity(World, "ZombieMan", (-32, 448, 0), frozen: false, tid: 1);

        imp.Kill(null);
        zombieMan.Kill(null);
        GameActions.TickWorld(World, 35);
        imp.IsDead().Should().BeTrue();
        zombieMan.IsDead().Should().BeTrue();

        GameActions.ActivateLine(World, Player, 42, ActivationContext.UseLine).Should().BeTrue();
        imp.IsDead().Should().BeFalse();
        zombieMan.IsDead().Should().BeFalse();
        imp.Height.Should().Be(imp.Properties.Height);
        zombieMan.Height.Should().Be(imp.Properties.Height);
    }

    [Fact(DisplayName = "Thing raise")]
    public void ThingStop()
    {
        var imp = GameActions.CreateEntity(World, "DoomImp", (-32, 448, 0), frozen: false, tid: 1);
        var zombieMan = GameActions.CreateEntity(World, "ZombieMan", (-32, 448, 0), frozen: false, tid: 1);

        imp.Velocity = new(1, 1, 1);
        zombieMan.Velocity = new(1, 1, 1);

        GameActions.ActivateLine(World, Player, 46, ActivationContext.UseLine).Should().BeTrue();
        imp.Velocity.Should().Be(Vec3D.Zero);
        zombieMan.Velocity.Should().Be(Vec3D.Zero);
    }

    [Fact(DisplayName = "Damage thing tid activator")]
    public void DamageThingTidActivator()
    {
        Player.Health.Should().Be(100);
        GameActions.ActivateLine(World, Player, 50, ActivationContext.UseLine).Should().BeTrue();
        Player.Health.Should().Be(95);
        Player.Health = 100;
    }

    [Fact(DisplayName = "Damage thing tid")]
    public void DamageThingTid()
    {
        var imp = GameActions.CreateEntity(World, "DoomImp", (-32, 448, 0), frozen: false, tid: 1);
        imp.Health.Should().Be(60);
        imp.Target().Should().BeNull();
        GameActions.ActivateLine(World, Player, 54, ActivationContext.UseLine).Should().BeTrue();
        // Setting target is somewhat random depending on the frame state == spawn state index or hitting pain state
        imp.Target().Should().Be(Player);
        imp.Health.Should().Be(55);
    }

    [Fact(DisplayName = "Thing move fog")]
    public void ThingMoveFog()
    {
        var imp = GameActions.CreateEntity(World, "DoomImp", (-32, 448, 0), frozen: false, tid: 1);
        var spot = GameActions.GetEntityByTid(World, 1);
        GameActions.ActivateLine(World, Player, 58, ActivationContext.UseLine).Should().BeTrue();
        imp.Position.Should().Be(spot.Position);

        var fog = GameActions.GetEntities(World, "TeleportFog");
        fog.Count.Should().Be(2);

        World.EntityManager.Destroy(fog[0]);
        World.EntityManager.Destroy(fog[1]);
    }

    [Fact(DisplayName = "Thing move no fog")]
    public void ThingMoveNoFog()
    {
        var imp = GameActions.CreateEntity(World, "DoomImp", (-32, 448, 0), frozen: false, tid: 1);
        var spot = GameActions.GetEntityByTid(World, 1);
        GameActions.ActivateLine(World, Player, 62, ActivationContext.UseLine).Should().BeTrue();
        imp.Position.Should().Be(spot.Position);

        var fog = GameActions.GetEntities(World, "TeleportFog");
        fog.Count.Should().Be(0);

        GameActions.TickWorld(World, 35);
    }

    [Fact(DisplayName = "Thrust thing z up")]
    public void ThrustThingZUp()
    {
        Player.Velocity = Vec3D.Zero;
        GameActions.ActivateLine(World, Player, 110, ActivationContext.UseLine).Should().BeTrue();
        Player.Velocity.Z.Should().Be(2);
    }

    [Fact(DisplayName = "Thrust thing z down")]
    public void ThrustThingZDown()
    {
        Player.Velocity = Vec3D.Zero;
        GameActions.ActivateLine(World, Player, 114, ActivationContext.UseLine).Should().BeTrue();
        Player.Velocity.Z.Should().Be(-2);
    }

    [Fact(DisplayName = "Thing change tid")]
    public void ThingChangeTid()
    {
        Player.ThingId.Should().Be(0);
        GameActions.ActivateLine(World, Player, 118, ActivationContext.UseLine).Should().BeTrue();
        Player.ThingId.Should().Be(420);
        Player.ThingId = 0;
    }

    [Fact(DisplayName = "Thing set special")]
    public void ThingSetSpecial()
    {
        var imp = GameActions.CreateEntity(World, "DoomImp", (-32, 448, 0), frozen: false, tid: 420);
        imp.Special.Should().Be(ZDoomLineSpecialType.None);
        GameActions.ActivateLine(World, Player, 126, ActivationContext.UseLine).Should().BeTrue();
        imp.Special.Should().Be(ZDoomLineSpecialType.ThrustThingZ);
        imp.Args.Arg0.Should().Be(0);
        imp.Args.Arg1.Should().Be(100);
        imp.Args.Arg2.Should().Be(1);
    }

    [Fact(DisplayName = "Entity death special targets killer")]
    public void EntityDeathSpecialActivator()
    {
        var imp = GameActions.CreateEntity(World, "DoomImp", (-32, 448, 0), frozen: false, tid: 420);
        var baron = GameActions.CreateEntity(World, "BaronOfHell", (-32, 320, 0), frozen: false);
        baron.Velocity.Z.Should().Be(0);
        imp.Special.Should().Be(ZDoomLineSpecialType.None);
        GameActions.ActivateLine(World, Player, 126, ActivationContext.UseLine).Should().BeTrue();
        imp.Special.Should().Be(ZDoomLineSpecialType.ThrustThingZ);
        imp.Kill(baron);
        baron.Velocity.Z.Should().Be(-25);
    }
}
