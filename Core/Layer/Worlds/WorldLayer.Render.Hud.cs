using System;
using System.Collections.Generic;
using System.Drawing;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics.String;
using Helion.Render.Common;
using Helion.Render.Common.Context;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Render.Legacy.Util;
using Helion.Resources.Definitions.Decorate.States;
using Helion.Util;
using Helion.Util.Consoles;
using Helion.Util.Extensions;
using Helion.Util.Timing;
using Helion.World;
using Helion.World.Entities.Definition.Properties;
using Helion.World.Entities.Definition.States;
using Helion.World.Entities.Inventories;
using Helion.World.Entities.Inventories.Powerups;
using Helion.World.StatusBar;
using static Helion.Render.Common.RenderDimensions;

namespace Helion.Layer.Worlds
{
    public partial class WorldLayer
    {
        private const int DebugFontSize = 16;
        private const int LeftOffset = 1;
        private const int TopOffset = 1;
        private const int MessageSpacing = 1;
        private const int FpsMessageSpacing = 2;
        private const long MaxVisibleTimeNanos = 4 * 1000L * 1000L * 1000L;
        private const long FadingNanoSpan = 350L * 1000L * 1000L;
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
            DrawBottomHud(hud, topRightY, hudContext);
            DrawHudEffectsDeprecated(hud);
            DrawRecentConsoleMessages(hud);
        }

