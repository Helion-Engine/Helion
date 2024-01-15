using FluentAssertions;
using Helion.Resources.Definitions.Decorate.Properties;
using Helion.Resources.IWad;
using Helion.Tests.Unit.GameAction.Util;
using Helion.Util;
using Helion.World;
using Helion.World.Cheats;
using Helion.World.Entities.Inventories.Powerups;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using System;
using Xunit;

namespace Helion.Tests.Unit.GameAction;

[Collection("GameActions")]
public class PlayerCheats
{
    const string RedCard = "RedCard";
    const string BlueCard = "BlueCard";
    const string YellowCard = "YellowCard";
    const string RedSkull = "RedSkull";
    const string BlueSkull = "BlueSkull";
    const string YellowSkull = "YellowSkull";

    private SinglePlayerWorld World;
    private Player Player => World.Player;

    public PlayerCheats()
    {
        World = WorldAllocator.LoadMap("Resources/box.zip", "box.wad", "MAP01", GetType().Name, (world) => { }, IWadType.Doom2, cacheWorld: false);
    }

    [Fact(DisplayName = "Cheat no clip")]
    public void NoClip()
    {
        Player.Flags.NoClip.Should().BeFalse();
        AssertCheat(CheatType.NoClip, false);
        World.CheatManager.ActivateCheat(Player, CheatType.NoClip);
        AssertCheat(CheatType.NoClip, true);
        Player.Flags.NoClip.Should().BeTrue();
        World.CheatManager.DeactivateCheat(Player, CheatType.NoClip);
        AssertCheat(CheatType.NoClip, false);
        Player.Flags.NoClip.Should().BeFalse();
    }

    [Fact(DisplayName = "Cheat god")]
    public void God()
    {
        Player.Health = 69;
        AssertCheat(CheatType.God, false);
        World.CheatManager.ActivateCheat(Player, CheatType.God);
        AssertCheat(CheatType.God, true);
        Player.Health.Should().Be(100);
        World.CheatManager.DeactivateCheat(Player, CheatType.God);
        AssertCheat(CheatType.God, false);
    }

    [Fact(DisplayName = "Cheat fly")]
    public void Fly()
    {
        Player.Flags.Fly.Should().BeFalse();
        Player.Flags.NoGravity.Should().BeFalse();
        AssertCheat(CheatType.Fly, false);
        World.CheatManager.ActivateCheat(Player, CheatType.Fly);
        AssertCheat(CheatType.Fly, true);
        Player.Flags.Fly.Should().BeTrue();
        Player.Flags.NoGravity.Should().BeTrue();
        World.CheatManager.DeactivateCheat(Player, CheatType.Fly);
        AssertCheat(CheatType.Fly, false);
        Player.Flags.Fly.Should().BeFalse();
        Player.Flags.NoGravity.Should().BeFalse();
    }

    [Fact(DisplayName = "Cheat resurrect")]
    public void Resurrect()
    {
        Player.Kill(null);
        Player.IsDead.Should().BeTrue();
        Player.Health.Should().Be(0);
        GameActions.TickWorld(World, 35);
        Player.WeaponOffset.Y.Should().Be(Constants.WeaponBottom);
        World.CheatManager.ActivateCheat(Player, CheatType.Resurrect);
        GameActions.TickWorld(World, 35);
        Player.WeaponOffset.Y.Should().Be(Constants.WeaponTop);
        Player.IsDead.Should().BeFalse();
        Player.Health.Should().Be(100);
    }

    [Fact(DisplayName = "Cheat give all no keys")]
    public void GiveAllNoKeys()
    {
        World.CheatManager.ActivateCheat(Player, CheatType.GiveAllNoKeys);
        InventoryUtil.AssertHasWeapon(Player, "Chainsaw");
        InventoryUtil.AssertHasWeapon(Player, "Shotgun");
        InventoryUtil.AssertHasWeapon(Player, "SuperShotgun");
        InventoryUtil.AssertHasWeapon(Player, "Chaingun");
        InventoryUtil.AssertHasWeapon(Player, "RocketLauncher");
        InventoryUtil.AssertHasWeapon(Player, "PlasmaRifle");
        InventoryUtil.AssertHasWeapon(Player, "BFG9000");
        Player.Armor.Should().Be(200);
        Player.ArmorProperties.Should().NotBeNull();
        Player.ArmorProperties!.Armor.SavePercent.Should().Be(50);
        InventoryUtil.AssertInventoryDoesNotContain(Player, RedCard);
        InventoryUtil.AssertInventoryDoesNotContain(Player, BlueCard);
        InventoryUtil.AssertInventoryDoesNotContain(Player, YellowCard);
        InventoryUtil.AssertInventoryDoesNotContain(Player, RedSkull);
        InventoryUtil.AssertInventoryDoesNotContain(Player, BlueSkull);
        InventoryUtil.AssertInventoryDoesNotContain(Player, YellowSkull);
    }

