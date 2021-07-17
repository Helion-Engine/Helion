using System;
using System.Drawing;
using Helion.Geometry;
using Helion.Render.Common.Context;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;

namespace Helion.Layer.New.Worlds
{
    public partial class WorldLayer
    {
        private const int DebugFontSize = 16;
        private const int MaxHudMessages = 4;
        private const int LeftOffset = 1;
        private const int TopOffset = 1;
        private const int MessageSpacing = 1;
        private const int FpsMessageSpacing = 2;
        private const int CrosshairLength = 10;
        private const int CrosshairWidth = 4;
        private const int CrosshairHalfWidth = CrosshairWidth / 2;
        private const int FullHudFaceX = 149;
        private const int FullHudFaceY = 170;
        private const long MaxVisibleTimeNanos = 4 * 1000L * 1000L * 1000L;
        private const long FadingNanoSpan = 350L * 1000L * 1000L;
        private const long OpaqueNanoRange = MaxVisibleTimeNanos - FadingNanoSpan;
        private static readonly Color PickupColor = Color.FromArgb(255, 255, 128);
        private static readonly Color DamageColor = Color.FromArgb(255, 0, 0);
        private static readonly string SmallHudFont = "SmallFont";
        private static readonly string LargeHudFont = "LargeHudFont";
        private static readonly string ConsoleFont = "Console";
        private int m_fontHeight = 16;
        private int m_padding = 4;
        private float m_scale = 1.0f;
        private Dimension m_viewport;
        
        private void DrawHud(HudRenderContext hudContext, IHudRenderContext hud)
        {
            m_scale = (float)m_config.Hud.Scale.Value;
            m_padding = (int)(4 * m_scale);
            m_fontHeight = (int)(16 * m_scale);
            m_viewport = hud.Dimension;

            DrawFPS(hud, out int topRightY);
            DrawPosition(hud, ref topRightY);
            // DrawBottomHud(topRightY, hudContext.DrawAutomap);
            // DrawPowerupEffect();
            // DrawPickupFlash();
            // DrawDamage();
            // DrawRecentConsoleMessages();
        }
        
        private void DrawFPS(IHudRenderContext hud, out int topRightY)
        {
            topRightY = 0;

            if (!m_config.Render.ShowFPS)
                return;

            DrawFpsValue("", m_fpsTracker.AverageFramesPerSecond, ref topRightY);
            DrawFpsValue("Max ", m_fpsTracker.MaxFramesPerSecond, ref topRightY);
            DrawFpsValue("Min ", m_fpsTracker.MinFramesPerSecond, ref topRightY);

            void DrawFpsValue(string prefix, double fps, ref int y)
            {
                string avgFps = $"{prefix}FPS: {(int)Math.Round(fps)}";
                hud.Text(avgFps, ConsoleFont, DebugFontSize, (-1, y), out Dimension avgArea, 
                    TextAlign.Right, both: Align.TopRight);
                y += avgArea.Height + FpsMessageSpacing;
            }
        }
        
        private void DrawPosition(IHudRenderContext hud, ref int topRightY)
        {
            if (!Player.Cheats.IsCheatActive(Helion.World.Cheats.CheatType.ShowPosition))
                return;

            DrawCoordinate('X', Player.Position.X, ref topRightY);
            DrawCoordinate('Y', Player.Position.Y, ref topRightY);
            DrawCoordinate('Z', Player.Position.Z, ref topRightY);
            
            void DrawCoordinate(char axis, double position, ref int y)
            {
                hud.Text($"{axis}: {Math.Round(position, 4)}", ConsoleFont, DebugFontSize, 
                    (-1, y), out Dimension area, TextAlign.Right, both: Align.TopRight,
                    color: Color.White);
                y += area.Height + FpsMessageSpacing;
            }
        }
        
        // private void DrawBottomHud(int topRightY, Dimension viewport, bool drawAutomap)
        // {
        //     if (Player.AnimationWeapon != null && !drawAutomap)
        //     {
        //         // Doom pushes the gun sprite up when the status bar is showing
        //         int yOffset = m_config.Hud.StatusBarSize == StatusBarSizeType.Full ? 16 : 0;
        //         DrawHudWeapon(player, Player.AnimationWeapon.FrameState, yOffset);
        //         if (Player.AnimationWeapon.FlashState.Frame.BranchType != Resources.Definitions.Decorate.States.ActorStateBranch.Stop)
        //             DrawHudWeapon(Player.AnimationWeapon.FlashState, yOffset);
        //     }
        //
        //     if (!drawAutomap)
        //         DrawHudCrosshair(viewport);
        //
        //     switch (m_config.Hud.StatusBarSize.Value)
        //     {
        //     case StatusBarSizeType.Full:
        //         DrawFullStatusBar();
        //         break;
        //     case StatusBarSizeType.Minimal:
        //         DrawMinimalStatusBar(topRightY);
        //         break;
        //     }            
        // }
    }
}
