using System;
using System.Collections.Generic;
using System.Drawing;
using Helion.Graphics.String;
using Helion.Render.Commands;
using Helion.Render.Commands.Alignment;
using Helion.Render.OpenGL.Util;
using Helion.Render.Shared.Drawers.Helper;
using Helion.Util;
using Helion.Util.Configuration;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;
using Helion.Util.Time;
using Helion.World;
using Helion.World.Entities.Definition.States;
using Helion.World.Entities.Players;
using MoreLinq;

namespace Helion.Render.Shared.Drawers
{
    public static class WorldHudDrawer
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
        private const int KeyWidth = 16;
        private const long MaxVisibleTimeNanos = 4 * 1000L * 1000L * 1000L;
        private const long FadingNanoSpan = 350L * 1000L * 1000L;
        private const long OpaqueNanoRange = MaxVisibleTimeNanos - FadingNanoSpan;
        private static readonly Color PickupColor = Color.FromArgb(255, 255, 128);
        private static readonly Color DamageColor = Color.FromArgb(255, 0, 0);
        private static readonly string HudFont = "LargeHudFont";

        public static void Draw(Player player, WorldBase world, float tickFraction, HelionConsole console,
            Dimension viewport, Config config, RenderCommands cmd)
        {
            DrawHelper draw = new(cmd);

            cmd.ClearDepth();

            DrawFPS(cmd.Config, viewport, cmd.FpsTracker, draw, out int topRightY);
            DrawHud(topRightY, player, world, tickFraction, viewport, config, draw);
            DrawPowerupEffect(player, viewport, draw);
            DrawPickupFlash(player, world, viewport, draw);
            DrawDamage(player, world, viewport, draw);
            DrawRecentConsoleMessages(world, console, draw);
        }

