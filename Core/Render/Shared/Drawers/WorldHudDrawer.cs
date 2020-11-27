using System;
using System.Collections.Generic;
using System.Drawing;
using Helion.Graphics.String;
using Helion.Render.Commands;
using Helion.Render.Commands.Align;
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
        private const long MaxVisibleTimeNanos = 4 * 1000L * 1000L * 1000L;
        private const long FadingNanoSpan = 350L * 1000L * 1000L;
        private const long OpaqueNanoRange = MaxVisibleTimeNanos - FadingNanoSpan;
        private static readonly Color PickupColor = Color.FromArgb(255, 255, 128);
        private static readonly Color DamageColor = Color.FromArgb(255, 0, 0);

        public static void Draw(Player player, WorldBase world, HelionConsole console, Dimension viewport, RenderCommands cmd)
        {
            DrawHelper helper = new DrawHelper(cmd);
            
            cmd.ClearDepth();

            DrawHud(player, world, viewport, helper);
            DrawPickupFlash(player, world, viewport, helper);
            DrawDamage(player, world, viewport, helper);
            DrawRecentConsoleMessages(world, console, helper);
            DrawFPS(cmd.Config, viewport, cmd.FpsTracker, helper);
        }
        
        private static void DrawHud(Player player, WorldBase world, Dimension viewport, DrawHelper helper)
        {
            DrawHudHealth(player, viewport, helper);
            DrawHudCrosshair(viewport, helper);

            if (player.Weapon != null)
            {
                DrawHudWeapon(player.Weapon.FrameState, viewport, helper);
                if (player.Weapon.FlashState.Frame.BranchType != Resources.Definitions.Decorate.States.ActorStateBranch.Stop)
                    DrawHudWeapon(player.Weapon.FlashState, viewport, helper);
            }
        }

        private static void DrawHudWeapon(FrameState frameState, Dimension viewport, DrawHelper helper)
        {
            string sprite = frameState.Frame.Sprite + (char)(frameState.Frame.Frame + 'A') + "0";
            if (helper.ImageExists(sprite))
            {
                Dimension dimension = helper.DrawInfoProvider.GetImageDimension(sprite);
                // TODO verify - Doom appears to have some hardcoded Y offset (because reasons?)
                Vec2I offset = helper.DrawInfoProvider.GetImageOffset(sprite);
                offset.Y -= 32;
                ScaleDimensions(viewport, ref dimension.Width, ref dimension.Height);
                ScaleDimensions(viewport, ref offset.X, ref offset.Y);
                // Translate doom image offset to OpenGL coordinates
                helper.Image(sprite, (offset.X / 2) - (dimension.Width / 2),
                    -offset.Y - dimension.Height, dimension.Width, dimension.Height);
            }
        }

        private static void ScaleDimensions(Dimension viewport, ref int width, ref int height)
        {
            float scaleWidth = viewport.Width / 320.0f;
            float scaleHeight = viewport.Height / 200.0f;
            width = (int)(width * scaleWidth);
            height = (int)(height * scaleHeight);
        }

        private static void DrawHudHealth(Player player, Dimension viewport, DrawHelper helper)
        {
            // We will draw the medkit slightly higher so it looks like it
            // aligns with the font.
            int x = 4;
            int y = viewport.Height - 4;
            helper.Image("MEDIA0", x, y, Alignment.BottomLeft, out Dimension medkitArea);

            // We will draw the health numbers with the same height as the
            // medkit image. However if someone ever replaces it, we probably
            // want to draw it at the height of that image. We also don't want
            // to have a missing or small image screw up the height so we'll
            // clamp it to be at least 16. Let's get a more robust solution in
            // the future!
            int fontHeight = Math.Max(16, medkitArea.Height);
            
            x += medkitArea.Width + 4;
            int health = Math.Max(0, player.Health);
            helper.Text(Color.Red, health.ToString(), "LargeHudFont", fontHeight, x, y, Alignment.BottomLeft, out _);
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
            Stack<(ColoredString msg, float alpha)> msgs = new Stack<(ColoredString, float)>();
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

        private static bool MessageTooOldToDraw(in ConsoleMessage msg, WorldBase world, HelionConsole console)
        {
            return msg.TimeNanos < world.CreationTimeNanos || msg.TimeNanos < console.LastClosedNanos;
        }

        private static void DrawFPS(Config config, Dimension viewport, FpsTracker fpsTracker, DrawHelper helper)
        {
            if (!config.Engine.Render.ShowFPS)
                return;

            int y = 0;
            
            string avgFps = $"FPS: {(int)Math.Round(fpsTracker.AverageFramesPerSecond)}";
            helper.Text(Color.White, avgFps, "Console", 16, viewport.Width - 1, y, Alignment.TopRight, out Dimension avgArea);
            y += avgArea.Height + FpsMessageSpacing;

            string maxFps = $"Max FPS: {(int)Math.Round(fpsTracker.MaxFramesPerSecond)}";
            helper.Text(Color.White, maxFps, "Console", 16, viewport.Width - 1, y, Alignment.TopRight, out Dimension maxArea);
            y += maxArea.Height + FpsMessageSpacing;

            string minFps = $"Min FPS: {(int)Math.Round(fpsTracker.MinFramesPerSecond)}";
            helper.Text(Color.White, minFps, "Console", 16, viewport.Width - 1, y, Alignment.TopRight, out _);
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