using FluentAssertions;
using Helion.Geometry.Vectors;
using Helion.Tests.Unit.GameAction.Util;
using Helion.World;
using Helion.World.Entities.Inventories.Powerups;
using Helion.World.Entities.Players;
using System;
using System.Linq;
using Xunit;

namespace Helion.Tests.Unit.GameAction
{
    public partial class Inventory
    {
        [Fact(DisplayName = "Radiation suit")]
        public void RadiationSuit()
        {
            AssertNoPowerups(Player);
            AssertPowerUp(World, Player, PowerupType.IronFeet, "Radsuit", () => { }, color: true, alpha: 0.125f);
        }

        [Fact(DisplayName = "Invulnerability sphere")]
        public void InvulnerabilitySphere()
        {
            AssertNoPowerups(Player);
            AssertPowerUp(World, Player, PowerupType.Invulnerable, "InvulnerabilitySphere", () => { }, colorMap: true);
        }

        [Fact(DisplayName = "Soulsphere")]
        public void Soulsphere()
        {
            Player.Health.Should().Be(100);
            var soulsphere = GameActions.CreateEntity(World, "Soulsphere", Vec3D.Zero);
            World.PerformItemPickup(Player, soulsphere);
            Player.Health.Should().Be(200);
        }

        [Fact(DisplayName = "Megasphere")]
        public void Megasphere()
        {
            Player.Health.Should().Be(100);
            Player.Armor.Should().Be(0);
            var megasphere = GameActions.CreateEntity(World, "Megasphere", Vec3D.Zero);
            World.PerformItemPickup(Player, megasphere);
            Player.Health.Should().Be(200);
            Player.Armor.Should().Be(200);
        }

        [Fact(DisplayName = "BlurSphere")]
        public void BlurSphere()
        {
            Player.Flags.Shadow.Should().BeFalse();
            AssertNoPowerups(Player);
            AssertPowerUp(World, Player, PowerupType.Invisibility, "BlurSphere", () =>
            {
                Player.Flags.Shadow.Should().BeTrue();
            });
            Player.Flags.Shadow.Should().BeFalse();
        }

        [Fact(DisplayName = "Infrared")]
        public void Infrared()
        {
            AssertNoPowerups(Player);
            AssertPowerUp(World, Player, PowerupType.LightAmp, "Infrared", () => { });
        }

        [Fact(DisplayName = "Allmap")]
        public void Allmap()
        {
            Player.Inventory.IsPowerupActive(PowerupType.ComputerAreaMap).Should().BeFalse();
            var item = GameActions.CreateEntity(World, "Allmap", Vec3D.Zero);
            World.PerformItemPickup(Player, item);
            Player.Inventory.IsPowerupActive(PowerupType.ComputerAreaMap).Should().BeTrue();
        }

        [Fact(DisplayName = "Berserk")]
        public void Berserk()
        {
            Player.Health = 1;
            InventoryUtil.AssertWeapon(Player.Weapon, "Pistol");
            Player.PendingWeapon.Should().BeNull();
            AssertNoPowerups(Player);
            AssertPowerUp(World, Player, PowerupType.Strength, "Berserk", () => { }, color: true, effectTimeout: false, alpha: 0.5f);
            Player.Health.Should().Be(100);
            InventoryUtil.AssertWeapon(Player.PendingWeapon, "Fist");
            GameActions.TickWorld(World, 2100);
            Player.Inventory.PowerupEffectColor.Should().BeNull();
            // Powerup stays past color (effectively forever)
            Player.Inventory.IsPowerupActive(PowerupType.Strength);

            AssertPowerUp(World, Player, PowerupType.Strength, "Berserk", () => { }, color: true, effectTimeout: false, checkActive: false, alpha: 0.5f);
        }

        [Fact(DisplayName = "Stacked powerup effects")]
        public void StackedPowerups()
        {
            var radsuit = GameActions.CreateEntity(World, "Radsuit", Vec3D.Zero);
            var invul = GameActions.CreateEntity(World, "InvulnerabilitySphere", Vec3D.Zero);
            var berserk = GameActions.CreateEntity(World, "Berserk", Vec3D.Zero);

            World.PerformItemPickup(Player, radsuit);
            Player.Inventory.PowerupEffectColor.Should().Be(Player.Inventory.GetPowerup(PowerupType.IronFeet));

            World.PerformItemPickup(Player, berserk);
            Player.Inventory.PowerupEffectColor.Should().Be(Player.Inventory.GetPowerup(PowerupType.Strength));

            World.PerformItemPickup(Player, invul);
            Player.Inventory.PowerupEffectColorMap.Should().Be(Player.Inventory.GetPowerup(PowerupType.Invulnerable));
            // Color map overrides color, color is still set
            Player.Inventory.PowerupEffectColor.Should().Be(Player.Inventory.GetPowerup(PowerupType.Strength));

            Player.Inventory.RemovePowerup(Player.Inventory.GetPowerup(PowerupType.Invulnerable)!);
            Player.Inventory.PowerupEffectColorMap.Should().BeNull();
            Player.Inventory.PowerupEffectColor.Should().Be(Player.Inventory.GetPowerup(PowerupType.Strength));

            Player.Inventory.RemovePowerup(Player.Inventory.GetPowerup(PowerupType.Strength)!);
            Player.Inventory.PowerupEffectColor.Should().Be(Player.Inventory.GetPowerup(PowerupType.IronFeet));
        }

