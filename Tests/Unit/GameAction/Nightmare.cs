using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Maps.Shared;
using Helion.Resources.IWad;
using Helion.Util.RandomGenerators;
using Helion.World.Cheats;
using Helion.World.Entities.Definition.States;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using System;
using System.Linq;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class Nightmare
{
    private SinglePlayerWorld World;
    private Player Player => World.Player;
    private readonly NoRandom m_random = new();

    public Nightmare()
    {
        World = WorldAllocator.LoadMap("Resources/box.zip", "box.wad", "MAP01", GetType().Name, WorldInit, IWadType.Doom2, SkillLevel.Nightmare, cacheWorld: false);
        World.SetRandom(m_random);
    }

    private void WorldInit(SinglePlayerWorld world)
    {
        world.Player.Cheats.SetCheatActive(CheatType.God);
        // Force missile attacks to run immediately
        m_random.RandomValue = 255;
    }

    [Fact(DisplayName = "Nightmare init")]
    public void NightmareInit()
    {
        World.IsFastMonsters.Should().BeTrue();
    }

    [Fact(DisplayName = "Nightmare demon")]
    public void NightmareDemon()
    {
        var demon = GameActions.CreateEntity(World, "Demon", (-320, -64, 0), frozen: false);
        demon.AngleRadians = GameActions.GetAngle(Bearing.South);
        GameActions.SetEntityPosition(World, Player, (-320, -320, 0));

        GameActions.TickWorld(World, 15);
        demon.FrameState.Frame.Properties.Fast.Should().BeTrue();
        demon.Position.X.Should().Be(-320);
        demon.Position.Y.Should().Be(-124);

        GameActions.TickWorld(World, () => demon.FrameState.Frame.ActionFunction != EntityActionFunctions.A_SargAttack, () => { });
        // Normal duration for attack is 8. Should be halved.
        GameActions.TickWorld(World, 4);
        (demon.FrameState.Frame.ActionFunction == EntityActionFunctions.A_FaceTarget).Should().BeTrue();
    }

    [Fact(DisplayName = "Nightmare imp")]
    public void NighmareImp()
    {
        var imp = GameActions.CreateEntity(World, "DoomImp", (-320, -64, 0), frozen: false);
        imp.AngleRadians = GameActions.GetAngle(Bearing.South);
        GameActions.SetEntityPosition(World, Player, (-320, -512, 0));

        GameActions.TickWorld(World, 15);
        imp.FrameState.Frame.Properties.Fast.Should().BeFalse();

        GameActions.TickWorld(World, () => imp.FrameState.Frame.ActionFunction != EntityActionFunctions.A_FaceTarget, () => { });

        GameActions.TickWorld(World, 22);
        var fireball = GameActions.GetEntity(World, "DoomImpBall");
        fireball.Should().NotBeNull();
        // Fireball should be twice as fast (10 -> 20)
        fireball.Velocity.Should().Be(new Vec3D(0, -20, 0));
    }

    [Fact(DisplayName = "Nightmare respawn")]
    public void NightmareRespawn()
    {
        // Force random respawn to work immediately (needs to be less than four)
        m_random.RandomValue = 0;

        var imp = GameActions.CreateEntity(World, "DoomImp", (-320, -64, 0), frozen: false);
        GameActions.SetEntityPosition(World, imp, (-320, -128, 0));
        imp.Kill(null);

        // Respawn will dispose the current imp corpse and create a new one
        GameActions.TickWorld(World, () => !imp.IsDisposed, () => { }, TimeSpan.FromSeconds(20));

        imp = GameActions.GetEntity(World, "DoomImp");
        imp.Should().NotBeNull();

        var fog = GameActions.GetEntities(World, "TeleportFog");
        fog.Count.Should().Be(2);

        fog.Any(x => x.Position == (-320, -64, 0)).Should().BeTrue();
        fog.Any(x => x.Position == (-320, -128, 0)).Should().BeTrue();
    }
}
