using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Resources.IWad;
using Helion.World.Entities;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using System;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Mbf21;

// Tests A_FindTracer, A_SeekTracer, and A_JumpIfTracerInSight
[Collection("GameActions")]
public class Tracer
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public Tracer()
    {
        World = WorldAllocator.LoadMap("Resources/tracer.zip", "tracer.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2, cacheWorld: false);
    }

    [Fact(DisplayName = "A_FindTracer 4 block fails (out of range)")]
    public void FindTracerFourFail()
    {
        var zombie = GameActions.GetEntity(World, "ZombieMan");
        var imp = GameActions.GetEntity(World, "DoomImp");
        CreateTracer("*deh/entity248", (-640, 256, 32));
        GameActions.TickWorld(World, 60 * 35);
        zombie.IsDead().Should().BeFalse();
        imp.IsDead().Should().BeFalse();
    }

    [Fact(DisplayName = "A_FindTracer 4 block success")]
    public void FindTracerFourSuccess()
    {
        var zombie = GameActions.GetEntity(World, "ZombieMan");
        var imp = GameActions.GetEntity(World, "DoomImp");
        CreateTracer("*deh/entity248", (-1024, 256, 32));
        GameActions.TickWorld(World, 60 * 35);
        zombie.IsDead().Should().BeTrue();
        imp.IsDead().Should().BeTrue();
    }

    [Fact(DisplayName = "A_FindTracer 4 block friendly")]
    public void FindTracerFourFriendly()
    {
        var zombie = GameActions.GetEntity(World, "ZombieMan");
        var imp = GameActions.GetEntity(World, "DoomImp");
        zombie.Flags.SetFriendly();
        imp.Flags.SetFriendly();
        CreateTracer("*deh/entity248", (-1024, 256, 32));
        GameActions.TickWorld(World, 60 * 35);
        zombie.IsDead().Should().BeFalse();
        imp.IsDead().Should().BeFalse();
    }

    [Fact(DisplayName = "A_FindTracer 12 block fail")]
    public void FindTracerTwelveFail()
    {
        var zombie = GameActions.GetEntity(World, "ZombieMan");
        var imp = GameActions.GetEntity(World, "DoomImp");
        CreateTracer("*deh/entity249", (512, 256, 32));
        GameActions.TickWorld(World, 60 * 35);
        zombie.IsDead().Should().BeFalse();
        imp.IsDead().Should().BeFalse();
    }

    [Fact(DisplayName = "A_FindTracer 12 block Success")]
    public void FindTracerTwelveSuccess()
    {
        var zombie = GameActions.GetEntity(World, "ZombieMan");
        var imp = GameActions.GetEntity(World, "DoomImp");
        CreateTracer("*deh/entity249", (256, 256, 32));
        GameActions.TickWorld(World, 60 * 35);
        zombie.IsDead().Should().BeFalse();
        imp.IsDead().Should().BeFalse();
    }

    private Entity CreateTracer(string name, Vec3D pos)
    {
        var entity = GameActions.CreateEntity(World, name, pos, frozen: false);
        entity.SetOwner(Player);
        entity.Velocity.Y = 32;
        return entity;
    }
}
