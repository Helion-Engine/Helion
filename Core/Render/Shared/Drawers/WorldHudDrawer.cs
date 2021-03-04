using System;
using System.Collections.Generic;
using System.Drawing;
using Helion.Graphics.String;
using Helion.Render.Commands;
using Helion.Render.Commands.Alignment;
using Helion.Render.OpenGL.Util;
using Helion.Render.Shared.Drawers.Helper;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Consoles;
using Helion.Util.Extensions;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;
using Helion.Util.Time;
using Helion.World;
using Helion.World.Entities.Definition.Properties;
using Helion.World.Entities.Definition.States;
using Helion.World.Entities.Inventories;
using Helion.World.Entities.Inventories.Powerups;
using Helion.World.Entities.Players;
using Font = Helion.Graphics.Fonts.Font;

namespace Helion.Render.Shared.Drawers
{
    public class WorldHudDrawer
    {
        private const int MaxHudMessages = 4;
        private const int LeftOffset = 1;
        private const int TopOffset = 1;
        private const int MessageSpacing = 1;
        private const int FpsMessageSpacing = 2;
        private const int CrosshairLength = 10;
        private const int CrosshairWidth = 4;
        private const int CrosshairHalfWidth = CrosshairWidth / 2;
        private const int FlashPickupTickDuration = 6;
        private const int Padding = 4;
        private const int FullHudFaceX = 149;
        private const int FullHudFaceY = 170;
        private const long MaxVisibleTimeNanos = 4 * 1000L * 1000L * 1000L;
        private const long FadingNanoSpan = 350L * 1000L * 1000L;
        private const long OpaqueNanoRange = MaxVisibleTimeNanos - FadingNanoSpan;
        private static readonly Color PickupColor = Color.FromArgb(255, 255, 128);
        private static readonly Color DamageColor = Color.FromArgb(255, 0, 0);
        private static readonly CIString SmallHudFont = "SmallFont";
        private static readonly CIString LargeHudFont = "LargeHudFont";
        private static readonly CIString ConsoleFont = "Console";

        private readonly ArchiveCollection m_archiveCollection;

        private bool m_maxHealthAreaInit;
        private Dimension m_maxHealthArea;

        public WorldHudDrawer(ArchiveCollection archiveCollection)
        {
            m_archiveCollection = archiveCollection;
        }

        public void Draw(Player player, WorldBase world, float tickFraction, HelionConsole console,
            Dimension viewport, Config config, RenderCommands cmd)
        {
            DrawHelper draw = new(cmd);
            Font? smallFont = m_archiveCollection.GetFont(SmallHudFont);
            Font? largeFont = m_archiveCollection.GetFont(LargeHudFont);
            Font? consoleFont = m_archiveCollection.GetFont(ConsoleFont);

            cmd.ClearDepth();

            DrawFPS(cmd.Config, viewport, cmd.FpsTracker, draw, consoleFont, out int topRightY);
            DrawHud(topRightY, player, world, tickFraction, viewport, config, largeFont, draw);
            DrawPowerupEffect(player, viewport, draw);
            DrawPickupFlash(player, world, viewport, draw);
            DrawDamage(player, world, viewport, draw);
            DrawRecentConsoleMessages(world, console, smallFont, draw);
        }

        private void DrawHud(int topRightY, Player player, WorldBase world, float tickFraction,
            Dimension viewport, Config config, Font? largeFont, DrawHelper draw)
        {
            if (player.AnimationWeapon != null)
            {
                DrawHudWeapon(player, tickFraction, player.AnimationWeapon.FrameState, viewport, draw);
                if (player.AnimationWeapon.FlashState.Frame.BranchType != Resources.Definitions.Decorate.States.ActorStateBranch.Stop)
                    DrawHudWeapon(player, tickFraction, player.AnimationWeapon.FlashState, viewport, draw);
            }

            DrawHudCrosshair(viewport, draw);

            // TODO: This should be at the top, rendering order is reversed somehow (check impl)
            if (config.Hud.FullStatusBar)
                DrawFullStatusBar(player, largeFont, draw);
            else
                DrawMinimalStatusBar(player, topRightY, largeFont, draw);
        }

