using FluentAssertions;
using Helion.Resources.IWad;
using Helion.Util;
using Helion.World;
using Helion.World.Cheats;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using System;
using Xunit;
using static Helion.World.Entities.Definition.States.EntityActionFunctions;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class EnemyState : IDisposable
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public EnemyState()
    {
        World = WorldAllocator.LoadMap("Resources/box.zip", "box.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
        World.CheatManager.ActivateCheat(Player, CheatType.God);
    }

    public void Dispose()
    {
        foreach (var sector in World.Sectors)
            sector.SoundTarget = new(null);

        GameActions.DestroyCreatedEntities(World);
    }

    [Fact(DisplayName = "Enemy spawn to see state from LOS")]
    public void SpawnToSeeStateLOS()
    {
        var imp = GameActions.CreateEntity(World, "DoomImp", (-320, -576, 0), frozen: false);
        imp.Target().Should().BeNull();
        AssertState(imp, Constants.FrameStates.Spawn);
        GameActions.SetEntityPosition(World, Player, (-320, -320));
        GameActions.TickWorld(World, 15);
        imp.Target().Should().Be(Player);
        AssertState(imp, Constants.FrameStates.See);
        GameActions.SetEntityOutOfBounds(World, Player);
    }

    [Fact(DisplayName = "Enemy spawn to see state from damage")]
    public void SpawnToSeeStateDamage()
    {
        var imp = GameActions.CreateEntity(World, "DoomImp", (-320, -576, 0), frozen: false);
        var baron = GameActions.CreateEntity(World, "BaronOfHell", (-320, -420, 0), frozen: false);
        imp.Target().Should().BeNull();
        AssertState(imp, Constants.FrameStates.Spawn);

        imp.Damage(baron, 1, true, DamageType.Normal);
        imp.Target().Should().Be(baron);
        AssertState(imp, Constants.FrameStates.See);
    }

    [Fact(DisplayName = "Enemy spawn to see state from sector sound target")]
    public void SpawnToSeeSoundTarget()
    {
        var imp = GameActions.CreateEntity(World, "DoomImp", (-320, -576, 0), frozen: false);
        imp.Target().Should().BeNull();
        imp.Sector.SoundTarget = new(Player);
        GameActions.TickWorld(World, 15);
        imp.Target().Should().Be(Player);
    }

    [Fact(DisplayName = "Enemy stays in spawn state from damage when pain state isn't set")]
    public void StaySpawnStateFromDamage()
    {
        var imp = GameActions.CreateEntity(World, "DoomImp", (-320, -576, 0), frozen: false);
        var baron = GameActions.CreateEntity(World, "BaronOfHell", (-320, -420, 0), frozen: false);
        imp.Target().Should().BeNull();
        imp.FrameState.SetFrameIndex(imp, imp.FrameState.Frame.NextFrameIndex);
        AssertState(imp, Constants.FrameStates.Spawn);

        // Doom would set the target but not change the state unless the frame matched exactly
        imp.Damage(baron, 1, false, DamageType.Normal);
        imp.Target().Should().Be(baron);
        AssertState(imp, Constants.FrameStates.Spawn);
    }

    [Fact(DisplayName = "Enemy goes back to spawn state when target dies")]
    public void BackToSeeState()
    {
        var imp = GameActions.CreateEntity(World, "DoomImp", (-320, -576, 0), frozen: false);
        var baron = GameActions.CreateEntity(World, "BaronOfHell", (-320, -420, 0), frozen: false);
        baron.Target().Should().BeNull();
        AssertState(baron, Constants.FrameStates.Spawn);
        baron.Damage(imp, 1, true, DamageType.Normal);
        baron.Target().Should().Be(imp);
        AssertState(baron, Constants.FrameStates.See);
        imp.Kill(baron);
        GameActions.TickWorld(World, 15);
        AssertState(baron, Constants.FrameStates.Spawn);
    }

    [Fact(DisplayName = "Enemy targets player after enemy target dies")]
    public void TargetsPlayerAfterMonster()
    {
        var imp = GameActions.CreateEntity(World, "DoomImp", (-320, -576, 0), frozen: false);
        var baron = GameActions.CreateEntity(World, "BaronOfHell", (-320, -420, 0), frozen: false);
        baron.Target().Should().BeNull();
        AssertState(baron, Constants.FrameStates.Spawn);
        baron.Damage(imp, 1, true, DamageType.Normal);
        baron.Target().Should().Be(imp);
        AssertState(baron, Constants.FrameStates.See);
        imp.Kill(baron);
        GameActions.TickWorld(World, 15);
        AssertState(baron, Constants.FrameStates.Spawn);

        GameActions.SetEntityPosition(World, Player, (-320, -320));
        GameActions.SetEntityPosition(World, baron, (-320, -420));
        baron.AngleRadians = GameActions.GetAngle(Bearing.North);
        GameActions.TickWorld(World, 15);
        baron.Target().Should().Be(Player);
        AssertState(baron, Constants.FrameStates.See);

        GameActions.SetEntityOutOfBounds(World, Player);
    }

    [Fact(DisplayName = "Enemy sets death state from damage")]
    public void DeathState()
    {
        var imp = GameActions.CreateEntity(World, "DoomImp", (-320, -576, 0), frozen: false);
        imp.Damage(Player, imp.Health, true, DamageType.Normal);
        imp.IsDead().Should().BeTrue();
        AssertState(imp, Constants.FrameStates.Death);
    }

    [Fact(DisplayName = "Enemy sets xdeath state from damage")]
    public void XDeathState()
    {
        var imp = GameActions.CreateEntity(World, "DoomImp", (-320, -576, 0), frozen: false);
        imp.Damage(Player, imp.Health * 10, true, DamageType.Normal);
        imp.IsDead().Should().BeTrue();
        // XDeath immediately moves frames
        AssertNotState(imp, Constants.FrameStates.Death);
    }

    [Fact(DisplayName = "Enemy raise state")]
    public void RaiseState()
    {
        var imp = GameActions.CreateEntity(World, "DoomImp", (-320, -576, 0), frozen: false);
        GameActions.SetEntityPosition(World, Player, (-320, -320));
        GameActions.TickWorld(World, 15);
        imp.Target().Should().Be(Player);
        AssertState(imp, Constants.FrameStates.See);
        imp.Kill(null);
        imp.IsDead().Should().BeTrue();
        AssertNotState(imp, Constants.FrameStates.See);
        imp.SetRaiseState();
        GameActions.TickWorld(World, 35);
        AssertState(imp, Constants.FrameStates.See);
    }

    [Fact(DisplayName = "Lost soul missile state goes to spawn and back to see")]
    public void LostSoulMissileState()
    {
        // Lost souls are weird, after missile they go back to spawn state.
        var soul = GameActions.CreateEntity(World, "LostSoul", (-320, -576, 0), frozen: false);
        AssertState(soul, Constants.FrameStates.Spawn);

        GameActions.SetEntityPosition(World, Player, (-320, -320));
        soul.Damage(Player, 1, true, DamageType.Normal);
        soul.Target().Should().Be(Player);
        AssertState(soul, Constants.FrameStates.See);

        GameActions.TickWorld(World, () => { return !IsFrameAction(soul, A_SkullAttack); }, () => { });
        GameActions.SetEntityOutOfBounds(World, Player);
        GameActions.TickWorld(World, () => { return soul.Velocity.Y > 0; }, () => { });

        AssertState(soul, Constants.FrameStates.Spawn);

        GameActions.SetEntityPosition(World, Player, (-320, -320));

        soul.AngleRadians = GameActions.GetAngle(Bearing.South);
        GameActions.TickWorld(World, 15);

        AssertState(soul, Constants.FrameStates.See);
        soul.Target().Should().Be(Player);
    }

    [Fact(DisplayName = "Friendly monster targets player and then enemy")]
    public void FriendlyTarget()
    {
        var baron = GameActions.CreateEntity(World, "BaronOfHell", (-320, -420, 0), frozen: false);
        baron.Flags.SetFriendly();
        baron.Target().Should().BeNull();
        baron.Sector.SoundTarget = new(Player);
        GameActions.TickWorld(World, 15);
        baron.Target().Should().Be(Player);

        var imp = GameActions.CreateEntity(World, "DoomImp", (-320, -576, 0), frozen: false);
        GameActions.TickWorld(World, 15);
        baron.Target().Should().Be(imp);

        imp.Kill(baron);
        GameActions.TickWorld(World, 15);
        baron.Target().Should().Be(Player);
    }

    private static bool IsFrameAction(Entity entity, ActionFunction action)
    {
        return entity.FrameState.Frame.ActionFunction == action;
    }

    private static void AssertState(Entity entity, string label)
    {
        entity.Definition.States.Labels.TryGetValue(label, out var index).Should().BeTrue();
        var frame = entity.FrameState.Frame;
        if (frame.MasterFrameIndex == index)
            return;

        var startFrame = frame;
        frame = frame.NextFrame;
        while (frame != startFrame)
        {
            if (frame.MasterFrameIndex == index)
                return;

            frame = frame.NextFrame;
            if (frame.Ticks == -1)
                break;
        }

        throw new Exception($"Entity is not frame state: {label}");
    }

    private static void AssertNotState(Entity entity, string label)
    {
        entity.Definition.States.Labels.TryGetValue(label, out var index).Should().BeTrue();
        var frame = entity.FrameState.Frame;
        if (frame.MasterFrameIndex == index)
            goto FailException;

        var startFrame = frame;
        frame = frame.NextFrame;
        while (frame != startFrame)
        {
            if (frame.MasterFrameIndex == index)
                goto FailException;

            frame = frame.NextFrame;
            if (frame.Ticks == -1)
                break;
        }

        return;

    FailException:
        throw new Exception($"Entity is not frame state: {label}");
    }
}
