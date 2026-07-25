using FluentAssertions;
using Helion.Resources.Definitions.MapInfo;
using Helion.Resources.IWad;
using Helion.Util.RandomGenerators;
using Helion.World.Entities;
using Helion.World.Entities.Definition.States;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using System;
using Xunit;
using Helion.Util.Configs.Components;

namespace Helion.Tests.Unit.GameAction.Boom;

[Collection("GameActions")]
public class BoomTelefrag : IDisposable
{
    private readonly SinglePlayerWorld World;
    private readonly Entity Imp;
    private readonly Entity ZombieMan;
    private readonly Entity Spawn;
    private readonly NoRandom NoRandom = new();

    public BoomTelefrag()
    {
        World = WorldAllocator.LoadMap("Resources/boomtelefrag.zip", "boomtelefrag.wad", "MAP30", GetType().Name, WorldInit, IWadType.Doom2, cacheWorld: false);
        World.SetRandom(NoRandom);
        Imp = GameActions.GetEntity(World, 1);
        ZombieMan = GameActions.GetEntity(World, 0);
        Spawn = GameActions.GetEntity(World, 4);
    }

    public void Dispose()
    {
        World.MapInfo.SetOption(MapOptions.AllowMonsterTelefrags, false);
        World.Config.Compatibility.MbfTelefrag.Set(true);
        World.SetRandom(NoRandom);
        GC.SuppressFinalize(this);
    }

    private void WorldInit(SinglePlayerWorld world)
    {
        world.Config.Compatibility.MbfTelefrag.Value.ToBool().Should().BeTrue();
        world.MapInfo.HasOption(MapOptions.AllowMonsterTelefrags).Should().BeTrue();
    }

    [Fact(DisplayName = "Monster teleport doesn't telefrag because MbfTelefrag is on")]
    public void MonsterTeleportNoTelefragCompatOn()
    {
        TeleportZombieMan().Should().BeFalse();
        Imp.IsDead().Should().BeFalse();
    }

    [Fact(DisplayName = "Monster teleport telefrags because MbfTelefrag is off")]
    public void MonsterTeleportTelefragCompatOff()
    {
        World.Config.Compatibility.MbfTelefrag.Set(false);
        TeleportZombieMan().Should().BeTrue();
        Imp.IsDead().Should().BeTrue();
    }

    [Fact(DisplayName = "Boss spawn fly telefrags with MbfTelefrag on")]
    public void BossSpawnTelefragCompatOn()
    {
        DoSpawnFly();
        Imp.IsDead().Should().BeTrue();
        GameActions.GetEntity(World, "Demon");
    }

    [Fact(DisplayName = "Boss spawn fly telefrags with MbfTelefrag off")]
    public void BossSpawnTelefragCompatOff()
    {
        World.Config.Compatibility.MbfTelefrag.Set(false);
        DoSpawnFly();
        Imp.IsDead().Should().BeTrue();
        GameActions.GetEntity(World, "Demon");
    }

    [Fact(DisplayName = "Boss spawn fly doesn't telefrag with MbfTelefrag off and MapInfo AllowMonsterTelefrags off")]
    public void BossSpawnTelefragCompatOffAndAllowMonsterTelefragsOff()
    {
        World.Config.Compatibility.MbfTelefrag.Set(false);
        World.MapInfo.SetOption(MapOptions.AllowMonsterTelefrags, false);
        DoSpawnFly();
        Imp.IsDead().Should().BeFalse();
    }

    private bool TeleportZombieMan() =>
        GameActions.ActivateLine(World, ZombieMan, 12, ActivationContext.CrossLine);

    private void DoSpawnFly()
    {
        NoRandom.RandomValue = 80;
        var entity = GameActions.CreateEntity(World, "SpawnShot", (0, 0, 0), frozen: false);
        entity.ReactionTime = 0;
        entity.SetTarget(Spawn);
        EntityActionFunctions.A_SpawnFly(entity);
    }
}
