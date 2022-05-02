using FluentAssertions;
using Helion.Resources.IWad;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.World;
using Helion.World.Cheats;
using Helion.World.Entities.Inventories;
using Helion.World.Entities.Players;
using Helion.World.Impl.SinglePlayer;
using System;
using Xunit;

namespace Helion.Tests.Unit.GameAction
{
    [Collection("GameActions")]
    public class Inventory : IDisposable
    {
        private class WeaponData
        {
            public WeaponData(string name, string ammo, int amount, int use)
            {
                Name = name;
                Ammo = ammo;
                AmmoStartAmount = amount;
                AmmoUseAmount = use;
            }

            public string Name { get; set; }
            public string Ammo { get; set; }
            public int AmmoStartAmount { get; set; }
            public int AmmoUseAmount { get; set; }
        }

        private readonly WeaponData[] WeaponDataInfo = new[]
        {
            new WeaponData("Fist", string.Empty, 0, 0),
            new WeaponData("Chainsaw", string.Empty, 0, 0),
            new WeaponData("Pistol", "Clip", 20, 1),
            new WeaponData("Shotgun", "Shell", 8, 1),
            new WeaponData("Chaingun", "Clip", 20, 2),
            new WeaponData("SuperShotgun", "Shell", 8, 2),
            new WeaponData("RocketLauncher", "RocketAmmo", 2, 1),
            new WeaponData("PlasmaRifle", "Cell", 40, 1),
            new WeaponData("BFG9000", "Cell", 40, 40),

        };

        private readonly SinglePlayerWorld World;
        private Player Player => World.Player;

        public Inventory()
        {
            World = WorldAllocator.LoadMap("Resources/box.zip", "box.wad", "MAP01", GetType().Name, WorldInit, IWadType.Doom2);
        }

        public void Dispose()
        {
            Player.Inventory.Clear();
            Player.SetDefaultInventory();
            GameActions.TickWorld(World, () => { return Player.PendingWeapon != null || Player.WeaponOffset.Y != Constants.WeaponTop; }, () => { });
        }

        private void WorldInit(SinglePlayerWorld world)
        {
            world.CheatManager.ActivateCheat(world.Player, CheatType.God);
            // A_Raise will have been called once so it will be off by WeaponRaiseSpeed
            world.Player.WeaponOffset.Y.Should().Be(Constants.WeaponBottom - Constants.WeaponRaiseSpeed);
            AssertWeapon(world.Player.Weapon, "Fist");
            GameActions.TickWorld(world, 1);
            AssertWeapon(world.Player.AnimationWeapon, "Pistol");

            RunWeaponSwitch(world, world.Player, "Pistol");
            AssertWeapon(world.Player.Weapon, "Pistol");
        }

        [Fact(DisplayName = "Give weapon")]
        public void GiveWeapon()
        {
            AssertDoesNotHaveWeapon(Player, "Shotgun");
            Player.GiveItem(GameActions.GetEntityDefinition(World, "Shotgun"), null);
            AssertHasWeapon(Player, "Shotgun");
            Player.Inventory.Weapons.GetWeapon("Shotgun").Should().NotBeNull();
        }

        [Fact(DisplayName = "Remove weapon")]
        public void RemoveWeapon()
        {
            AssertHasWeapon(Player, "Pistol");
            var weapon = Player.Inventory.Weapons.GetWeapon("Pistol");
            weapon.Should().NotBeNull();
            Player.Inventory.Weapons.Remove("Pistol");
            AssertDoesNotHaveWeapon(Player, "Pistol");
        }

        [Fact(DisplayName = "Remove active weapon")]
        public void RemoveActiveWeapon()
        {
            Player.PendingWeapon.Should().BeNull();
            AssertHasWeapon(Player, "Pistol");
            AssertWeapon(Player.Weapon, "Pistol");
            var weapon = Player.Inventory.Weapons.GetWeapon("Pistol");
            weapon.Should().NotBeNull();
            Player.Inventory.Weapons.Remove("Pistol");
            AssertDoesNotHaveWeapon(Player, "Pistol");

            Player.PendingWeapon.Should().NotBeNull();
            AssertWeapon(Player.PendingWeapon, "Fist");
            RunWeaponSwitch(World, Player, "Fist");
        }