        [Fact(DisplayName = "Powerup resets time on new pickup")]
        public void PowerupReset()
        {
            var radsuit = GameActions.CreateEntity(World, "Radsuit", Vec3D.Zero);
            World.PerformItemPickup(Player, radsuit);
            var powerup = Player.Inventory.GetPowerup(PowerupType.IronFeet)!;
            powerup.Should().NotBeNull();
            powerup.DrawAlpha.Should().Be(0.125f);

            powerup.EffectTicks.Should().Be(35 * 60);
            powerup.Ticks.Should().Be(35 * 60);

            GameActions.TickWorld(World, 35 * 30);
            powerup.EffectTicks.Should().Be(35 * 30);
            powerup.Ticks.Should().Be(35 * 30);

            radsuit = GameActions.CreateEntity(World, "Radsuit", Vec3D.Zero);
            World.PerformItemPickup(Player, radsuit);
            powerup.EffectTicks.Should().Be(35 * 60);
            powerup.Ticks.Should().Be(35 * 60);
            powerup.DrawAlpha.Should().Be(0.125f);
        }

        [Fact(DisplayName = "Powerup flashes color")]
        public void PowerupColorFlash()
        {
            var radsuit = GameActions.CreateEntity(World, "Radsuit", Vec3D.Zero);
            World.PerformItemPickup(Player, radsuit);
            var powerup = Player.Inventory.GetPowerup(PowerupType.IronFeet)!;
            powerup.Should().NotBeNull();
            powerup.DrawPowerupEffect.Should().BeTrue();

            GameActions.TickWorld(World, 35 * 60 - 128);
            powerup.DrawPowerupEffect.Should().BeFalse();

            int drawTicks = 0;
            int noDrawTicks = 0;

            GameActions.TickWorld(World, 128, () =>
            {
                bool shouldDraw = (powerup.EffectTicks & 8) > 0;
                if (shouldDraw)
                    drawTicks++;
                else
                    noDrawTicks++;
                powerup.DrawPowerupEffect.Should().Be(shouldDraw);
            });

            drawTicks.Should().Be(64);
            noDrawTicks.Should().Be(64);
        }

        private static void AssertPowerUp(WorldBase world, Player player, PowerupType type, string powerupDefName, Action onGive,
            bool color = false, bool colorMap = false, bool effectTimeout = true, bool checkActive = true, float alpha = -1)
        {
            if (checkActive)
                player.Inventory.IsPowerupActive(type).Should().BeFalse();

            var item = GameActions.CreateEntity(world, powerupDefName, Vec3D.Zero);
            world.PerformItemPickup(player, item);
            player.Inventory.HasItem(powerupDefName).Should().BeTrue();
            player.Inventory.Powerups.Count.Should().Be(1);
            var powerup = player.Inventory.Powerups.First();
            powerup.PowerupType.Should().Be(type);
            player.Inventory.IsPowerupActive(type).Should().BeTrue();
            onGive();

            if (color)
                player.Inventory.PowerupEffectColor.Should().Be(powerup);
            else
                player.Inventory.PowerupEffectColor.Should().BeNull();

            if (alpha != -1)
                player.Inventory.PowerupEffectColor!.DrawAlpha.Should().Be(alpha);

            if (colorMap)
                player.Inventory.PowerupEffectColorMap.Should().Be(powerup);
            else
                player.Inventory.PowerupEffectColorMap.Should().BeNull();

            if (effectTimeout)
            {
                RunPowerup(world, player, powerup);

                // TODO
                //player.Inventory.HasItem(powerupDefName).Should().BeFalse();
                player.Inventory.PowerupEffectColor.Should().BeNull();
                player.Inventory.PowerupEffectColorMap.Should().BeNull();
            }
        }

        private static void AssertNoPowerups(Player player)
        {
            player.Inventory.Powerups.Count.Should().Be(0);
            player.Inventory.PowerupEffectColor.Should().BeNull();
            player.Inventory.PowerupEffectColorMap.Should().BeNull();
        }

        private static void RunPowerup(WorldBase world, Player player, IPowerup powerup)
        {
            GameActions.TickWorld(world, () => { return player.Inventory.IsPowerupActive(powerup.PowerupType); }, 
                () => { }, timeout: TimeSpan.FromMinutes(5));
        }
    }
}
