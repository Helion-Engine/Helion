using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Maps.Shared;
using Helion.Resources.IWad;
using Helion.Util.RandomGenerators;
using Helion.World;
using Helion.World.Impl.SinglePlayer;
using System;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Id24;

[Collection("GameActions")]
public class Respawn
{
    private readonly SinglePlayerWorld World;
    private NoRandom Random = new();

    public Respawn()
    {
        World = WorldAllocator.LoadMap("Resources/box.zip", "box.WAD", "MAP01", GetType().Name, WorldInit, IWadType.Doom2, 
             skillLevel: SkillLevel.Nightmare, dehackedPatch: Dehacked, cacheWorld: false);
    }

    private void WorldInit(IWorld world)
    {
        WorldBase worldBase = (WorldBase)world;
        worldBase.SetRandom(Random);
    }

    [Fact(DisplayName = "Min respawn tics")]
    public void MinRespawnTicks()
    {
        // Force respawn immediately
        Random.RandomValue = 0;
        var monster = GameActions.CreateEntity(World, "ZombieMan", Vec3D.Zero, frozen: false);
        monster.Properties.RespawnTicks.Should().Be(175);
        monster.Kill(null);        
        
        // Respawn will dispose the current corpse and create a new one
        GameActions.TickWorld(World, () => !monster.IsDisposed, () => { }, TimeSpan.FromSeconds(30));

        monster = GameActions.GetEntity(World, "ZombieMan");
        monster.Should().NotBeNull();
    }

    [Fact(DisplayName = "Respawn dice")]
    public void RespawnDice()
    {
        // Disable respawn
        Random.RandomValue = 9;
        var monster = GameActions.CreateEntity(World, "ZombieMan", Vec3D.Zero, frozen: false);
        monster.Properties.RespawnDice.Should().Be(8);
        monster.Kill(null);

        GameActions.TickWorld(World, 35 * 30);

        monster.IsDisposed.Should().BeFalse();
        Random.RandomValue = 8;

        GameActions.TickWorld(World, () => !monster.IsDisposed, () => { }, TimeSpan.FromSeconds(30));

        monster = GameActions.GetEntity(World, "ZombieMan");
        monster.Should().NotBeNull();
    }

    [Fact(DisplayName = "No respawn")]
    public void NoRespawn()
    {
        var monster = GameActions.CreateEntity(World, "ShotgunGuy", Vec3D.Zero, frozen: false);
        monster.Flags.NoRespawn.Should().BeTrue();

        monster.Kill(null);

        GameActions.TickWorld(World, 35 * 30);
        monster.IsDisposed.Should().BeFalse();
    }

    private static readonly string Dehacked =
@"Thing 2 (ZombieMan)
Min respawn tics = 175
Respawn dice = 8

Thing 3 (ShotgunGuy)
Min respawn tics = 0
Respawn dice = 0
ID24 Bits = NORESPAWN
";
}
