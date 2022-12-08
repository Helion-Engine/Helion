using FluentAssertions;
using Helion.Resources.IWad;
using Helion.Tests.Unit.GameAction.Util;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.World;
using Helion.World.Cheats;
using Helion.World.Entities.Inventories;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using System;
using System.Linq;
using Xunit;

namespace Helion.Tests.Unit.GameAction
{
    [Collection("GameActions")]
    public partial class Inventory : IDisposable
    {
        private SinglePlayerWorld World;
        private Player Player => World.Player;

        public Inventory()
        {
            World = WorldAllocator.LoadMap("Resources/box.zip", "box.wad", "MAP01", GetType().Name, WorldInit, IWadType.Doom2);
            World.Player.TickCommand = new TestTickCommand();
        }

        public void Dispose()
        {
            InventoryUtil.Reset(World, Player);
        }

        private void WorldInit(SinglePlayerWorld world)
        {
            world.CheatManager.ActivateCheat(world.Player, CheatType.God);
            world.Player.WeaponOffset.Y.Should().Be(InventoryUtil.WeaponBottomRaise);
            InventoryUtil.AssertWeapon(world.Player.Weapon, "Pistol");
            GameActions.TickWorld(world, 1);
            InventoryUtil.AssertWeapon(world.Player.AnimationWeapon, "Pistol");

            InventoryUtil.RunWeaponSwitch(world, world.Player, "Pistol");
            InventoryUtil.AssertWeapon(world.Player.Weapon, "Pistol");
        }

        [Fact(DisplayName = "Set amount")]
        public void SetAmount()
        {
            InventoryUtil.AssertInventoryContains(Player, "Clip");
            Player.Inventory.Amount("Clip").Should().Be(50);
            Player.Inventory.SetAmount(GameActions.GetEntityDefinition(World, "Clip"), 100).Should().BeTrue();
            Player.Inventory.Amount("Clip").Should().Be(100);

            Player.Inventory.SetAmount(GameActions.GetEntityDefinition(World, "Clip"), 25).Should().BeTrue();
            Player.Inventory.Amount("Clip").Should().Be(25);

            Player.Inventory.SetAmount(GameActions.GetEntityDefinition(World, "Clip"), 0).Should().BeTrue();
            Player.Inventory.Amount("Clip").Should().Be(0);

            Player.Inventory.Remove("Clip", 100);
            InventoryUtil.AssertInventoryDoesNotContain(Player, "Clip");
        }

        [Fact(DisplayName = "Set negative amount")]
        public void SetAmountNegative()
        {
            Player.Inventory.Amount("Clip").Should().Be(50);
            Player.Inventory.SetAmount(GameActions.GetEntityDefinition(World, "Clip"), -1).Should().BeFalse();
            Player.Inventory.Amount("Clip").Should().Be(50);
        }

        [Fact(DisplayName = "Set amount not owned")]
        public void SetAmountNotOwned()
        {
            Player.Inventory.HasItem("Shell").Should().BeFalse();
            Player.Inventory.Amount("Shell").Should().Be(0);
            Player.Inventory.SetAmount(GameActions.GetEntityDefinition(World, "Shell"), 100).Should().BeFalse();
            Player.Inventory.HasItem("Shell").Should().BeFalse();
            Player.Inventory.Amount("Shell").Should().Be(0);
        }

        [Fact(DisplayName = "Player inventory carries over to next level")]
        public void NextLevelCarryOver()
        {
            Player existingPlayer = Player;
            existingPlayer.GiveItem(GameActions.GetEntityDefinition(World, "RocketLauncher"), null);
            existingPlayer.GiveItem(GameActions.GetEntityDefinition(World, "Chaingun"), null);
            existingPlayer.GiveItem(GameActions.GetEntityDefinition(World, "RedCard"), null);
            InventoryUtil.RunWeaponSwitch(World, existingPlayer, "Chaingun");
            InventoryUtil.AssertWeapon(existingPlayer.Weapon, "Chaingun");
            existingPlayer.Inventory.HasItem("RedCard").Should().BeTrue();

            World = WorldAllocator.LoadMap("Resources/box.zip", "box.wad", "MAP01", Guid.NewGuid().ToString(), (SinglePlayerWorld world) => { }, IWadType.Doom2, 
                existingPlayer: existingPlayer);
            InventoryUtil.AssertWeapon(Player.Weapon, "Chaingun");
            Player.WeaponOffset.Y.Should().Be(InventoryUtil.WeaponBottomRaise);
            InventoryUtil.RunWeaponSwitch(World, Player, "Chaingun");

            var existingWeapons = existingPlayer.Inventory.Weapons.GetWeapons();
            var carryWeapons = Player.Inventory.Weapons.GetWeapons();

            existingWeapons.Count.Should().Be(carryWeapons.Count);
            foreach (var existingWeapon in existingWeapons)
                carryWeapons.FirstOrDefault(x => x.Definition.Name.EqualsIgnoreCase(existingWeapon.Definition.Name)).Should().NotBeNull();

            var existingItems = existingPlayer.Inventory.GetInventoryItems().Except(existingPlayer.Inventory.GetKeys()).ToList();
            var carryItems = Player.Inventory.GetInventoryItems();

            existingItems.Count.Should().Be(carryItems.Count);
            foreach (var existingItem in existingItems)
            {
                carryItems.FirstOrDefault(x => x.Definition.Name.EqualsIgnoreCase(existingItem.Definition.Name)).Should().NotBeNull();
                existingPlayer.Inventory.Amount(existingItem.Definition.Name).Should().Be(Player.Inventory.Amount(existingItem.Definition.Name));
            }
        }

        private void GiveAllWeaponsNoAmmo()
        {
            Player.Inventory.Clear();
            foreach (var weaponData in WeaponDataInfo)
            {
                Player.GiveItem(GameActions.GetEntityDefinition(World, weaponData.Name), null);
                if (string.IsNullOrEmpty(weaponData.Ammo))
                    continue;
                Player.Inventory.SetAmount(GameActions.GetEntityDefinition(World, weaponData.Ammo), 0);
            }

            if (Player.PendingWeapon != null)
                InventoryUtil.RunWeaponSwitch(World, Player, Player.PendingWeapon.Definition.Name);
        }

        private void GiveAllWeapons()
        {
            Player.Inventory.Clear();
            foreach (var weaponData in WeaponDataInfo)
                Player.GiveItem(GameActions.GetEntityDefinition(World, weaponData.Name), null);

            if (Player.PendingWeapon != null)
                InventoryUtil.RunWeaponSwitch(World, Player, Player.PendingWeapon.Definition.Name);
        }
    }
}
