﻿using System;
using System.Collections.Generic;
using System.Linq;
using FluentAssertions;
using Helion.Resources.IWad;
using Helion.Util;
using Helion.World;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class Misc
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public Misc()
    {
        World = WorldAllocator.LoadMap("Resources/box.zip", "box.WAD", "MAP01", GetType().Name, WorldInit,
            IWadType.Doom2);
    }

    private void WorldInit(SinglePlayerWorld world)
    {
    }

    [Fact(DisplayName = "Limit lost souls to 21")]
    public void PainElementalLostSoulLimit()
    {
        World.Config.Compatibility.PainElementalLostSoulLimit.Set(true);
        var painElemental = GameActions.CreateEntity(World, "PainElemental", (-320, -320, 0));
        var lostSoulId = World.EntityManager.DefinitionComposer.GetByName("LostSoul")!.Id;
        painElemental.SetTarget(Player);

        for (int i = 0; i < 20; i++)
            GameActions.CreateEntity(World, "LostSoul", (0, 0, 0));

        World.EntityCount(lostSoulId).Should().Be(20);

        painElemental.SetMissileState();
        GameActions.TickWorld(World, 35);
        World.EntityCount(lostSoulId).Should().Be(21);

        painElemental.SetMissileState();
        GameActions.TickWorld(World, 35);
        World.EntityCount(lostSoulId).Should().Be(21);

        World.Config.Compatibility.PainElementalLostSoulLimit.Set(false);

        painElemental.SetMissileState();
        GameActions.TickWorld(World, 35);
        World.EntityCount(lostSoulId).Should().Be(22);
    }

    [Fact(DisplayName = "No toss drops off")]
    public void NoTossDropsOff()
    {
        var monsters = GameActions.CreateEntity(World, "ZombieMan", (-320, -320, 0));
        monsters.Kill(null);

        var clip = GameActions.GetEntity(World, "Clip");
        clip.Position.Z.Should().BeGreaterThan(0);
        clip.Velocity.Z.Should().BeGreaterThan(0);
    }

    [Fact(DisplayName = "No toss drops on")]
    public void NoTossDropsOn()
    {
        World.Config.Compatibility.NoTossDrops.Set(true);
        var monsters = GameActions.CreateEntity(World, "ZombieMan", (-320, -320, 0));
        monsters.Kill(null);

        var clip = GameActions.GetEntity(World, "Clip");
        clip.Position.Z.Should().Be(0);
        clip.Velocity.Z.Should().Be(0);
        World.Config.Compatibility.NoTossDrops.Set(false);
    }

    [Fact(DisplayName = "Hitscan default option")]
    public void HitscanDefault()
    {
        List<Entity> monsters = new();
        for (int i = 0; i < 3; i++)
            monsters.Add(GameActions.CreateEntity(World, "Zombieman", (-320, -576 + (i * 64), 0)));

        foreach (var monster in monsters)
            monster.IsDead.Should().BeFalse();

        GameActions.SetEntityToLine(World, Player, 2, 64);
        GameActions.SetEntityPosition(World, Player, (-320, -160));
        World.FireHitscan(Player, Player.AngleRadians, 0, 2048, 1000);

        monsters[0].IsDead.Should().BeFalse();
        monsters[1].IsDead.Should().BeFalse();
        monsters[2].IsDead.Should().BeTrue();
    }

    [Fact(DisplayName = "Hitscan pass through option")]
    public void HitscanPassThroughOption()
    {
        List<Entity> monsters = new();
        for (int i = 0; i < 3; i++)
            monsters.Add(GameActions.CreateEntity(World, "Zombieman", (-320, -576 + (i*64), 0)));

        var behindLineMonster = GameActions.CreateEntity(World, "Zombieman", (-320, -704, 0));

        foreach (var monster in monsters)
            monster.IsDead.Should().BeFalse();
        behindLineMonster.IsDead.Should().BeFalse();

        GameActions.SetEntityToLine(World, Player, 2, 64);
        GameActions.SetEntityPosition(World, Player, (-320, -160));
        World.FireHitscan(Player, Player.AngleRadians, 0, 2048, 1000, HitScanOptions.PassThroughEntities);

        foreach (var monster in monsters)
            monster.IsDead.Should().BeTrue();
        behindLineMonster.IsDead.Should().BeFalse();
    }
}

