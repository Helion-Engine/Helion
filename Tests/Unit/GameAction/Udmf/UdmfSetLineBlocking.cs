using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Lines;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using System;
using System.Collections.Generic;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Udmf;

[Collection("GameActions")]
public class UdmfSetLineBlocking : IDisposable
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    private static LineBlockFlags NoBlocking;

    public UdmfSetLineBlocking()
    {
        World = WorldAllocator.LoadMap("Resources/udmflinesetblocking.zip", "udmflinesetblocking.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
        NoBlocking = new();
    }

    public void Dispose()
    {
        var line = GameActions.GetLine(World, 8);
        line.Flags.Blocking = NoBlocking;
        line.Flags.BlockSound = false;
    }

    [Fact(DisplayName = "Line_SetBlocking set some flags")]
    public void LineSetBlockingSetSome()
    {
        var line = GameActions.GetLine(World, 8);
        line.Flags.Blocking.Should().Be(NoBlocking);
        GameActions.ActivateLine(World, Player, 4, ActivationContext.UseLine).Should().BeTrue();
        AssertFlags(line.Flags.Blocking, nameof(LineBlockFlags.LegacyImpassible), nameof(LineBlockFlags.Players), nameof(LineBlockFlags.Monsters), nameof(LineBlockFlags.Use), nameof(LineBlockFlags.Hitscan));
        line.Flags.BlockSound.Should().BeTrue();
    }

    [Fact(DisplayName = "Line_SetBlocking clear some flags")]
    public void LineSetBlockingClearSome()
    {
        var line = GameActions.GetLine(World, 8);
        line.Flags.Blocking.Should().Be(NoBlocking);
        GameActions.ActivateLine(World, Player, 4, ActivationContext.UseLine).Should().BeTrue();
        AssertFlags(line.Flags.Blocking, nameof(LineBlockFlags.LegacyImpassible), nameof(LineBlockFlags.Players), nameof(LineBlockFlags.Monsters), nameof(LineBlockFlags.Use), nameof(LineBlockFlags.Hitscan));

        GameActions.ActivateLine(World, Player, 11, ActivationContext.UseLine).Should().BeTrue();
        AssertFlags(line.Flags.Blocking, nameof(LineBlockFlags.Hitscan));
        line.Flags.BlockSound.Should().BeTrue();
    }

    [Fact(DisplayName = "Line_SetBlocking set all flags")]
    public void LineSetBlockingSetAll()
    {
        var line = GameActions.GetLine(World, 8);
        GameActions.ActivateLine(World, Player, 15, ActivationContext.UseLine).Should().BeTrue();
        AssertAllFlags(line.Flags.Blocking, true);
        line.Flags.BlockSound.Should().BeTrue();
    }

    [Fact(DisplayName = "Line_SetBlocking clear all flags")]
    public void LineSetBlockingClearAll()
    {
        var line = GameActions.GetLine(World, 8);
        GameActions.ActivateLine(World, Player, 15, ActivationContext.UseLine).Should().BeTrue();
        AssertAllFlags(line.Flags.Blocking, true);
        line.Flags.BlockSound.Should().BeTrue();

        GameActions.ActivateLine(World, Player, 19, ActivationContext.UseLine).Should().BeTrue();
        AssertAllFlags(line.Flags.Blocking, false);
        line.Flags.BlockSound.Should().BeFalse();
    }

    private static void AssertFlags(in LineBlockFlags actual, params string[] expectedTrue)
    {
        var expectedSet = new HashSet<string>(expectedTrue);

        foreach (var prop in typeof(LineBlockFlags).GetFields())
        {
            var value = (bool)prop.GetValue(actual)!;
            var expected = expectedSet.Contains(prop.Name);

            if (value != expected)
            {
                throw new Exception(
                    $"Flag mismatch: {prop.Name} was {(value ? "true" : "false")} but expected {(expected ? "true" : "false")}.");
            }
        }
    }

    private static void AssertAllFlags(in LineBlockFlags actual, bool expected)
    {
        foreach (var prop in typeof(LineBlockFlags).GetFields())
        {
            if (prop.Name == nameof(LineBlockFlags.PlayersMbf21) || prop.Name == nameof(LineBlockFlags.LandMonstersMbf21) || 
                prop.Name == nameof(LineBlockFlags.MidTex3D) || prop.Name == nameof(LineBlockFlags.BlockMissileMidTex3D))
                continue;

            var value = (bool)prop.GetValue(actual)!;
            if (value != expected)
            {
                throw new Exception(
                    $"Flag mismatch: {prop.Name} was {(value ? "true" : "false")} but expected {(expected ? "true" : "false")}.");
            }
        }
    }
}