        [Fact(DisplayName = "Remove all weapons")]
        public void RemoveAllWeapons()
        {
            Player.PendingWeapon.Should().BeNull();
            AssertHasWeapon(Player, "Pistol");
            AssertWeapon(Player.Weapon, "Pistol");
            var weapon = Player.Inventory.Weapons.GetWeapon("Pistol");
            weapon.Should().NotBeNull();
            Player.Inventory.Clear();
            foreach (var weaponData in WeaponDataInfo)
                AssertDoesNotHaveWeapon(Player, weaponData.Name);

            // Weapon and AnimationWeapon are still the pistol until it lowers
            AssertWeapon(Player.Weapon, "Pistol");
            AssertWeapon(Player.AnimationWeapon, "Pistol");

            GameActions.TickWorld(World, () => { return Player.Weapon != null; }, () => { });

            Player.Weapon.Should().BeNull();
            Player.AnimationWeapon.Should().BeNull();
            Player.PendingWeapon.Should().BeNull();
        }

        [Fact(DisplayName = "Set amount")]
        public void SetAmount()
        {
            Player.Inventory.Amount("Clip").Should().Be(50);
            Player.Inventory.SetAmount(GameActions.GetEntityDefinition(World, "Clip"), 100).Should().BeTrue();
            Player.Inventory.Amount("Clip").Should().Be(100);

            Player.Inventory.SetAmount(GameActions.GetEntityDefinition(World, "Clip"), 25).Should().BeTrue();
            Player.Inventory.Amount("Clip").Should().Be(25);

            Player.Inventory.SetAmount(GameActions.GetEntityDefinition(World, "Clip"), 0).Should().BeTrue();
            Player.Inventory.Amount("Clip").Should().Be(0);
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

        [Fact(DisplayName = "Player automatically switches weapon on pickup")]
        public void AutoSwitch()
        {
            AssertDoesNotHaveWeapon(Player, "Shotgun");
            Player.Weapon.Should().NotBeNull();
            AssertWeapon(Player.Weapon, "Pistol");
            Player.GiveItem(GameActions.GetEntityDefinition(World, "Shotgun"), null);
            AssertHasWeapon(Player, "Shotgun");
            RunWeaponSwitch(World, Player, "Shotgun");
            AssertWeapon(Player.Weapon, "Shotgun");
        }

        [Fact(DisplayName = "Player does not automatically switch weapon pickup (already owned)")]
        public void NoAutoSwitch()
        {
            AssertDoesNotHaveWeapon(Player, "Shotgun");
            Player.Weapon.Should().NotBeNull();
            AssertWeapon(Player.Weapon, "Pistol");

            Player.GiveItem(GameActions.GetEntityDefinition(World, "Shotgun"), null);
            AssertHasWeapon(Player, "Shotgun");
            RunWeaponSwitch(World, Player, "Shotgun");
            AssertWeapon(Player.Weapon, "Shotgun");
            AssertAmount(Player, "Shell", 8);

            Player.GiveItem(GameActions.GetEntityDefinition(World, "Chaingun"), null);
            AssertHasWeapon(Player, "Chaingun");
            RunWeaponSwitch(World, Player, "Chaingun");
            AssertWeapon(Player.Weapon, "Chaingun");
            AssertAmount(Player, "Clip", 70);

            Player.GiveItem(GameActions.GetEntityDefinition(World, "Shotgun"), null);
            Player.PendingWeapon.Should().BeNull();
            AssertAmount(Player, "Shell", 16);
        }

        [Fact(DisplayName = "Default weapon amounts")]
        public void DefaultWeaponAmounts()
        {
            foreach (var weaponData in WeaponDataInfo)
            {
                Player.Inventory.Clear();
                Player.Inventory.Weapons.GetWeapons().Count.Should().Be(0);

                Player.Inventory.Weapons.OwnsWeapon(weaponData.Name).Should().BeFalse();
                Player.GiveItem(GameActions.GetEntityDefinition(World, weaponData.Name), null);
                Player.Inventory.Weapons.OwnsWeapon(weaponData.Name).Should().BeTrue();
                RunWeaponSwitch(World, Player, weaponData.Name);

                if (string.IsNullOrEmpty(weaponData.Ammo))
                    continue;

                AssertAmount(Player, weaponData.Ammo, weaponData.AmmoStartAmount);
            }
        }

        [Fact(DisplayName = "Player changes weapons when out of ammo")]
        public void OutOfAmmoSwitch()
        {
            foreach (var weaponData in WeaponDataInfo)
                Player.GiveItem(GameActions.GetEntityDefinition(World, weaponData.Name), null);

            for (int i = WeaponDataInfo.Length - 1; i > 2; i--)
            {
                var weaponData = WeaponDataInfo[i];
                var ammoDef = GameActions.GetEntityDefinition(World, weaponData.Ammo);

                Player.Inventory.SetAmount(ammoDef, 0);
                var weapon = Player.Inventory.Weapons.GetWeapon(weaponData.Name);
                weapon.Should().NotBeNull();
                Player.ChangeWeapon(weapon!);
                RunWeaponSwitch(World, Player, weaponData.Name);

                // Weapon fails to fire
                Player.FireWeapon().Should().BeFalse();

                string switchWeaponName = WeaponDataInfo[i - 1].Name;
                // Rocket launcher is skipped
                if (weaponData.Name.EqualsIgnoreCase("BFG9000") || weaponData.Name.EqualsIgnoreCase("PlasmaRifle"))
                    switchWeaponName = "SuperShotgun";

                Player.Inventory.SetAmount(ammoDef, 100);
                RunWeaponSwitch(World, Player, switchWeaponName);
                Player.Inventory.Weapons.Remove(weaponData.Name);
            }
        }

        [Fact(DisplayName = "Player only switches to rocket launcher when all other options are exhausted")]
        public void RocketLauncherAutoSwitch()
        {
            GiveAllWeaponsNoAmmo();
            Player.Inventory.SetAmount(GameActions.GetEntityDefinition(World, "RocketAmmo"), 1);
            var pistol = GetWeapon(Player, "Pistol");
            Player.ChangeWeapon(pistol);
            RunWeaponSwitch(World, Player, "Pistol");

            Player.FireWeapon().Should().BeFalse();
            RunWeaponSwitch(World, Player, "Chainsaw");

            // Chainsaw doesn't have wimpy weapon flag, so it will switch
            Player.Inventory.Weapons.Remove("Chainsaw");
            Player.ChangeWeapon(pistol);
            RunWeaponSwitch(World, Player, "Pistol");

            // Player will not change to fist because of wimpy weapon flag
            Player.FireWeapon().Should().BeFalse();
            RunWeaponSwitch(World, Player, "RocketLauncher");
        }

        [Fact(DisplayName = "Player switches from fist when picking up ammo")]
        public void WimpyWeaponSwitch()
        {
            GiveAllWeaponsNoAmmo();

            var fist = GetWeapon(Player, "Fist");
            var pistol = GetWeapon(Player, "Pistol");

            Player.ChangeWeapon(fist);
            RunWeaponSwitch(World, Player, "Fist");

            var shellDef = GameActions.GetEntityDefinition(World, "Shell");
            Player.GiveItem(shellDef, null);
            Player.PendingWeapon.Should().NotBeNull();
            AssertWeapon(Player.PendingWeapon, "SuperShotgun");

            Player.Inventory.SetAmount(shellDef, 0);
            Player.ChangeWeapon(pistol);
            RunWeaponSwitch(World, Player, "Pistol");

            Player.GiveItem(shellDef, null);
            Player.PendingWeapon.Should().NotBeNull();
            AssertWeapon(Player.PendingWeapon, "SuperShotgun");
        }

        [Fact(DisplayName = "Player uses ammo when firing weapon")]
        public void AmmoUseAmount()
        {
            GiveAllWeapons();

            foreach (var weaponData in WeaponDataInfo)
            {
                if (weaponData.AmmoUseAmount == 0)
                    continue;

                var ammoDef = GameActions.GetEntityDefinition(World, weaponData.Ammo);
                var weapon = GetWeapon(Player, weaponData.Name);
                Player.ChangeWeapon(weapon);
                RunWeaponSwitch(World, Player, weaponData.Name);

                Player.GiveItem(ammoDef, null);
                // Add one to prevent switching to next
                Player.Inventory.SetAmount(ammoDef, weaponData.AmmoUseAmount + 1);

                int startAmount = Player.Inventory.Amount(weaponData.Ammo);
                Player.FireWeapon().Should().BeTrue();
                RunWeaponFire(World, Player);
                Player.Inventory.Amount(weaponData.Ammo).Should().Be(startAmount - weaponData.AmmoUseAmount);

                if (weaponData.AmmoUseAmount <= 1)
                    continue;

                Player.GiveItem(ammoDef, null);
                Player.Inventory.SetAmount(ammoDef, weaponData.AmmoUseAmount - 1);

                // Chaingun calls A_FireCGun twice, should fire and use the single bullet
                if (weaponData.Name.EqualsIgnoreCase("Chaingun"))
                {
                    Player.FireWeapon().Should().BeTrue();
                    RunWeaponFire(World, Player);
                    Player.Inventory.Amount(weaponData.Ammo).Should().Be(0);
                }
                else
                {
                    Player.FireWeapon().Should().BeFalse();
                    Player.Inventory.Amount(weaponData.Ammo).Should().Be(weaponData.AmmoUseAmount - 1);
                }

                if (Player.PendingWeapon != null)
                    RunWeaponSwitch(World, Player, Player.PendingWeapon.Definition.Name);
            }
        }

        [Fact(DisplayName = "Weapon switching lower and raise times")]
        public void LowerAndRaiseTime()
        {
            Player.PendingWeapon.Should().BeNull();
            Player.WeaponOffset.Y.Should().Be(Constants.WeaponTop);
            Player.ChangeWeapon(GetWeapon(Player, "Fist"));
            int start = World.Gametick;
            GameActions.TickWorld(World, () => { return Player.WeaponOffset.Y != Constants.WeaponBottom - Constants.WeaponRaiseSpeed; }, () => { });
            int time = World.Gametick - start;
            time.Should().Be(14);

            start = World.Gametick; GameActions.TickWorld(World, () => { return Player.WeaponOffset.Y != Constants.WeaponTop; }, () => { });
            time = World.Gametick - start;
            time.Should().Be(16);
        }

        [Fact(DisplayName = "Weapon switch during lower does not reset pickup time")]
        public void QuickSwitch()
        {
            GiveAllWeapons();
            Player.PendingWeapon.Should().BeNull();
            Player.WeaponOffset.Y.Should().Be(Constants.WeaponTop);

            // When the player is actively in the weapon lower phase, switching weapons does not reset the total pickup time
            Player.ChangeWeapon(GetWeapon(Player, "Fist"));
            Player.PendingWeapon.Should().NotBeNull();
            int start = World.Gametick;
            GameActions.TickWorld(World, 8);
            Player.WeaponOffset.Y.Should().Be(86);

            Player.ChangeWeapon(GetWeapon(Player, "Shotgun"));
            GameActions.TickWorld(World, () => { return Player.WeaponOffset.Y != Constants.WeaponBottom - Constants.WeaponRaiseSpeed; }, () => { });
            int time = World.Gametick - start;
            time.Should().Be(14);
        }

        [Fact(DisplayName = "Can switch to new weapon after clear")]
        public void CanSwitchAfterClear()
        {
            Player.Inventory.Clear();
            GameActions.TickWorld(World, () => { return Player.Weapon != null || Player.PendingWeapon != null; }, () => { });

            Player.GiveItem(GameActions.GetEntityDefinition(World, "Chaingun"), null);
            GameActions.TickWorld(World, 16);
            Player.Weapon.Should().NotBeNull();
            Player.Weapon!.ReadyState.Should().BeTrue();
            Player.WeaponOffset.Y.Should().Be(Constants.WeaponTop);
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
                RunWeaponSwitch(World, Player, Player.PendingWeapon.Definition.Name);
        }

        private void GiveAllWeapons()
        {
            Player.Inventory.Clear();
            foreach (var weaponData in WeaponDataInfo)
                Player.GiveItem(GameActions.GetEntityDefinition(World, weaponData.Name), null);

            if (Player.PendingWeapon != null)
                RunWeaponSwitch(World, Player, Player.PendingWeapon.Definition.Name);
        }

        private static Weapon GetWeapon(Player player, string name)
        {
            var weapon = player.Inventory.Weapons.GetWeapon(name);
            weapon.Should().NotBeNull();
            return weapon!;
        }

        private static void AssertWeapon(Weapon? weapon, string name)
        {
            weapon.Should().NotBeNull();
            weapon!.Definition.Name.EqualsIgnoreCase(name).Should().BeTrue();
        }

        private static void AssertHasWeapon(Player player, string name) =>
            player.Inventory.Weapons.OwnsWeapon(name).Should().BeTrue();

        private static void AssertDoesNotHaveWeapon(Player player, string name) =>
            player.Inventory.Weapons.OwnsWeapon(name).Should().BeFalse();

        private static void AssertInventoryContains(Player player, string name) =>
            player.Inventory.HasItem(name).Should().BeTrue();

        private static void AssertInventoryDoesNotContain(Player player, string name) =>
            player.Inventory.HasItem(name).Should().BeFalse();

        private static void AssertAmount(Player player, string name, int amount) =>
            player.Inventory.Amount(name).Should().Be(amount);

        private static void RunWeaponSwitch(WorldBase world, Player player, string switchToName)
        {
            GameActions.TickWorld(world, () =>
            {
                return player.PendingWeapon != null || 
                    !player.Weapon!.Definition.Name.EqualsIgnoreCase(switchToName) ||  player.WeaponOffset.Y != Constants.WeaponTop;
            }, 
            () => { });
        }

        private static void RunWeaponFire(WorldBase world, Player player)
        {
            player.Weapon.Should().NotBeNull();
            world.Tick();
            GameActions.TickWorld(world, () => { return !player.Weapon!.ReadyToFire; },
                () => { });
        }
    }
}