    [Fact(DisplayName = "Cheat give all")]
    public void GiveAll()
    {
        World.CheatManager.ActivateCheat(Player, CheatType.GiveAll);
        InventoryUtil.AssertHasWeapon(Player, "Chainsaw");
        InventoryUtil.AssertHasWeapon(Player, "Shotgun");
        InventoryUtil.AssertHasWeapon(Player, "SuperShotgun");
        InventoryUtil.AssertHasWeapon(Player, "Chaingun");
        InventoryUtil.AssertHasWeapon(Player, "RocketLauncher");
        InventoryUtil.AssertHasWeapon(Player, "PlasmaRifle");
        InventoryUtil.AssertHasWeapon(Player, "BFG9000");
        Player.Armor.Should().Be(200);
        Player.ArmorProperties.Should().NotBeNull();
        Player.ArmorProperties!.Armor.SavePercent.Should().Be(50);
        InventoryUtil.AssertInventoryContains(Player, RedCard);
        InventoryUtil.AssertInventoryContains(Player, BlueCard);
        InventoryUtil.AssertInventoryContains(Player, YellowCard);
        InventoryUtil.AssertInventoryContains(Player, RedSkull);
        InventoryUtil.AssertInventoryContains(Player, BlueSkull);
        InventoryUtil.AssertInventoryContains(Player, YellowSkull);
    }

    [Fact(DisplayName = "Cheat give chainsaw")]
    public void GiveChainsaw()
    {
        InventoryUtil.AssertDoesNotHaveWeapon(Player, "Chainsaw");
        World.CheatManager.ActivateCheat(Player, CheatType.Chainsaw);
        InventoryUtil.AssertHasWeapon(Player, "Chainsaw");
    }

    [Fact(DisplayName = "Cheat behold")]
    public void Behold()
    {
        World.CheatManager.ActivateCheat(Player, CheatType.BeholdRadSuit);
        Player.Inventory.IsPowerupActive(PowerupType.IronFeet).Should().BeTrue();

        World.CheatManager.ActivateCheat(Player, CheatType.BeholdPartialInvisibility);
        Player.Inventory.IsPowerupActive(PowerupType.Invisibility).Should().BeTrue();

        World.CheatManager.ActivateCheat(Player, CheatType.BeholdInvulnerability);
        Player.Inventory.IsPowerupActive(PowerupType.Invulnerable).Should().BeTrue();

        World.CheatManager.ActivateCheat(Player, CheatType.BeholdComputerAreaMap);
        Player.Inventory.IsPowerupActive(PowerupType.ComputerAreaMap).Should().BeTrue();

        World.CheatManager.ActivateCheat(Player, CheatType.BeholdLightAmp);
        Player.Inventory.IsPowerupActive(PowerupType.LightAmp).Should().BeTrue();

        World.CheatManager.ActivateCheat(Player, CheatType.BeholdBerserk);
        GameActions.TickWorld(World, 1);
        Player.Inventory.IsPowerupActive(PowerupType.Strength).Should().BeTrue();

        World.CheatManager.ActivateCheat(Player, CheatType.Automap);
        InventoryUtil.AssertInventoryContains(Player, "Allmap");
    }

    [Fact(DisplayName = "Cheat automap")]
    public void Automap()
    {
        World.CheatManager.ActivateCheat(Player, CheatType.AutomapMode);
        AssertCheat(CheatType.AutoMapModeShowAllLines, true);
        World.CheatManager.ActivateCheat(Player, CheatType.AutomapMode);
        AssertCheat(CheatType.AutoMapModeShowAllLinesAndThings, true);
        World.CheatManager.ActivateCheat(Player, CheatType.AutomapMode);
        AssertCheat(CheatType.AutoMapModeShowAllLines, false);
        AssertCheat(CheatType.AutoMapModeShowAllLinesAndThings, false);
    }

    [Fact(DisplayName = "Cheat change level")]
    public void ChangeLevel()
    {
        bool changedLevel = false;
        int levelNumber = 0;
        World.LevelExit += World_LevelExit;

        World.CheatManager.ActivateCheat(Player, CheatType.ChangeLevel, 2);
        changedLevel.Should().BeTrue();
        levelNumber.Should().Be(2);

        void World_LevelExit(object? sender, LevelChangeEvent e)
        {
            changedLevel = true;
            levelNumber = e.LevelNumber;
            e.Cancel = true;
        }
    }

    [Fact(DisplayName = "Cheat exit")]
    public void Exit()
    {
        bool changedLevel = false;
        LevelChangeType? changeType = null;
        World.LevelExit += World_LevelExit;

        World.CheatManager.ActivateCheat(Player, CheatType.Exit);
        // This delays like normal exit
        GameActions.TickWorld(World, 35);
        changedLevel.Should().BeTrue();
        changeType.Should().Be(LevelChangeType.Next);

        void World_LevelExit(object? sender, LevelChangeEvent e)
        {
            changedLevel = true;
            changeType = e.ChangeType;
            e.Cancel = true;
        }
    }

    [Fact(DisplayName = "Cheat exit secret")]
    public void ExitSecret()
    {
        bool changedLevel = false;
        LevelChangeType? changeType = null;
        World.LevelExit += World_LevelExit;

        World.CheatManager.ActivateCheat(Player, CheatType.ExitSecret);
        // This delays like normal exit
        GameActions.TickWorld(World, 35);
        changedLevel.Should().BeTrue();
        changeType.Should().Be(LevelChangeType.SecretNext);

        void World_LevelExit(object? sender, LevelChangeEvent e)
        {
            changedLevel = true;
            changeType = e.ChangeType;
            e.Cancel = true;
        }
    }

    private void AssertCheat(CheatType type, bool active)
    {
        Player.Cheats.IsCheatActive(type).Should().Be(active);
    }
}
