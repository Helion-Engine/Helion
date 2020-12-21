using System;
using System.Collections.Generic;
using System.Drawing;
using Helion.Graphics.String;
using Helion.Render.Commands;
using Helion.Render.Commands.Align;
using Helion.Render.OpenGL.Util;
using Helion.Render.Shared.Drawers.Helper;
using Helion.Resource.Definitions.Decorate.States;
using Helion.Util;
using Helion.Util.Configuration;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;
using Helion.Util.Time;
using Helion.Worlds;
using Helion.Worlds.Entities.Definition.States;
using Helion.Worlds.Entities.Players;
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

        public static void Draw(Player player, World world, float fraction, HelionConsole console, Dimension viewport, RenderCommands cmd)
        {
            DrawHelper helper = new(cmd);

            cmd.ClearDepth();

            int y = DrawFPS(cmd.Config, viewport, cmd.FpsTracker, helper);
            DrawHud(y, player, world, fraction, viewport, helper);
            DrawPickupFlash(player, world, viewport, helper);
            DrawDamage(player, world, viewport, helper);
            DrawRecentConsoleMessages(world, console, helper);
        }

        private static void DrawHud(int topRightY, Player player, World world, float fraction, Dimension viewport, DrawHelper helper)
        {
            DrawHudHealthAndArmor(player, viewport, helper);
            DrawHudKeys(topRightY, player, viewport, helper);
            DrawHudAmmo(player, viewport, helper);

            if (player.AnimationWeapon != null)
            {
                DrawHudWeapon(player, fraction, player.AnimationWeapon.FrameState, viewport, helper);
                if (player.AnimationWeapon.FlashState.Frame.BranchType != ActorStateBranch.Stop)
                    DrawHudWeapon(player, fraction, player.AnimationWeapon.FlashState, viewport, helper);
            }

            DrawHudCrosshair(viewport, helper);
        }

        private static void DrawHudKeys(int y, Player player, Dimension viewport, DrawHelper helper)
        {
            var keys = player.Inventory.GetKeys();
            y += Padding;

            foreach (var key in keys)
            {
                string icon = key.Definition.Properties.Inventory.Icon;
                if (!helper.ImageExists(icon))
                    continue;

                Dimension dimension = helper.DrawInfoProvider.GetImageDimension(icon);
                int height = (int)(KeyWidth / dimension.AspectRatio);
                helper.Image(icon, viewport.Width - Padding - KeyWidth, y, KeyWidth, height);
                y += height + Padding;
            }
        }

        private static void DrawHudWeapon(Player player, float fraction, FrameState frameState, Dimension viewport, DrawHelper helper)
        {
            int lightLevel = frameState.Frame.Properties.Bright ?  255 :
                (int)(GLHelper.DoomLightLevelToColor(player.Sector.LightLevel + (player.ExtraLight * Constants.ExtraLightFactor) + Constants.ExtraLightFactor) * 255);

            Color lightLevelColor = Color.FromArgb(lightLevel, lightLevel, lightLevel);
            string sprite = $"{frameState.Frame.Sprite.BaseName}A0";
            if (helper.ImageExists(sprite))
            {
                Dimension dimension = helper.DrawInfoProvider.GetImageDimension(sprite);
                Vec2I offset = helper.DrawInfoProvider.GetImageOffset(sprite);
                Vec2I weaponOffset = DrawHelper.ScaleWorldOffset(viewport, player.PrevWeaponOffset.Interpolate(player.WeaponOffset, fraction));

                DrawHelper.ScaleImageDimensions(viewport, ref dimension.Width, ref dimension.Height);
                DrawHelper.ScaleImageOffset(viewport, ref offset.X, ref offset.Y);

                // Translate doom image offset to OpenGL coordinates
                helper.Image(sprite, (offset.X / 2) - (dimension.Width / 2) + weaponOffset.X,
                    -offset.Y - dimension.Height + weaponOffset.Y,
                    dimension.Width, dimension.Height, lightLevelColor);
            }
        }

        private static void DrawHudHealthAndArmor(Player player, Dimension viewport, DrawHelper helper)
        {
            // We will draw the medkit slightly higher so it looks like it
            // aligns with the font.
            int x = Padding;
            int y = viewport.Height - Padding;
            helper.Image("MEDIA0", x, y, Alignment.BottomLeft, out Dimension medkitArea);

            // We will draw the health numbers with the same height as the
            // medkit image. However if someone ever replaces it, we probably
            // want to draw it at the height of that image. We also don't want
            // to have a missing or small image screw up the height so we'll
            // clamp it to be at least 16. Let's get a more robust solution in
            // the future!
            int fontHeight = Math.Max(16, medkitArea.Height);
            int health = Math.Max(0, player.Health);
            helper.Text(Color.Red, health.ToString(), HudFont, fontHeight, x + medkitArea.Width + Padding, y, Alignment.BottomLeft, out Dimension healthArea);

            if (player.Armor > 0)
            {
                y -= healthArea.Height + Padding;

                if (player.ArmorProperties != null && helper.ImageExists(player.ArmorProperties.Inventory.Icon))
                {
                    helper.Image(player.ArmorProperties.Inventory.Icon, x, y, Alignment.BottomLeft, out Dimension armorArea);
                    x += armorArea.Width + Padding;
                }

                helper.Text(Color.Red, player.Armor.ToString(), HudFont, fontHeight, x, y, Alignment.BottomLeft, out _);
            }
        }

        private static void DrawHudAmmo(Player player, Dimension viewport, DrawHelper helper)
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
            helper.Text(colorString, HudFont, 19, x, y, Alignment.BottomLeft, out _);
        }

        private static void DrawHudCrosshair(Dimension viewport, DrawHelper helper)
        {
            Vec2I center = viewport.ToVector() / 2;
            Vec2I horizontalStart = center - new Vec2I(CrosshairLength, CrosshairHalfWidth);
            Vec2I verticalStart = center - new Vec2I(CrosshairHalfWidth, CrosshairLength);

            helper.FillRect(horizontalStart.X, horizontalStart.Y, CrosshairLength * 2, CrosshairHalfWidth * 2, Color.LawnGreen);
            helper.FillRect(verticalStart.X, verticalStart.Y, CrosshairHalfWidth * 2, CrosshairLength * 2, Color.LawnGreen);
        }

        private static void DrawPickupFlash(Player player, World world, Dimension viewport, DrawHelper helper)
        {
            int ticksSincePickup = world.Gametick - player.LastPickupGametick;
            if (ticksSincePickup < FlashPickupTickDuration)
                helper.FillRect(0, 0, viewport.Width, viewport.Height, PickupColor, 0.15f);
        }

        private static void DrawDamage(Player player, World world, Dimension viewport, DrawHelper helper)
        {
            if (player.DamageCount > 0)
                helper.FillRect(0, 0, viewport.Width, viewport.Height, DamageColor, player.DamageCount * 0.01f);
        }

        private static void DrawRecentConsoleMessages(World world, HelionConsole console, DrawHelper helper)
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
            Stack<(ColoredString msg, float alpha)> msgs = new();
            foreach (ConsoleMessage msg in console.Messages)
            {
                if (messagesDrawn >= MaxHudMessages || MessageTooOldToDraw(msg, world, console))
                    break;

                long timeSinceMessage = currentNanos - msg.TimeNanos;
                if (timeSinceMessage > MaxVisibleTimeNanos)
                    break;

                msgs.Push((msg.Message, CalculateFade(timeSinceMessage)));
                messagesDrawn++;
            }

            msgs.ForEach(pair =>
            {
                helper.Text(pair.msg, "SmallFont", 16, LeftOffset, offsetY, Alignment.TopLeft,
                            pair.alpha, out Dimension drawArea);
                offsetY += drawArea.Height + MessageSpacing;
            });
        }

        private static bool MessageTooOldToDraw(in ConsoleMessage msg, World world, HelionConsole console)
        {
            return msg.TimeNanos < world.CreationTimeNanos || msg.TimeNanos < console.LastClosedNanos;
        }

        private static int DrawFPS(Config config, Dimension viewport, FpsTracker fpsTracker, DrawHelper helper)
        {
            if (!config.Engine.Render.ShowFPS)
                return 0;

            int y = 0;

            string avgFps = $"FPS: {(int)Math.Round(fpsTracker.AverageFramesPerSecond)}";
            helper.Text(Color.White, avgFps, "Console", 16, viewport.Width - 1, y, Alignment.TopRight, out Dimension avgArea);
            y += avgArea.Height + FpsMessageSpacing;

            string maxFps = $"Max FPS: {(int)Math.Round(fpsTracker.MaxFramesPerSecond)}";
            helper.Text(Color.White, maxFps, "Console", 16, viewport.Width - 1, y, Alignment.TopRight, out Dimension maxArea);
            y += maxArea.Height + FpsMessageSpacing;

            string minFps = $"Min FPS: {(int)Math.Round(fpsTracker.MinFramesPerSecond)}";
            helper.Text(Color.White, minFps, "Console", 16, viewport.Width - 1, y, Alignment.TopRight, out Dimension minArea);

            return y + minArea.Height;
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