        private void DrawFullStatusBar(Player player, Font? largeFont, DrawHelper draw)
        {
            draw.AtResolution(DoomHudHelper.DoomResolutionInfo, () =>
            {
                draw.Image("STBAR", window: Align.BottomLeft, image: Align.BottomLeft);

                DrawFullHudHealthArmorAmmo(player, largeFont, draw);
                DrawFullHudWeaponSlots(player, draw);
                DrawFace(player, draw, FullHudFaceX, FullHudFaceY);
                DrawFullHudKeys(player, draw);
                DrawFullTotalAmmo(player, draw);
            });
        }

        private static void DrawFullHudHealthArmorAmmo(Player player, Font? largeFont, DrawHelper draw)
        {
            const int OffsetY = 171;
            const int FontSize = 15;

            if (largeFont == null)
                return;

            if (player.Weapon != null && !player.Weapon.Definition.Properties.Weapons.AmmoType.Empty())
            {
                int ammoAmount = player.Inventory.Amount(player.Weapon.Definition.Properties.Weapons.AmmoType);
                string ammo = Math.Clamp(ammoAmount, 0, 999).ToString();
                draw.Text(Color.Red, ammo, largeFont, FontSize, 43, OffsetY, TextAlign.Right, textbox: Align.TopRight);
            }

            string health = $"{Math.Clamp(player.Health, 0, 999)}%";
            draw.Text(Color.Red, health, largeFont, FontSize, 102, OffsetY, TextAlign.Right, textbox: Align.TopRight);

            string armor = $"{Math.Clamp(player.Armor, 0, 999)}%";
            draw.Text(Color.Red, armor, largeFont, FontSize, 233, OffsetY, TextAlign.Right, textbox: Align.TopRight);
        }

        private static void DrawFullHudWeaponSlots(Player player, DrawHelper draw)
        {
            draw.Image("STARMS", 104, 0, both: Align.BottomLeft);

            for (int slot = 2; slot <= 7; slot++)
                DrawWeaponNumber(player, slot, draw);
        }

        private static void DrawWeaponNumber(Player player, int slot, DrawHelper draw)
        {
            Weapon? weapon = player.Inventory.Weapons.GetWeapon(player, slot, 0);
            if (slot == 3 && weapon == null)
                weapon = player.Inventory.Weapons.GetWeapon(player, slot, 1);

            string numberImage = (weapon != null ? "STYSNUM" : "STGNUM") + slot;

            (int x, int y) = slot switch
            {
                2 => (111, 172),
                3 => (123, 172),
                4 => (135, 172),
                5 => (111, 182),
                6 => (123, 182),
                7 => (135, 182),
                _ => throw new Exception($"Bad slot index: {slot}")
            };

            draw.Image(numberImage, x, y);
        }

        private static void DrawFace(Player player, DrawHelper draw, int x, int y, 
            Align? both = null)
        {
            draw.Image(player.StatusBar.GetFacePatch(), x, y, both: both);
        }

        private static void DrawFullHudKeys(Player player, DrawHelper draw)
        {
            const int x = 239;

            foreach (InventoryItem key in player.Inventory.GetKeys())
            {
                DrawKeyIfOwned(key, "BlueSkull", "BlueCard", x, 171, draw);
                DrawKeyIfOwned(key, "YellowSkull", "YellowCard", x, 181, draw);
                DrawKeyIfOwned(key, "RedSkull", "RedCard", x, 191, draw);
            }
        }

        private static void DrawKeyIfOwned(InventoryItem key, string skullKeyName, string keyName, int x, int y,
            DrawHelper draw)
        {
            string imageName = key.Definition.Properties.Inventory.Icon;

            foreach (string name in new[] { skullKeyName, keyName })
            {
                if (key.Definition.Name == name && draw.ImageExists(imageName))
                {
                    draw.Image(imageName, x, y);
                    break;
                }
            }
        }

