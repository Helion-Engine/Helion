using FluentAssertions;
using Helion.Util;
using Helion.Util.Extensions;
using Helion.World.Entities.Players;
using System;
using Xunit;

namespace Helion.Tests.Unit.GameAction
{
    public partial class Inventory
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

        // A_Raise will have been called once so it will be off by WeaponRaiseSpeed
        private const double WeaponBottom = Constants.WeaponBottom - Constants.WeaponRaiseSpeed;

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
            GameActions.TickWorld(World, () => { return Player.WeaponOffset.Y != WeaponBottom; }, () => { });
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
            GameActions.TickWorld(World, () => { return Player.WeaponOffset.Y != WeaponBottom; }, () => { });
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

        [Fact(DisplayName = "Weapon pistol fire")]
        public void WeaponPistolFire()
        {
            Player.Inventory.Weapons.OwnsWeapon("Pistol").Should().BeTrue();
            Player.Weapon.Should().NotBeNull();
            AssertWeapon(Player.Weapon, "Pistol");
            Player.Inventory.Amount("Clip").Should().Be(50);
            
            var weapon = Player.Weapon!;
            weapon.ReadyState.Should().BeTrue();
            weapon.ReadyToFire.Should().BeTrue();
            weapon.FlashState.Frame.IsNullFrame.Should().BeTrue();
            weapon.FrameState.Frame.ActionFunction.Should().NotBeNull();
            weapon.FrameState.Frame.ActionFunction!.Method.Name.Should().Be("A_WeaponReady");

            // Doesn't have TickCommand.Attack
            Player.CanFireWeapon().Should().BeFalse();
            Player.TickCommand.Add(TickCommands.Attack);
            Player.CanFireWeapon().Should().BeTrue();

            Player.FireWeapon().Should().BeTrue();
            // Still true, world hasn't ticked yet
            weapon.ReadyToFire.Should().BeTrue();
            Player.CanFireWeapon().Should().BeTrue();
            weapon.FlashState.Frame.IsNullFrame.Should().BeTrue();
            World.Tick();
            weapon.ReadyToFire.Should().BeFalse();
            Player.CanFireWeapon().Should().BeFalse();
            weapon.FrameState.Frame.ActionFunction.Should().BeNull();

            bool flashFrame = false;
            bool fireFunction = false;

            RunWeaponFire(World, Player, () =>
            {
                if (!weapon.FlashState.Frame.IsNullFrame)
                    flashFrame = true;
                if (weapon.FrameState.Frame.ActionFunction != null && weapon.FrameState.Frame.ActionFunction.Method.Name.Equals("A_FirePistol"))
                    fireFunction = true;
            });

            flashFrame.Should().BeTrue();
            fireFunction.Should().BeTrue();
            weapon.ReadyToFire.Should().BeTrue();
            weapon.FrameState.Frame.ActionFunction.Should().NotBeNull();
            weapon.FrameState.Frame.ActionFunction!.Method.Name.Should().Be("A_WeaponReady");
            Player.Inventory.Amount("Clip").Should().Be(49);
        }

        [Fact(DisplayName = "Weapon pistol refire")]
        public void WeaponRefire()
        {
            Player.Inventory.Weapons.OwnsWeapon("Pistol").Should().BeTrue();
            Player.Weapon.Should().NotBeNull();
            Player.Refire.Should().BeFalse();
            AssertWeapon(Player.Weapon, "Pistol");
            Player.Inventory.Amount("Clip").Should().Be(50);
            var weapon = Player.Weapon!;

            bool flash = false;
            bool fire = false;

            Player.TickCommand.Add(TickCommands.Attack);
            Player.FireWeapon().Should().BeTrue();

            RunWeaponUntilRefire(Player, () =>
            {
                if (!weapon.FlashState.Frame.IsNullFrame)
                    flash = true;
                if (weapon.FrameState.Frame.ActionFunction != null && weapon.FrameState.Frame.ActionFunction.Method.Name.Equals("A_FirePistol"))
                    fire = true;
            });

            flash.Should().BeTrue();
            fire.Should().BeTrue();
            Player.Refire.Should().BeFalse();
            Player.Inventory.Amount("Clip").Should().Be(49);

            flash = false;
            fire = false;
            RunWeaponUntilNotRefire(Player, () => { });

            Player.TickCommand.Add(TickCommands.Attack);

            RunWeaponUntilRefire(Player, () =>
            {
                if (!weapon.FlashState.Frame.IsNullFrame)
                    flash = true;
                if (weapon.FrameState.Frame.ActionFunction != null && weapon.FrameState.Frame.ActionFunction.Method.Name.Equals("A_FirePistol"))
                    fire = true;
            });

            flash.Should().BeTrue();
            fire.Should().BeTrue();
            Player.Refire.Should().BeFalse();
            Player.Inventory.Amount("Clip").Should().Be(48);

            RunWeaponUntilNotRefire(Player, () => { });

            Player.Refire.Should().BeFalse();
            weapon.ReadyToFire.Should().BeTrue();
            weapon.FrameState.Frame.ActionFunction.Should().NotBeNull();
            weapon.FrameState.Frame.ActionFunction!.Method.Name.Should().Be("A_WeaponReady");
        }

        [Fact(DisplayName = "Refire pistol until out of ammo")]
        public void RefireAllAmmo()
        {
            Player.Inventory.Weapons.OwnsWeapon("Pistol").Should().BeTrue();
            Player.Weapon.Should().NotBeNull();
            Player.Refire.Should().BeFalse();
            AssertWeapon(Player.Weapon, "Pistol");
            Player.Inventory.Amount("Clip").Should().Be(50);
            var weapon = Player.Weapon!;

            Player.TickCommand.Add(TickCommands.Attack);
            Player.FireWeapon().Should().BeTrue();

            bool hitRefire = false;
            int fireCount = 0;

            RunWeaponUntilRefire(Player, () =>
            {
                Player.TickCommand.Add(TickCommands.Attack);
                if (Player.Refire)
                    hitRefire = true;
                if (weapon.FrameState.Frame.ActionFunction != null && weapon.FrameState.Frame.ActionFunction.Method.Name.Equals("A_FirePistol") && 
                    weapon.FrameState.CurrentTick == 6)
                    fireCount++;
            });

            hitRefire.Should().BeTrue();
            fireCount.Should().Be(50);

            Player.Inventory.Amount("Clip").Should().Be(0);
            AssertWeapon(Player.PendingWeapon, "Fist");
        }

        private void RunWeaponUntilRefire(Player player, Action onTick)
        {
            player.Weapon.Should().NotBeNull();
            var weapon = Player.Weapon!;
            GameActions.TickWorld(World, () =>
            {
                return weapon.FrameState.Frame.ActionFunction == null || !weapon.FrameState.Frame.ActionFunction.Method.Name.Equals("A_ReFire");
            }, onTick);
        }

        private void RunWeaponUntilNotRefire(Player player, Action onTick)
        {
            player.Weapon.Should().NotBeNull();
            var weapon = Player.Weapon!;
            GameActions.TickWorld(World, () =>
            {
                if (weapon.FrameState.Frame.ActionFunction == null)
                    return false;
                if (!weapon.FrameState.Frame.ActionFunction.Method.Name.Equals("A_ReFire"))
                    return false;
                return true;
            }, onTick);
        }
    }
}
