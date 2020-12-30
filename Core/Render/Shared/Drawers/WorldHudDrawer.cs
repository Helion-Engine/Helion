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
using Helion.Util.Configuration;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;
using Helion.Util.Time;
using Helion.World;
using Helion.World.Entities.Definition.Properties;
using Helion.World.Entities.Definition.States;
using Helion.World.Entities.Inventories;
using Helion.World.Entities.Players;
using MoreLinq;
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
        private const long MaxVisibleTimeNanos = 4 * 1000L * 1000L * 1000L;
        private const long FadingNanoSpan = 350L * 1000L * 1000L;
        private const long OpaqueNanoRange = MaxVisibleTimeNanos - FadingNanoSpan;
        private static readonly Color PickupColor = Color.FromArgb(255, 255, 128);
        private static readonly Color DamageColor = Color.FromArgb(255, 0, 0);
        private static readonly CIString SmallHudFont = "SmallFont";
        private static readonly CIString LargeHudFont = "LargeHudFont";
        private static readonly CIString ConsoleFont = "Console";

        private readonly ArchiveCollection m_archiveCollection;

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
            if (config.Engine.Hud.FullStatusBar)
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
                DrawFullHudKeys(player, draw);
                DrawFullHudFace(player, draw);
            });
        }

        private void DrawFullHudHealthArmorAmmo(Player player, Font? largeFont, DrawHelper draw)
        {
            const int offsetY = 171;

            if (player.Weapon != null)
            {
                // TODO: Need to get the ammo.
                int ammoAmount = 50;
                string ammo = Math.Clamp(ammoAmount, 0, 999).ToString();
                DrawFullHudBigFont(ammo, 43, offsetY, largeFont, draw);
            }

            string health = $"{Math.Clamp(player.Health, 0, 999)}%";
            DrawFullHudBigFont(health, 102, offsetY, largeFont, draw);

            string armor = $"{Math.Clamp(player.Armor, 0, 999)}%";
            DrawFullHudBigFont(armor, 233, offsetY, largeFont, draw);
        }

        private void DrawFullHudBigFont(string message, int x, int y, Font? largeFont, DrawHelper draw)
        {
            const int FullHudLargeFontSize = 15;

            if (largeFont == null)
                return;

            draw.Text(Color.Red, message, largeFont, FullHudLargeFontSize, x, y, TextAlign.Right,
                textbox: Align.TopRight);
        }

        private void DrawFullHudWeaponSlots(Player player, DrawHelper draw)
        {
            draw.Image("STARMS", 104, 0, both: Align.BottomLeft);

            // TODO: Draw weapon numbers if we have them.
        }

        private void DrawFullHudKeys(Player player, DrawHelper draw)
        {
            const int x = 239;

            foreach (InventoryItem key in player.Inventory.GetKeys())
            {
                DrawKeyIfOwned(key, "BlueSkull", "BlueCard", x, 171, draw);
                DrawKeyIfOwned(key, "YellowSkull", "YellowCard", x, 181, draw);
                DrawKeyIfOwned(key, "RedSkull", "RedCard", x, 191, draw);
            }
        }

        private void DrawKeyIfOwned(InventoryItem key, string skullKeyName, string keyName, int x, int y,
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

        private void DrawFullHudFace(Player player, DrawHelper draw)
        {
            // TODO
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

            if (player.Armor > 0)
            {
                y -= healthArea.Height + Padding;

                EntityProperties? armorProp = player.ArmorProperties;
                if (armorProp != null && draw.ImageExists(armorProp.Inventory.Icon))
                {
                    draw.Image(armorProp.Inventory.Icon, x, y, out Dimension armorArea, both: Align.BottomLeft);
                    x += armorArea.Width + Padding;
                }

                draw.Text(Color.Red, player.Armor.ToString(), largeFont, fontHeight,
                    x, y, TextAlign.Left, both: Align.BottomLeft);
            }
        }

        private void DrawMinimalHudKeys(int y, Player player, DrawHelper draw)
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

        private void DrawHudWeapon(Player player, float tickFraction, FrameState frameState, Dimension viewport,
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

                // Translate doom image offset to OpenGL coordinates
                int x = (offset.X / 2) - (width / 2) + weaponOffset.X;
                int y = -offset.Y - height + weaponOffset.Y;
                draw.Image(sprite, x, y, width, height, color: lightLevelColor);
            }
        }

        private void DrawMinimalHudAmmo(Player player, Font? largeFont, DrawHelper helper)
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

        private void DrawHudCrosshair(Dimension viewport, DrawHelper helper)
        {
            Vec2I center = viewport.ToVector() / 2;
            Vec2I horizontalStart = center - new Vec2I(CrosshairLength, CrosshairHalfWidth);
            Vec2I verticalStart = center - new Vec2I(CrosshairHalfWidth, CrosshairLength);

            helper.FillRect(horizontalStart.X, horizontalStart.Y, CrosshairLength * 2, CrosshairHalfWidth * 2, Color.LawnGreen);
            helper.FillRect(verticalStart.X, verticalStart.Y, CrosshairHalfWidth * 2, CrosshairLength * 2, Color.LawnGreen);
        }

        private void DrawPickupFlash(Player player, WorldBase world, Dimension viewport, DrawHelper helper)
        {
            int ticksSincePickup = world.Gametick - player.LastPickupGametick;
            if (ticksSincePickup < FlashPickupTickDuration)
                helper.FillRect(0, 0, viewport.Width, viewport.Height, PickupColor, 0.15f);
        }

        private void DrawPowerupEffect(Player player, Dimension viewport, DrawHelper helper)
        {
            if (player.Inventory.PowerupEffectColor?.DrawColor == null || !player.Inventory.PowerupEffectColor.DrawPowerupEffect)
                return;

            helper.FillRect(0, 0, viewport.Width, viewport.Height, player.Inventory.PowerupEffectColor.DrawColor.Value,
                player.Inventory.PowerupEffectColor.DrawAlpha);
        }

        private void DrawDamage(Player player, WorldBase world, Dimension viewport, DrawHelper helper)
        {
            if (player.DamageCount > 0)
                helper.FillRect(0, 0, viewport.Width, viewport.Height, DamageColor, player.DamageCount * 0.01f);
        }

        private void DrawRecentConsoleMessages(WorldBase world, HelionConsole console, Font? smallFont,
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

            messages.ForEach(pair =>
            {
                helper.Text(pair.message, smallFont, 16, out Dimension drawArea,
                    LeftOffset, offsetY, textbox: Align.TopLeft, alpha: pair.alpha);
                offsetY += drawArea.Height + MessageSpacing;
            });
        }

        private static bool MessageTooOldToDraw(in ConsoleMessage msg, WorldBase world, HelionConsole console)
        {
            return msg.TimeNanos < world.CreationTimeNanos || msg.TimeNanos < console.LastClosedNanos;
        }

        private void DrawFPS(Config config, Dimension viewport, FpsTracker fpsTracker,
            DrawHelper draw, Font? consoleFont, out int y)
        {
            y = 0;

            if (consoleFont == null || !config.Engine.Render.ShowFPS)
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