        private  void DrawFullTotalAmmo(Player player, DrawHelper draw)
        {
            Font? yellowFont = m_archiveCollection.GetFont("HudYellowNumbers");
            if (yellowFont == null)
                return;

            bool backpack = player.Inventory.HasItemOfClass(Inventory.BackPackBaseClassName);

            DrawFullTotalAmmoText("Clip", backpack ? 400 : 200, 173);
            DrawFullTotalAmmoText("Shell", backpack ? 100 : 50, 179);
            DrawFullTotalAmmoText("RocketAmmo", backpack ? 100 : 50, 185);
            DrawFullTotalAmmoText("Cell", backpack ? 600 : 300, 191);

            void DrawFullTotalAmmoText(string ammoName, int maxAmmo, int y)
            {
                const int FontSize = 6;

                int ammo = player.Inventory.Amount(ammoName);
                draw.Text(Color.White, ammo.ToString(), yellowFont, FontSize, 287, y, textbox: Align.TopRight);
                draw.Text(Color.White, maxAmmo.ToString(), yellowFont, FontSize, 315, y, textbox: Align.TopRight);
            }
        }

        private void DrawMinimalStatusBar(Player player, int topRightY, Font? largeFont, DrawHelper draw)
        {
            DrawMinimalHudHealthAndArmor(player, largeFont, draw);
            DrawMinimalHudKeys(topRightY, player, draw);
            DrawMinimalHudAmmo(player, largeFont, draw);
        }

        private void DrawMinimalHudHealthAndArmor(Player player, Font? largeFont, DrawHelper draw)
        {
            if (largeFont == null)
                return;

            // We will draw the medkit slightly higher so it looks like it
            // aligns with the font.
            int x = Padding;
            int y = -Padding;

            draw.Image("MEDIA0", x, y, out Dimension medkitArea, both: Align.BottomLeft);

            // We will draw the health numbers with the same height as the
            // medkit image. However if someone ever replaces it, we probably
            // want to draw it at the height of that image. We also don't want
            // to have a missing or small image screw up the height so we'll
            // clamp it to be at least 16.
            int fontHeight = Math.Max(16, medkitArea.Height);
            int health = Math.Max(0, player.Health);
            draw.Text(Color.Red, health.ToString(), largeFont, fontHeight, out Dimension healthArea,
                x + medkitArea.Width + Padding, y, TextAlign.Left, both: Align.BottomLeft);

            Dimension maxHealthArea = GetMaxHealthArea(largeFont, draw, fontHeight);
            DrawFace(player, draw, x + medkitArea.Width + maxHealthArea.Width + (Padding * 3), y, both: Align.BottomLeft);

            if (player.Armor > 0)
            {
                y -= healthArea.Height + Padding;

                EntityProperties? armorProp = player.ArmorProperties;
                if (armorProp != null && draw.ImageExists(armorProp.Inventory.Icon))
                {
                    draw.Image(armorProp.Inventory.Icon, x, y, out Dimension armorArea, both: Align.BottomLeft);
                    x += armorArea.Width + Padding;
                }

                draw.Text(Color.Red, player.Armor.ToString(), largeFont, fontHeight, out Dimension armorTextArea,
                    x, y, TextAlign.Left, both: Align.BottomLeft);
            }
        }

        private Dimension GetMaxHealthArea(Font font, DrawHelper draw, int fontHeight)
        {
            if (m_maxHealthAreaInit)
                return m_maxHealthArea;

            // This is assuming vanilla doom maxing out with 3 numbers
            // Decorate can change this and will need to be accounted for later
            m_maxHealthArea = draw.TextDrawArea("000", font, fontHeight);
            m_maxHealthAreaInit = true;
            return m_maxHealthArea;
        }

        private static void DrawMinimalHudKeys(int y, Player player, DrawHelper draw)
        {
            List<InventoryItem> keys = player.Inventory.GetKeys();
            y += Padding;

            foreach (InventoryItem key in keys)
            {
                string icon = key.Definition.Properties.Inventory.Icon;
                if (!draw.ImageExists(icon))
                    continue;

                // If we want to scale these, use `draw.DrawInfoProvider.GetImageDimension(icon)`
                // and use that width/height below but scaled.
                draw.Image(icon, -Padding, y, out Dimension drawArea, both: Align.TopRight);
                y += drawArea.Height + Padding;
            }
        }

