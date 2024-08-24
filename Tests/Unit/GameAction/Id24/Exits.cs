using FluentAssertions;
using Helion.Resources.IWad;
using Helion.World;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using Helion.World.Physics;
using System;
using Xunit;

namespace Helion.Tests.Unit.GameAction.Id24;

[Collection("GameActions")]
public class Exits
{
    private readonly SinglePlayerWorld World;
    private Player Player => World.Player;

    public Exits()
    {
        World = WorldAllocator.LoadMap("Resources/id24exits.zip", "id24exits.WAD", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2);
    }

    [Fact(DisplayName = "2070 - S1 ExitResetInventory")]
    public void Action2070_ExitResetInventory()
    {
        AssertExit(LevelChangeType.Next, LevelChangeFlags.ResetInventory, () =>
        {
            GameActions.ActivateLine(World, Player, 2, ActivationContext.UseLine).Should().BeTrue();
        });
    }

    [Fact(DisplayName = "2071 - G1 ExitResetInventory")]
    public void Action2071_ExitResetInventory()
    {
        AssertExit(LevelChangeType.Next, LevelChangeFlags.ResetInventory, () =>
        {
            GameActions.SetEntityToLine(World, Player, 4, Player.Radius * 2).Should().BeTrue();
            GameActions.PlayerFirePistol(World, Player).Should().BeTrue();
        });
    }


    [Fact(DisplayName = "2073 - S1 ExitSecretResetInventory")]
    public void Action2073_ExitSecretResetInventory()
    {
        AssertExit(LevelChangeType.SecretNext, LevelChangeFlags.ResetInventory, () =>
        {
            GameActions.ActivateLine(World, Player, 5, ActivationContext.UseLine).Should().BeTrue();
        });
    }

    [Fact(DisplayName = "2069 - W1 ExitResetInventory")]
    public void Action2069_ExitResetInventory()
    {
        AssertExit(LevelChangeType.Next, LevelChangeFlags.ResetInventory, () =>
        {
            GameActions.ActivateLine(World, Player, 8, ActivationContext.CrossLine).Should().BeTrue();
        });
    }

    [Fact(DisplayName = "2074 - G1 ExitSecretResetInventory")]
    public void Action2074_ExitSecretResetInventory()
    {
        AssertExit(LevelChangeType.SecretNext, LevelChangeFlags.ResetInventory, () =>
        {
            GameActions.SetEntityToLine(World, Player, 6, Player.Radius * 2).Should().BeTrue();
            GameActions.PlayerFirePistol(World, Player).Should().BeTrue();
        });
    }

    [Fact(DisplayName = "2072 - W1 ExitSecretResetInventory")]
    public void Action2072_ExitSecretResetInventory()
    {
        AssertExit(LevelChangeType.SecretNext, LevelChangeFlags.ResetInventory, () =>
        {
            GameActions.ActivateLine(World, Player, 14, ActivationContext.CrossLine).Should().BeTrue();
        });
    }

    private void AssertExit(LevelChangeType levelChangeType, LevelChangeFlags levelChangeFlags, Action action)
    {
        bool exited = false;
        LevelChangeEvent? levelChangeEvent = null;
        World.LevelExit += World_LevelExit;
        action();
        GameActions.TickWorld(World, () => { return !exited; }, () => { });
        levelChangeEvent.Should().NotBeNull();
        levelChangeEvent!.ChangeType.Should().Be(levelChangeType);
        levelChangeEvent!.Flags.Should().Be(levelChangeFlags);

        void World_LevelExit(object? sender, LevelChangeEvent e)
        {
            e.Cancel = true;
            levelChangeEvent = e;
            exited = true;
        }
    }
}