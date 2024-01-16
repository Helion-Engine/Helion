using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.Tests.Unit.GameAction;
using Helion.Util.RandomGenerators;
using Helion.World;
using Helion.World.Entities;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Properties.Components;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class Shoot
{
    private SinglePlayerWorld World;
    private Player Player => World.Player;
    private readonly NoRandom Random = new();

    public Shoot()
    {
        World = WorldAllocator.LoadMap("Resources/shoot.zip", "shoot.wad", "MAP01", GetType().Name, WorldInit, IWadType.Doom2, cacheWorld: false);
    }

    private void WorldInit(SinglePlayerWorld world)
    {
        world.TextureManager.SetSkyTexture();
        world.TextureManager.IsSkyTexture(GameActions.GetSector(world, 0).Ceiling.TextureHandle).Should().BeTrue();
        world.SetRandom(Random);
    }

    private static int DamageFunc(DamageFuncParams p) => 1;

    private EntityDefinition GetMissileDef() => World.ArchiveCollection.EntityDefinitionComposer.GetByName("PlasmaBall");

    [Fact(DisplayName = "Auto aim hitscan single bullet success")]
    public void AutoAimHitScanSuccess()
    {
        var demon = GameActions.GetEntity(World, "Demon");
        int startHealth = demon.Health;
        World.FirePlayerHitscanBullets(Player, 1, 0, 0, 0, 2048, true, DamageFunc);
        demon.Health.Should().Be(startHealth - 1);
        GameActions.FindEntity(World, "Blood").Should().NotBeNull();
    }

    [Fact(DisplayName = "Auto aim hitscan multiple bullets success")]
    public void AutoAimHitScanMultipleSuccess()
    {
        var demon = GameActions.GetEntity(World, "Demon");
        int startHealth = demon.Health;
        World.FirePlayerHitscanBullets(Player, 5, 0, 0, 0, 2048, true, DamageFunc);
        demon.Health.Should().Be(startHealth - 5);
    }

    [Fact(DisplayName = "Auto aim hitscan blocked by higher floor")]
    public void AutoAimHitScanBlockedByHigherFloor()
    {
        GameActions.SetEntityPosition(World, Player, (-256, -180, 0));
        var demon = GameActions.GetEntity(World, "Demon");
        int startHealth = demon.Health;
        World.FirePlayerHitscanBullets(Player, 1, 0, 0, 0, 2048, true, DamageFunc);
        demon.Health.Should().Be(startHealth);
    }

    [Fact(DisplayName = "Auto aim hitscan blocked by lower floor")]
    public void AutoAimHitScanBlockedByLowerFloor()
    {
        var sector = GameActions.GetSector(World, 0);
        sector.Floor.Z = 224;
        var demon = GameActions.GetEntity(World, "Demon");
        int startHealth = demon.Health;
        World.FirePlayerHitscanBullets(Player, 1, 0, 0, 0, 2048, true, DamageFunc);
        demon.Health.Should().Be(startHealth);
    }

    [Fact(DisplayName = "Auto aim missile success")]
    public void AutoAimMissile()
    {
        var demon = GameActions.GetEntity(World, "Demon");
        World.FireProjectile(Player, 0, 0, 2048, true, GetMissileDef(), out var autoAimEntity);
        autoAimEntity.Should().Be(demon);
    }

    [Fact(DisplayName = "Auto aim missile blocked by higher floor")]
    public void AutoAimMissileBlockedByHigherFloor()
    {
        GameActions.SetEntityPosition(World, Player, (-256, -180, 0));
        World.FireProjectile(Player, 0, 0, 2048, true, GetMissileDef(), out var autoAimEntity);
        autoAimEntity.Should().BeNull();
    }

    [Fact(DisplayName = "Shoot one-sided wall with sky creates bulletpuff")]
    public void ShootOneSidedWallWithSky()
    {
        GameActions.SetEntityPosition(World, Player, (-352, -480, 0));
        World.FirePlayerHitscanBullets(Player, 1, 0, 0, 0.45, 2048, false, DamageFunc);
        GameActions.FindEntity(World, "BulletPuff").Should().NotBeNull();
    }

    [Fact(DisplayName = "Shoot one-sided wall ceiling")]
    public void ShootOneSidedWallCeiling()
    {
        GameActions.SetEntityPosition(World, Player, (-64, -480, 0));
        World.FirePlayerHitscanBullets(Player, 1, 0, 0, 0.658, 2048, false, DamageFunc);
        var bulletPuff = GameActions.FindEntity(World, "BulletPuff");
        bulletPuff.Should().NotBeNull();
        bulletPuff!.Position.ApproxEquals((-63.99999999999997, -29.750731254404627, 380)).Should().BeTrue();
    }

    [Fact(DisplayName = "Shoot two-sided wall creates bulletpuff")]
    public void ShootTwoSidedWall()
    {
        World.FirePlayerHitscanBullets(Player, 1, 0, 0, 0, 2048, false, DamageFunc);
        GameActions.FindEntity(World, "BulletPuff").Should().NotBeNull();
    }

    [Fact(DisplayName = "Shoot one-sided sky does not create bulletpuff")]
    public void ShootOneSidedSky()
    {
        GameActions.SetEntityPosition(World, Player, (-352, -480, 0));
        World.FirePlayerHitscanBullets(Player, 1, 0, 0, 0.6875, 2048, false, DamageFunc);
        GameActions.FindEntity(World, "BulletPuff").Should().BeNull();
    }

    [Fact(DisplayName = "Shoot two-sided sky does not create bulletpuff")]
    public void ShootTwoSidedSky()
    {
        GameActions.SetEntityPosition(World, Player, (-448, -480, 0));
        World.FirePlayerHitscanBullets(Player, 1, 0, 0, 0.6875, 2048, false, DamageFunc);
        GameActions.FindEntity(World, "BulletPuff").Should().BeNull();
    }

    [Fact(DisplayName = "Shoot floor creates bulletpuff")]
    public void ShootFloor()
    {
        World.FirePlayerHitscanBullets(Player, 1, 0, 0, -1.09375, 2048, false, DamageFunc);
        var bulletPuff = GameActions.FindEntity(World, "BulletPuff");
        bulletPuff.Should().NotBeNull();
        bulletPuff!.Position.ApproxEquals(new Vec3D(-256, -461.3929545568001, 0)).Should().BeTrue();
    }

    [Fact(DisplayName = "Shoot ceiling creates bulletpuff")]
    public void ShootCeiling()
    {
        World.FirePlayerHitscanBullets(Player, 1, 0, 0, 1.4818119517948967, 2048, false, DamageFunc);
        var bulletPuff = GameActions.FindEntity(World, "BulletPuff");
        bulletPuff.Should().NotBeNull();
        bulletPuff!.Position.ApproxEquals(new Vec3D(-256, -448.95144445352173, 380)).Should().BeTrue();
    }

    [Fact(DisplayName = "Monster is thrust forward when shot")]
    public void ShootMonsterThrustForward()
    {
        var zombieman = GameActions.CreateEntity(World, "ZombieMan", (-448, -112, 128));
        var angle = Player.Position.Angle(zombieman.Position);
        var pitch = Player.PitchTo(Player.HitscanAttackPos, zombieman);
        Player.AngleRadians = angle;
        // Damage must be less than 40 but more than remaining health with random & 1
        Random.RandomValue = 1;
        zombieman.Health = 5;
        World.FirePlayerHitscanBullets(Player, 1, 0, 0, pitch, 2048, false, (p) => 10);
        zombieman.IsDead.Should().BeTrue();
        zombieman.Velocity.ApproxEquals((2.4806946917841666, -4.341215710622298, 0));
    }

    [Fact(DisplayName = "Shoot ripper projectile")]
    public void Ripper()
    {
        var imps = GameActions.GetEntities(World, "DoomImp");
        imps.Count.Should().Be(4);
        GameActions.SetEntityPosition(World, Player, (96, -480, 0));
        var projectile = World.FireProjectile(Player, 0, 0, 2048, true, GetMissileDef(), out _);
        projectile.Should().NotBeNull();
        projectile!.Flags.Ripper = true;
        projectile!.Definition.Properties.Damage = new DamageRangeProperty { Value = 100, Exact = true };
        GameActions.TickWorld(World, 35 * 10);        
        foreach (var imp in imps)
            imp.IsDead.Should().BeTrue();
    }

    [Fact(DisplayName = "Shoot rail")]
    public void Rail()
    {
        var imps = GameActions.GetEntities(World, "DoomImp");
        imps.Count.Should().Be(4);
        GameActions.SetEntityPosition(World, Player, (96, -480, 0));
        World.FireHitscan(Player, Player.AngleRadians, 0, 8192, 1000, HitScanOptions.PassThroughEntities | HitScanOptions.DrawRail);
        Player.Tracers.Tracers.Count.Should().Be(1);
        Player.Tracers.Tracers.First.Should().NotBeNull();
        var tracer = Player.Tracers.Tracers.First!.Value;
        tracer.Segs.Count.Should().Be(1);
        var start = Player.HitscanAttackPos;
        tracer.Segs[0].Start.Should().Be(start);
        tracer.Segs[0].End.ApproxEquals(new Vec3D(96, -2, start.Z)).Should().BeTrue();
        foreach (var imp in imps)
            imp.IsDead.Should().BeTrue();
    }
}