        private static void DrawHudWeapon(Player player, float tickFraction, FrameState frameState, Dimension viewport,
            DrawHelper draw)
        {
            int lightLevel = frameState.Frame.Properties.Bright || player.DrawFullBright() ? 255 :
                (int)(GLHelper.DoomLightLevelToColor(player.Sector.LightLevel + (player.ExtraLight * Constants.ExtraLightFactor) + Constants.ExtraLightFactor) * 255);

            Color lightLevelColor = Color.FromArgb(lightLevel, lightLevel, lightLevel);
            string sprite = frameState.Frame.Sprite + (char)(frameState.Frame.Frame + 'A') + "0";

            if (draw.ImageExists(sprite))
            {
                (int width, int height) = draw.DrawInfoProvider.GetImageDimension(sprite);
                Vec2I offset = draw.DrawInfoProvider.GetImageOffset(sprite);
                Vec2D interpolateOffset = player.PrevWeaponOffset.Interpolate(player.WeaponOffset, tickFraction);
                Vec2I weaponOffset = DoomHudHelper.ScaleWorldOffset(viewport, interpolateOffset);
                DoomHudHelper.ScaleImageDimensions(viewport, ref width, ref height);
                DoomHudHelper.ScaleImageOffset(viewport, ref offset.X, ref offset.Y);

                float alpha = 1.0f;
                IPowerup? powerup = player.Inventory.GetPowerup(PowerupType.Invisibility);
                if (powerup != null && powerup.DrawPowerupEffect)
                    alpha = 0.3f;

                bool drawInvul = player.DrawInvulnerableColorMap();

                // Translate doom image offset to OpenGL coordinates
                int x = (offset.X / 2) - (width / 2) + weaponOffset.X;
                int y = -offset.Y - height + weaponOffset.Y;
                draw.Image(sprite, x, y, width, height, color: lightLevelColor, alpha: alpha, drawInvul: drawInvul);
            }
        }

        private static void DrawMinimalHudAmmo(Player player, Font? largeFont, DrawHelper helper)
        {
            if (largeFont == null || player.Weapon == null || player.Weapon.Definition.Properties.Weapons.AmmoType.Length == 0)
                return;

            int x = -Padding;
            int y = -Padding;

            int ammo = player.Inventory.Amount(player.Weapon.Definition.Properties.Weapons.AmmoType);
            helper.Text(Color.Red, ammo.ToString(), largeFont, 19, out Dimension textRect,
                x, y, both: Align.BottomRight);

            x = x - textRect.Width - Padding;
            if (player.Weapon.AmmoSprite.Length > 0 && helper.ImageExists(player.Weapon.AmmoSprite))
            {
                Dimension dimension = helper.DrawInfoProvider.GetImageDimension(player.Weapon.AmmoSprite);
                x -= dimension.Width;
                helper.Image(player.Weapon.AmmoSprite, x, y, both: Align.BottomRight);
            }
        }

        private static void DrawHudCrosshair(Dimension viewport, DrawHelper helper)
        {
            Vec2I center = viewport.ToVector() / 2;
            Vec2I horizontalStart = center - new Vec2I(CrosshairLength, CrosshairHalfWidth);
            Vec2I verticalStart = center - new Vec2I(CrosshairHalfWidth, CrosshairLength);

            helper.FillRect(horizontalStart.X, horizontalStart.Y, CrosshairLength * 2, CrosshairHalfWidth * 2, Color.LawnGreen);
            helper.FillRect(verticalStart.X, verticalStart.Y, CrosshairHalfWidth * 2, CrosshairLength * 2, Color.LawnGreen);
        }

        private static void DrawPickupFlash(Player player, WorldBase world, Dimension viewport, DrawHelper helper)
        {
            int ticksSincePickup = world.Gametick - player.LastPickupGametick;
            if (ticksSincePickup < FlashPickupTickDuration)
                helper.FillRect(0, 0, viewport.Width, viewport.Height, PickupColor, 0.15f);
        }