        private static void DrawHud(int topRightY, Player player, WorldBase world, float tickFraction,
            Dimension viewport, Config config, DrawHelper draw)
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
                DrawFullStatusBar(player, draw);
            else
                DrawMinimalStatusBar(player, topRightY, viewport, draw);
        }


        private static void DrawFullStatusBar(Player player, DrawHelper draw)
        {
            draw.AtResolution(DoomHudHelper.DoomResolutionInfo, () =>
            {
                draw.Image("STBAR", window: Align.BottomLeft, image: Align.BottomLeft);

                DrawFullHudHealthArmorAmmo(player, draw);
                DrawFullHudWeaponSlots(player, draw);
                DrawFullHudKeys(player, draw);
                DrawFullHudFace(player, draw);
            });
        }

        private static void DrawFullHudHealthArmorAmmo(Player player, DrawHelper draw)
        {
            const int offsetY = 172;

            if (player.Weapon != null)
            {
                // TODO: Need to get the ammo.
                int ammoAmount = 50;
                string ammo = Math.Clamp(ammoAmount, 0, 999).ToString();
                DrawFullHudBigFont(ammo, 8, offsetY, draw);
            }

            string health = $"{Math.Clamp(player.Health, 0, 999)}%";
            DrawFullHudBigFont(health, 53, offsetY, draw);

            string armor = $"{Math.Clamp(player.Armor, 0, 999)}%";
            DrawFullHudBigFont(armor, 188, offsetY, draw);
        }

        private static void DrawFullHudBigFont(string message, int x, int y, DrawHelper draw)
        {
            const int FullHudLargeFontSize = 76;

            draw.Text(Color.Red, message, "LargeHudFont", FullHudLargeFontSize, x, y, TextAlign.Right,
                textbox: Align.TopLeft);
        }

        private static void DrawFullHudWeaponSlots(Player player, DrawHelper draw)
        {
            draw.Image("STARMS", 104, 0, window: Align.BottomLeft, image: Align.BottomLeft);

            // TODO: Draw weapon numbers if we have them.
        }

        private static void DrawFullHudKeys(Player player, DrawHelper draw)
        {
            // TODO
        }

        private static void DrawFullHudFace(Player player, DrawHelper draw)
        {
            // TODO
        }

        private static void DrawMinimalStatusBar(Player player, in int topRightY, Dimension viewport, DrawHelper draw)
        {
            DrawMinimalHudHealthAndArmor(player, viewport, draw);
            DrawMinimalHudKeys(topRightY, player, viewport, draw);
            DrawMinimalHudAmmo(player, viewport, draw);
        }

        private static void DrawMinimalHudHealthAndArmor(Player player, Dimension viewport, DrawHelper draw)
        {
            // We will draw the medkit slightly higher so it looks like it
            // aligns with the font.
            int x = Padding;
            int y = viewport.Height - Padding;

            draw.Image("MEDIA0", Padding, -Padding, out Dimension medkitArea,
                window: Align.BottomLeft, image: Align.BottomLeft);

            // We will draw the health numbers with the same height as the
            // medkit image. However if someone ever replaces it, we probably
            // want to draw it at the height of that image. We also don't want
            // to have a missing or small image screw up the height so we'll
            // clamp it to be at least 16. Let's get a more robust solution in
            // the future!
            int fontHeight = Math.Max(16, medkitArea.Height);
            int health = Math.Max(0, player.Health);
            draw.Text(Color.Red, health.ToString(), HudFont, fontHeight, out Dimension healthArea,
                x + medkitArea.Width + Padding, y, TextAlign.Left, textbox: Align.BottomLeft);

            if (player.Armor > 0)
            {
                y -= healthArea.Height + Padding;

                if (player.ArmorProperties != null && draw.ImageExists(player.ArmorProperties.Inventory.Icon))
                {
                    draw.Image(player.ArmorProperties.Inventory.Icon, x, y, out Dimension armorArea, window: Align.BottomLeft);
                    x += armorArea.Width + Padding;
                }

                draw.Text(Color.Red, player.Armor.ToString(), HudFont, fontHeight,
                    x, y, TextAlign.Left, textbox: Align.BottomLeft);
            }
        }

        private static void DrawMinimalHudKeys(int y, Player player, Dimension viewport, DrawHelper draw)
        {
            var keys = player.Inventory.GetKeys();
            y += Padding;

            foreach (var key in keys)
            {
                string icon = key.Definition.Properties.Inventory.Icon;
                if (!draw.ImageExists(icon))
                    continue;

                Dimension dimension = draw.DrawInfoProvider.GetImageDimension(icon);
                int height = (int)(KeyWidth / dimension.AspectRatio);
                draw.Image(icon, viewport.Width - Padding - KeyWidth, y, KeyWidth, height);
                y += height + Padding;
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

                // Translate doom image offset to OpenGL coordinates
                int x = (offset.X / 2) - (width / 2) + weaponOffset.X;
                int y = -offset.Y - height + weaponOffset.Y;
                draw.Image(sprite, x, y, width, height, color: lightLevelColor);
            }
        }

        private static void DrawMinimalHudAmmo(Player player, Dimension viewport, DrawHelper helper)
        {
            if (player.Weapon == null || player.Weapon.Definition.Properties.Weapons.AmmoType.Length == 0)
                return;

            int x = viewport.Width - Padding;
            int y = viewport.Height - Padding;

            int ammo = player.Inventory.Amount(player.Weapon.Definition.Properties.Weapons.AmmoType);
            ColoredString colorString = ColoredStringBuilder.From(Color.Red, ammo.ToString());

            Dimension textRect = helper.DrawInfoProvider.GetDrawArea(colorString, HudFont, 19);

            if (player.Weapon.AmmoSprite.Length > 0 && helper.ImageExists(player.Weapon.AmmoSprite))
            {
                Dimension dimension = helper.DrawInfoProvider.GetImageDimension(player.Weapon.AmmoSprite);
                x -= dimension.Width;
                helper.Image(player.Weapon.AmmoSprite, x, y - dimension.Height);
            }

            x = x - textRect.Width - Padding;
            helper.Text(colorString, HudFont, 19, x, y, textbox: Align.BottomLeft);
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
            if (player.Inventory.PowerupEffect == null || !player.Inventory.PowerupEffect.DrawColor.HasValue || !player.Inventory.PowerupEffect.DrawPowerupEffect)
                return;

            helper.FillRect(0, 0, viewport.Width, viewport.Height, player.Inventory.PowerupEffect.DrawColor.Value,
                player.Inventory.PowerupEffect.DrawAlpha);
        }

        private static void DrawDamage(Player player, WorldBase world, Dimension viewport, DrawHelper helper)
        {
            if (player.DamageCount > 0)
                helper.FillRect(0, 0, viewport.Width, viewport.Height, DamageColor, player.DamageCount * 0.01f);
        }

        private static void DrawRecentConsoleMessages(WorldBase world, HelionConsole console, DrawHelper helper)
        {
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
                helper.Text(pair.message, "SmallFont", 16, out Dimension drawArea,
                    LeftOffset, offsetY, textbox: Align.TopLeft, alpha: pair.alpha);
                offsetY += drawArea.Height + MessageSpacing;
            });
        }

        private static bool MessageTooOldToDraw(in ConsoleMessage msg, WorldBase world, HelionConsole console)
        {
            return msg.TimeNanos < world.CreationTimeNanos || msg.TimeNanos < console.LastClosedNanos;
        }

        private static void DrawFPS(Config config, Dimension viewport, FpsTracker fpsTracker,
            DrawHelper draw, out int y)
        {
            y = 0;

            if (!config.Engine.Render.ShowFPS)
                return;

            string avgFps = $"FPS: {(int)Math.Round(fpsTracker.AverageFramesPerSecond)}";
            draw.Text(Color.White, avgFps, "Console", 16, out Dimension avgArea,
                viewport.Width - 1, y, textbox: Align.TopRight);
            y += avgArea.Height + FpsMessageSpacing;

            string maxFps = $"Max FPS: {(int)Math.Round(fpsTracker.MaxFramesPerSecond)}";
            draw.Text(Color.White, maxFps, "Console", 16, out Dimension maxArea,
                viewport.Width - 1, y, textbox: Align.TopRight);
            y += maxArea.Height + FpsMessageSpacing;

            string minFps = $"Min FPS: {(int)Math.Round(fpsTracker.MinFramesPerSecond)}";
            draw.Text(Color.White, minFps, "Console", 16, out Dimension minArea,
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