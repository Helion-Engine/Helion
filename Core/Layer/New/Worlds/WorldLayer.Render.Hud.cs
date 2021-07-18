using System;
using System.Collections.Generic;
using System.Drawing;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Render.Common;
using Helion.Render.Common.Context;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Render.Legacy.Util;
using Helion.Resources.Definitions.Decorate.States;
using Helion.Util;
using Helion.World.Entities.Definition.Properties;
using Helion.World.Entities.Definition.States;
using Helion.World.Entities.Inventories;
using Helion.World.Entities.Inventories.Powerups;
using Helion.World.StatusBar;
using static Helion.Render.Common.RenderDimensions;

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
            DrawBottomHud(hud, topRightY, hudContext.DrawAutomap);
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
        
        private void DrawBottomHud(IHudRenderContext hud, int topRightY, bool drawAutomap)
        {
            if (Player.AnimationWeapon != null && !drawAutomap)
            {
                // Doom pushes the gun sprite up when the status bar is showing
                int yOffset = m_config.Hud.StatusBarSize == StatusBarSizeType.Full ? 16 : 0;
                DrawHudWeapon(hud, Player.AnimationWeapon.FrameState, yOffset);
                if (Player.AnimationWeapon.FlashState.Frame.BranchType != ActorStateBranch.Stop)
                    DrawHudWeapon(hud, Player.AnimationWeapon.FlashState, yOffset);
            }
        
            if (!drawAutomap)
                DrawCrosshair(hud);
        
            switch (m_config.Hud.StatusBarSize.Value)
            {
            case StatusBarSizeType.Minimal:
                DrawMinimalStatusBar(hud, topRightY);
                break;
            case StatusBarSizeType.Full:
                DrawFullStatusBar(hud);
                break;
            }            
        }

        private void DrawHudWeapon(IHudRenderContext hud, FrameState frameState, int yOffset)
        {
            int lightLevel;

            if (frameState.Frame.Properties.Bright || Player.DrawFullBright())
            {
                lightLevel = 255;
            }
            else
            {
                int extraLight = Player.ExtraLight * Constants.ExtraLightFactor;
                if (m_config.Render.LightDropoff)
                    lightLevel = GLHelper.DoomLightLevelToColor(Player.Sector.LightLevel, extraLight);
                else
                    lightLevel = (int)(GLHelper.DoomLightLevelToColorStatic(Player.Sector.LightLevel, extraLight) * 255);
            }

            Color lightLevelColor = Color.FromArgb(lightLevel, lightLevel, lightLevel);
            string sprite = frameState.Frame.Sprite + (char)(frameState.Frame.Frame + 'A') + "0";

            if (!hud.Textures.TryGet(sprite, out var handle))
                return;
            
            hud.DoomVirtualResolution(() =>
            {
                Dimension dimension = handle.Dimension;
                Vec2I offset = handle.Offset;
                float tickFraction = m_lastTickInfo.Fraction;
                
                offset.Y += yOffset;
                Vec2I weaponOffset = Player.PrevWeaponOffset.Interpolate(Player.WeaponOffset, tickFraction).Int;
                
                float alpha = 1.0f;
                IPowerup? powerup = Player.Inventory.GetPowerup(PowerupType.Invisibility);
                if (powerup != null && powerup.DrawPowerupEffect)
                    alpha = 0.3f;
                
                bool drawInvul = Player.DrawInvulnerableColorMap();
                offset = TranslateDoomOffset(offset, dimension);

                // TODO: Invulnerability!
                hud.Image(sprite, offset + weaponOffset, color: lightLevelColor, alpha: alpha);
            });
        }
        
        private void DrawCrosshair(IHudRenderContext hud)
        {
            const int Width = 4;
            const int HalfWidth = Width / 2;
            const int Length = 10;

            Vec2I center = m_viewport.Vector / 2;
            Vec2I horizontal = center - new Vec2I(Length, HalfWidth);
            Vec2I vertical = center - new Vec2I(HalfWidth, Length);

            hud.FillBox((horizontal.X, horizontal.Y, Length * 2, HalfWidth * 2), Color.LawnGreen);
            hud.FillBox((vertical.X, vertical.Y, HalfWidth * 2, Length * 2), Color.LawnGreen);
        }
        
        private void DrawMinimalStatusBar(IHudRenderContext hud, int topRightY)
        {
            DrawMinimalHudHealthAndArmor(hud);
            DrawMinimalHudKeys(hud, topRightY);
            DrawMinimalHudAmmo(hud);
        }

        private void DrawMinimalHudHealthAndArmor(IHudRenderContext hud)
        {
            const string Medkit = "MEDIA0";
            
            // We will draw the medkit slightly higher so it looks like it
            // aligns with the font.
            int x = m_padding;
            int y = -m_padding;
            
            hud.Image(Medkit, (x, y), out HudBox medkitArea, both: Align.BottomLeft, scale: m_scale);

            string health = Math.Max(0, Player.Health).ToString();
            hud.Text(health, LargeHudFont, m_fontHeight, (x + medkitArea.Width + m_padding, y), 
                both: Align.BottomLeft);
            
            DrawFace(hud, (x + medkitArea.Width + (m_padding * 3), y), Align.BottomLeft, true);
            
            if (Player.Armor > 0)
            {
                y -= medkitArea.Height + m_padding;
            
                EntityProperties? armorProp = Player.ArmorProperties;
                if (armorProp != null && hud.Textures.HasImage(armorProp.Inventory.Icon))
                {
                    hud.Image(armorProp.Inventory.Icon, (x, y), out HudBox armorArea, both: Align.BottomLeft,
                        scale: m_scale);
                    x += armorArea.Width + m_padding;
                }
            
                hud.Text(Player.Armor.ToString(), LargeHudFont, m_fontHeight, (x, y), both: Align.BottomLeft);
            }
        }
        
        private void DrawFace(IHudRenderContext hud, Vec2I origin, Align? both = null, bool scaleDraw = false)
        {
            hud.Image(Player.StatusBar.GetFacePatch(), origin, both: both, scale: scaleDraw ? m_scale : 1.0f);
        }
        
        private void DrawMinimalHudKeys(IHudRenderContext hud, int y)
        {
            List<InventoryItem> keys = Player.Inventory.GetKeys();
            y += m_padding;

            foreach (InventoryItem key in keys)
            {
                string icon = key.Definition.Properties.Inventory.Icon;
                if (!hud.Textures.HasImage(icon))
                    continue;
                
                hud.Image(icon, (-m_padding, y), out HudBox drawArea, both: Align.TopRight, scale: m_scale);
                y += drawArea.Height + m_padding;
            }
        }

        private void DrawMinimalHudAmmo(IHudRenderContext hud)
        {
            if (Player.Weapon == null)
                return;
            
            string ammoType = Player.Weapon.Definition.Properties.Weapons.AmmoType;
            if (ammoType == "")
                return;

            int x = -m_padding;
            int y = -m_padding;

            int ammo = Player.Inventory.Amount(ammoType);
            hud.Text(ammo.ToString(), LargeHudFont, m_fontHeight, (x, y), out Dimension textRect, 
                both: Align.BottomRight);

            x -= textRect.Width + m_padding;
            if (Player.Weapon.AmmoSprite.Length <= 0 || !hud.Textures.TryGet(Player.Weapon.AmmoSprite, out var handle)) 
                return;
            
            x -= (int)(handle.Dimension.Width * m_scale);
            hud.Image(Player.Weapon.AmmoSprite, (x, y), both: Align.BottomRight, scale: m_scale);
        }
        
        private void DrawFullStatusBar(IHudRenderContext hud)
        {
            // TODO
        }
    }
}