        private static void DrawPowerupEffect(Player player, Dimension viewport, DrawHelper helper)
        {
            if (player.Inventory.PowerupEffectColor?.DrawColor == null || !player.Inventory.PowerupEffectColor.DrawPowerupEffect)
                return;

            helper.FillRect(0, 0, viewport.Width, viewport.Height, player.Inventory.PowerupEffectColor.DrawColor.Value,
                player.Inventory.PowerupEffectColor.DrawAlpha);
        }

        private static void DrawDamage(Player player, WorldBase world, Dimension viewport, DrawHelper helper)
        {
            if (player.DamageCount > 0)
                helper.FillRect(0, 0, viewport.Width, viewport.Height, DamageColor, player.DamageCount * 0.01f);
        }

        private static void DrawRecentConsoleMessages(WorldBase world, HelionConsole console, Font? smallFont,
            DrawHelper helper)
        {
            if (smallFont == null)
                return;

            long currentNanos = Ticker.NanoTime();
            int messagesDrawn = 0;
            int offsetY = TopOffset;

            // We want to draw the ones that are less recent at the top first,
            // so when we iterate and see most recent to least recent, pushing
            // most recent onto the stack means when we iterate over this we
            // will draw the later ones at the top. Otherwise if we were to do
            // forward iteration without the stack, then they get drawn in the
            // reverse order and fading begins at the wrong end.
            Stack<(ColoredString message, float alpha)> messages = new();
            foreach (ConsoleMessage msg in console.Messages)
            {
                if (messagesDrawn >= MaxHudMessages || MessageTooOldToDraw(msg, world, console))
                    break;

                long timeSinceMessage = currentNanos - msg.TimeNanos;
                if (timeSinceMessage > MaxVisibleTimeNanos)
                    break;

                messages.Push((msg.Message, CalculateFade(timeSinceMessage)));
                messagesDrawn++;
            }

            foreach ((ColoredString message, float alpha) in messages)
            {
                helper.Text(message, smallFont, 16, out Dimension drawArea,
                    LeftOffset, offsetY, textbox: Align.TopLeft, alpha: alpha);
                offsetY += drawArea.Height + MessageSpacing;
            }
        }

        private static bool MessageTooOldToDraw(in ConsoleMessage msg, WorldBase world, HelionConsole console)
        {
            return msg.TimeNanos < world.CreationTimeNanos || msg.TimeNanos < console.LastClosedNanos;
        }

        private static void DrawFPS(Config config, Dimension viewport, FpsTracker fpsTracker,
            DrawHelper draw, Font? consoleFont, out int y)
        {
            y = 0;

            if (consoleFont == null || !config.Render.ShowFPS)
                return;

            string avgFps = $"FPS: {(int)Math.Round(fpsTracker.AverageFramesPerSecond)}";
            draw.Text(Color.White, avgFps, consoleFont, 16, out Dimension avgArea,
                viewport.Width - 1, y, textbox: Align.TopRight);
            y += avgArea.Height + FpsMessageSpacing;

            string maxFps = $"Max FPS: {(int)Math.Round(fpsTracker.MaxFramesPerSecond)}";
            draw.Text(Color.White, maxFps, consoleFont, 16, out Dimension maxArea,
                viewport.Width - 1, y, textbox: Align.TopRight);
            y += maxArea.Height + FpsMessageSpacing;

            string minFps = $"Min FPS: {(int)Math.Round(fpsTracker.MinFramesPerSecond)}";
            draw.Text(Color.White, minFps, consoleFont, 16, out Dimension minArea,
                viewport.Width - 1, y, textbox: Align.TopRight);

            y += minArea.Height;
        }

        private static float CalculateFade(long timeSinceMessage)
        {
            if (timeSinceMessage < OpaqueNanoRange)
                return 1.0f;

            double fractionIntoFadeRange = (double)(timeSinceMessage - OpaqueNanoRange) / FadingNanoSpan;
            return 1.0f - (float)fractionIntoFadeRange;
        }
    }
}