        [Obsolete("Will be moved to a post-processing step in a shader")]
        private void DrawHudEffectsDeprecated(IHudRenderContext hud)
        {
            IPowerup? powerup = Player.Inventory.PowerupEffectColor;
            if (powerup?.DrawColor != null && powerup.DrawPowerupEffect)
                hud.Clear(powerup.DrawColor.Value, powerup.DrawAlpha);
            
            if (Player.BonusCount > 0)
                hud.Clear(PickupColor, 0.2f);

            if (Player.DamageCount > 0)
                hud.Clear(DamageColor, Player.DamageCount * 0.01f);
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
        
        private void DrawBottomHud(IHudRenderContext hud, int topRightY, HudRenderContext hudContext)
        {
            if (Player.AnimationWeapon != null && !hudContext.DrawAutomap)
            {
                hudContext.DrawInvul = Player.DrawInvulnerableColorMap();
                
                // Doom pushes the gun sprite up when the status bar is showing
                int yOffset = m_config.Hud.StatusBarSize == StatusBarSizeType.Full ? 16 : 0;
                DrawHudWeapon(hud, Player.AnimationWeapon.FrameState, yOffset);
                if (Player.AnimationWeapon.FlashState.Frame.BranchType != ActorStateBranch.Stop)
                    DrawHudWeapon(hud, Player.AnimationWeapon.FlashState, yOffset);

                hudContext.DrawInvul = false;
            }
        
            if (!hudContext.DrawAutomap)
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
                
                offset = TranslateDoomOffset(offset, dimension);

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

            hud.FillBox((horizontal.X, horizontal.Y, horizontal.X + (Length * 2), horizontal.Y + (HalfWidth * 2)), Color.LawnGreen);
            hud.FillBox((vertical.X, vertical.Y, vertical.X + (HalfWidth * 2), vertical.Y + (Length * 2)), Color.LawnGreen);
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
            x += medkitArea.Width + m_padding;

            string health = Math.Max(0, Player.Health).ToString();
            hud.Text(health, LargeHudFont, m_fontHeight, (x, y), out Dimension healthArea, both: Align.BottomLeft);
            x += healthArea.Width + (m_padding * 3);
            
            DrawFace(hud, (x, y), Align.BottomLeft, true);
            
            if (Player.Armor > 0)
            {
                x = m_padding;
                y -= medkitArea.Height + (m_padding * 2);
            
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
            const int FullHudFaceX = 149;
            const int FullHudFaceY = 170;
            const string StatusBar = "STBAR";
            const string StatusBackground = "W94_1";
            
            hud.DoomVirtualResolution(() =>
            {
                if (!hud.Textures.TryGet(StatusBackground, out var backgroundHandle) ||
                    !hud.Textures.TryGet(StatusBar, out var barHandle))
                {
                    return;
                }

                int xOffset = 0;
                int yOffset = -barHandle.Dimension.Height + 1;
                int width = backgroundHandle.Dimension.Width;

                while (xOffset < hud.Width)
                {
                    hud.Image(StatusBackground, (xOffset, yOffset), Align.BottomLeft);
                    xOffset += width;
                }
            });

            hud.DoomVirtualResolution(() =>
            {
                hud.Image(StatusBar, (0, 0), both: Align.BottomLeft);
                DrawFullHudHealthArmorAmmo(hud);
                DrawFullHudWeaponSlots(hud);
                DrawFace(hud, (FullHudFaceX, FullHudFaceY));
                DrawFullHudKeys(hud);
                DrawFullTotalAmmo(hud);
            }, ResolutionScale.Center);
        }

        private void DrawFullHudHealthArmorAmmo(IHudRenderContext hud)
        {
            const int OffsetY = 171;
            const int FontSize = 15;
            
            if (Player.Weapon != null && Player.Weapon.Definition.Properties.Weapons.AmmoType != "")
            {
                int ammoAmount = Player.Inventory.Amount(Player.Weapon.Definition.Properties.Weapons.AmmoType);
                string ammo = Math.Clamp(ammoAmount, 0, 999).ToString();
                hud.Text(ammo, LargeHudFont, FontSize, (43, OffsetY), anchor: Align.TopRight);
            }

            string health = $"{Math.Clamp(Player.Health, 0, 999)}%";
            hud.Text(health, LargeHudFont, FontSize, (102, OffsetY), anchor: Align.TopRight);
            
            string armor = $"{Math.Clamp(Player.Armor, 0, 999)}%";
            hud.Text(armor, LargeHudFont, FontSize, (233, OffsetY), anchor: Align.TopRight);
        }
        
        private void DrawFullHudWeaponSlots(IHudRenderContext hud)
        {
            hud.Image("STARMS", (104, 0), both: Align.BottomLeft);

            for (int slot = 2; slot <= 7; slot++)
                DrawWeaponNumber(hud, slot);
        }
        
        private void DrawWeaponNumber(IHudRenderContext hud, int slot)
        {
            Weapon? weapon = Player.Inventory.Weapons.GetWeapon(Player, slot, 0);
            if (slot == 3 && weapon == null)
                weapon = Player.Inventory.Weapons.GetWeapon(Player, slot, 1);

            string numberImage = (weapon != null ? "STYSNUM" : "STGNUM") + slot;

            hud.Image(numberImage, slot switch
            {
                2 => (111, 172),
                3 => (123, 172),
                4 => (135, 172),
                5 => (111, 182),
                6 => (123, 182),
                7 => (135, 182),
                _ => throw new Exception($"Bad slot index: {slot}")
            });
        }
        
        private void DrawFullHudKeys(IHudRenderContext hud)
        {
            const int OffsetX = 239;

            foreach (InventoryItem key in Player.Inventory.GetKeys())
            {
                DrawKeyIfOwned(hud, key, "BlueSkull", "BlueCard", OffsetX, 171);
                DrawKeyIfOwned(hud, key, "YellowSkull", "YellowCard", OffsetX, 181);
                DrawKeyIfOwned(hud, key, "RedSkull", "RedCard", OffsetX, 191);
            }
        }
        
        private void DrawKeyIfOwned(IHudRenderContext hud, InventoryItem key, string skullKeyName, 
            string keyName, int x, int y)
        {
            string imageName = key.Definition.Properties.Inventory.Icon;

            foreach (string name in new[] { skullKeyName, keyName })
            {
                if (key.Definition.Name.EqualsIgnoreCase(name) && hud.Textures.HasImage(imageName))
                {
                    hud.Image(imageName, (x, y));
                    break;
                }
            }
        }
        
        private void DrawFullTotalAmmo(IHudRenderContext hud)
        {
            const int FontSize = 6;
            const string YellowFontName = "HudYellowNumbers";

            bool backpack = Player.Inventory.HasItemOfClass(Inventory.BackPackBaseClassName);

            DrawFullTotalAmmoText("Clip", backpack ? 400 : 200, 173);
            DrawFullTotalAmmoText("Shell", backpack ? 100 : 50, 179);
            DrawFullTotalAmmoText("RocketAmmo", backpack ? 100 : 50, 185);
            DrawFullTotalAmmoText("Cell", backpack ? 600 : 300, 191);

            void DrawFullTotalAmmoText(string ammoName, int maxAmmo, int y)
            {
                int ammo = Player.Inventory.Amount(ammoName);
                hud.Text(ammo.ToString(), YellowFontName, FontSize, (287, y), anchor: Align.TopRight);
                hud.Text(maxAmmo.ToString(), YellowFontName, FontSize, (315, y), anchor: Align.TopRight);
            }
        }
        
        private void DrawRecentConsoleMessages(IHudRenderContext hud)
        {
            const int MaxHudMessages = 4;
        
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
            foreach (ConsoleMessage msg in m_console.Messages)
            {
                if (messagesDrawn >= MaxHudMessages || MessageTooOldToDraw(msg, World, m_console))
                    break;

                long timeSinceMessage = currentNanos - msg.TimeNanos;
                if (timeSinceMessage > MaxVisibleTimeNanos)
                    break;

                messages.Push((msg.Message, CalculateFade(timeSinceMessage)));
                messagesDrawn++;
            }

            foreach ((ColoredString message, float alpha) in messages)
            {
                hud.Text(message, SmallHudFont, 8, (LeftOffset, offsetY),
                    out Dimension drawArea, window: Align.TopLeft, scale: m_scale, alpha: alpha);
                offsetY += drawArea.Height + MessageSpacing;
            }
        }
        
        private static bool MessageTooOldToDraw(ConsoleMessage msg, WorldBase world, HelionConsole console)
        {
            return msg.TimeNanos < world.CreationTimeNanos || msg.TimeNanos < console.LastClosedNanos;
        }
        
        private static float CalculateFade(long timeSinceMessage)
        {
            const long OpaqueNanoRange = MaxVisibleTimeNanos - FadingNanoSpan;

            if (timeSinceMessage < OpaqueNanoRange)
                return 1.0f;

            double fractionIntoFadeRange = (double)(timeSinceMessage - OpaqueNanoRange) / FadingNanoSpan;
            return 1.0f - (float)fractionIntoFadeRange;
        }
    }
}
