using System;
using System.Collections.Generic;
using System.Drawing;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Render.Common;
using Helion.Render.Common.Context;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Render.OpenGL;
using Helion.Render.OpenGL.Util;
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
using Helion.World.Entities.Players;
using Helion.World.StatusBar;
using static Helion.Render.Common.RenderDimensions;

namespace Helion.Layer.Worlds;

public partial class WorldLayer
{
    private const int DebugFontSize = 8;
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
    private int m_infoFontSize = DebugFontSize;
    private Dimension m_viewport;
    private readonly List<(string message, float alpha)> m_messages = new();

    private void DrawHud(HudRenderContext hudContext, IHudRenderContext hud, bool automapVisible)
    {
        m_scale = (float)m_config.Hud.Scale.Value;
        m_infoFontSize = Math.Max((int)(m_scale * DebugFontSize), 12);
        m_padding = (int)(4 * m_scale);
        m_fontHeight = (int)(16 * m_scale);
        m_viewport = hud.Dimension;

        DrawFPS(hud, out int topRightY);
        DrawPosition(hud, ref topRightY);
        DrawStatInfo(hud, automapVisible, (0, topRightY + m_padding), ref topRightY);
        DrawBottomHud(hud, topRightY, hudContext);
        DrawHudEffects(hud);
        DrawRecentConsoleMessages(hud);
        DrawPause(hud);
    }

    private void DrawPause(IHudRenderContext hud)
    {
        if (!Player.World.DrawPause)
            return;

        hud.DoomVirtualResolution(() =>
        {
            hud.Image("M_PAUSE", (0, 8), both: Align.TopMiddle);
        });
    }

    private static readonly string[] StatLabels = new string[] { "Kills: ", "Items: ", "Secrets: " };
    private readonly string[] StatValues = new string[] { string.Empty, string.Empty, string.Empty };

    private void DrawStatInfo(IHudRenderContext hud, bool automapVisible, Vec2I start, ref int topRightY)
    {
        if (!m_config.Hud.ShowStats && !automapVisible)
            return;

        start.X = -m_padding;
        Vec2I labelPos = start;
        int maxLabelWidth = 0;
        int maxValueWidth = 0;
        var align = Align.TopRight;

        StatValues[0] = $"{World.LevelStats.KillCount} / {World.LevelStats.TotalMonsters}";
        StatValues[1] = $"{World.LevelStats.ItemCount} / {World.LevelStats.TotalItems}";
        StatValues[2] = $"{World.LevelStats.SecretCount} / {World.LevelStats.TotalSecrets}";

        for (int i = 0; i < StatLabels.Length; i++)
        {
            maxLabelWidth = Math.Max(hud.MeasureText(StatLabels[i], ConsoleFont, m_infoFontSize).Width, maxLabelWidth);
            maxValueWidth = Math.Max(hud.MeasureText(StatValues[i], ConsoleFont, m_infoFontSize).Width, maxValueWidth);
        }

        labelPos.X = -(maxValueWidth + m_padding);

        for (int i = 0; i < StatLabels.Length; i++)
        {
            hud.Text(StatLabels[i], ConsoleFont, m_infoFontSize, labelPos, out Dimension labelDim,
                TextAlign.Right, both: align);
            labelPos.Y += labelDim.Height;
        }

        labelPos = start;
        hud.Text(StatValues[0], ConsoleFont, m_infoFontSize, labelPos, out Dimension dim,
            TextAlign.Right, both: align, color: GetStatColor(World.LevelStats.KillCount, World.LevelStats.TotalMonsters));
        labelPos.Y += dim.Height;
        hud.Text(StatValues[1], ConsoleFont, m_infoFontSize, labelPos, out dim,
            TextAlign.Right, both: align, color: GetStatColor(World.LevelStats.ItemCount, World.LevelStats.TotalItems));
        labelPos.Y += dim.Height;
        hud.Text(StatValues[2], ConsoleFont, m_infoFontSize, labelPos, out dim,
            TextAlign.Right, both: align, color: GetStatColor(World.LevelStats.SecretCount, World.LevelStats.TotalSecrets));
        labelPos.Y += dim.Height + m_padding;

        hud.Text(TimeSpan.FromSeconds(World.LevelTime / 35).ToString(), ConsoleFont, m_infoFontSize, labelPos, out dim,
            TextAlign.Right, both: align, color: Color.White);
        labelPos.Y += dim.Height;

        topRightY = labelPos.Y;
    }

    private void DrawHudEffects(IHudRenderContext hud)
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

        if (!m_config.Hud.ShowFPS)
            return;

        DrawFpsValue("", m_fpsTracker.AverageFramesPerSecond, ref topRightY);
        DrawFpsValue("Max ", m_fpsTracker.MaxFramesPerSecond, ref topRightY);
        DrawFpsValue("Min ", m_fpsTracker.MinFramesPerSecond, ref topRightY);

        void DrawFpsValue(string prefix, double fps, ref int y)
        {
            string avgFps = $"{prefix}FPS: {(int)Math.Round(fps)}";
            hud.Text(avgFps, ConsoleFont, m_infoFontSize, (-m_padding, y), out Dimension avgArea,
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
        DrawCoordinate('A', Player.AngleRadians * 180 / Math.PI, ref topRightY);

        void DrawCoordinate(char axis, double position, ref int y)
        {
            hud.Text($"{axis}: {Math.Round(position, 4)}", ConsoleFont, m_infoFontSize,
                (-m_padding, y), out Dimension area, TextAlign.Right, both: Align.TopRight,
                color: Color.White);
            y += area.Height + FpsMessageSpacing;
        }
    }

    private void DrawBottomHud(IHudRenderContext hud, int topRightY, HudRenderContext hudContext)
    {
        if (Player.AnimationWeapon != null && !m_drawAutomap)
        {
            hudContext.DrawInvul = Player.DrawInvulnerableColorMap();

            // Doom pushes the gun sprite up when the status bar is showing
            int yOffset = m_config.Hud.StatusBarSize == StatusBarSizeType.Full ? 16 : 0;
            DrawHudWeapon(hud, Player.AnimationWeapon.FrameState, yOffset);
            if (Player.AnimationWeapon.FlashState.Frame.BranchType != ActorStateBranch.Stop)
                DrawHudWeapon(hud, Player.AnimationWeapon.FlashState, yOffset);

            hudContext.DrawInvul = false;
        }

        if (!m_drawAutomap && m_config.Hud.Crosshair)
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

    private static short GetLightLevel(Player player)
    {
        if (player.Sector.TransferHeights == null)
            return player.Sector.LightLevel;

        return player.Sector.GetRenderSector(player.Sector, player.GetViewPosition().Z).LightLevel;
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
            int extraLight = Player.GetExtraLightRender();
            lightLevel = GetLightLevel(Player);
            if (m_config.Render.LightDropoff)
                lightLevel = GLHelper.DoomLightLevelToColor(lightLevel, extraLight);
            else
                lightLevel = (int)(GLHelper.DoomLightLevelToColorStatic(lightLevel, extraLight) * 255);
        }

        Color lightLevelColor = Color.FromArgb(lightLevel, lightLevel, lightLevel);
        string sprite = frameState.Frame.Sprite + (char)(frameState.Frame.Frame + 'A') + "0";

        if (!hud.Textures.TryGet(sprite, out var handle))
            return;

        hud.DoomVirtualResolution(() =>
        {
            Vec2I offset = handle.Offset;
            float tickFraction = m_lastTickInfo.Fraction;

            offset.Y += yOffset;
            Vec2I weaponOffset = Player.PrevWeaponOffset.Interpolate(Player.WeaponOffset, tickFraction).Int + 
                Player.PrevBobOffset.Interpolate(Player.BobOffset, tickFraction).Int;

            float alpha = 1.0f;
            IPowerup? powerup = Player.Inventory.GetPowerup(PowerupType.Invisibility);
            if (powerup != null && powerup.DrawPowerupEffect)
                alpha = 0.3f;

            offset = TranslateDoomOffset(offset);
            hud.Image(sprite, offset + weaponOffset, color: lightLevelColor, alpha: alpha);
        });
    }

    private void DrawCrosshair(IHudRenderContext hud)
    {
        int Width = Math.Max((int)(1 * m_scale), 1);
        int HalfWidth = Math.Max(Width / 2, 1);
        int Length = (int)(5 * m_scale);

        Color color = Player.CrosshairTarget.Entity == null ? Color.LawnGreen : Color.Red;
        int crosshairLength = Player.CrosshairTarget.Entity == null ? Length : (int)(Length * 0.8f);
        int totalCrosshairLength = crosshairLength * 2;
        if (Width == 1)
            totalCrosshairLength += 1;

        Vec2I center = m_viewport.Vector / 2;
        Vec2I horizontal = center - new Vec2I(crosshairLength, HalfWidth);
        Vec2I vertical = center - new Vec2I(HalfWidth, crosshairLength);

        if (Width == 1)
        {
            vertical.X += 1;
            horizontal.Y += 1;
        }
        else
        {
            HalfWidth *= 2;
        }            

        hud.FillBox((horizontal.X, horizontal.Y, horizontal.X + totalCrosshairLength, horizontal.Y + HalfWidth), color);
        hud.FillBox((vertical.X, vertical.Y, vertical.X + HalfWidth, vertical.Y + totalCrosshairLength), color);
    }

    private void DrawMinimalStatusBar(IHudRenderContext hud, int topRightY)
    {
        DrawMinimalHudHealthAndArmor(hud);
        DrawMinimalHudKeys(hud, topRightY);
        DrawMinimalHudAmmo(hud);
    }


    private static Color GetStatColor(int current, int total)
    {
        if (current >= total)
            return Color.LightGreen;
        return Color.White;
    }

    private void DrawMinimalHudHealthAndArmor(IHudRenderContext hud)
    {
        const string Medkit = "MEDIA0";

        // We will draw the medkit slightly higher so it looks like it
        // aligns with the font.
        int x = m_padding;
        int y = -m_padding;

        hud.Image(Medkit, (x, y), out var medkitArea, both: Align.BottomLeft, scale: m_scale);
        x += medkitArea.Width + m_padding;

        string health = Math.Max(0, Player.Health).ToString();
        hud.Text(health, LargeHudFont, m_fontHeight, (x, y), both: Align.BottomLeft);

        // This is to make sure the face never moves (even if the health changes).
        x += hud.MeasureText("9999", LargeHudFont, m_fontHeight).Width;
        int highestX = x;

        DrawFace(hud, (x, y), out HudBox faceArea, Align.BottomLeft, true);

        if (Player.Armor > 0)
        {
            x = m_padding;
            y -= medkitArea.Height + (m_padding * 2);

            EntityProperties? armorProp = Player.ArmorProperties;
            if (armorProp != null && hud.Textures.HasImage(armorProp.Inventory.Icon))
            {
                hud.Image(armorProp.Inventory.Icon, (x, y), out var armorArea, both: Align.BottomLeft, scale: m_scale);
                x += armorArea.Width + m_padding;
            }

            hud.Text(Player.Armor.ToString(), LargeHudFont, m_fontHeight, (x, y), both: Align.BottomLeft);
        }
    }

    private void DrawFace(IHudRenderContext hud, Vec2I origin, out HudBox area, Align? both = null, bool scaleDraw = false)
    {
        hud.Image(Player.StatusBar.GetFacePatch(), origin, out area, both: both, scale: scaleDraw ? m_scale : 1.0f);
    }

    private void DrawMinimalHudKeys(IHudRenderContext hud, int y)
    {
        List<InventoryItem> keys = Player.Inventory.GetKeys();
        y += m_padding;

        for (int i = 0; i < keys.Count; i++)
        {
            InventoryItem key = keys[i];
            string icon = key.Definition.Properties.Inventory.Icon;
            if (!hud.Textures.HasImage(icon))
                continue;

            hud.Image(icon, (-m_padding, y), out HudBox drawArea, both: Align.TopRight, scale: m_scale);
            y += drawArea.Height + m_padding;
        }
    }

    private void DrawMinimalHudAmmo(IHudRenderContext hud)
    {
        var weapon = Player.AnimationWeapon;
        if (weapon == null)
            return;

        string ammoType = weapon.Definition.Properties.Weapons.AmmoType;
        if (ammoType.Length == 0)
            return;

        int x = -m_padding;
        int y = -m_padding;

        int ammo = Player.Inventory.Amount(ammoType);
        hud.Text(ammo.ToString(), LargeHudFont, m_fontHeight, (x, y), out Dimension textRect,
            both: Align.BottomRight);

        x -= textRect.Width + m_padding;
        if (weapon.AmmoSprite.Length <= 0 || !hud.Textures.TryGet(weapon.AmmoSprite, out var handle))
            return;

        x -= (int)(handle.Dimension.Width * m_scale);
        hud.Image(weapon.AmmoSprite, (x, y), both: Align.BottomRight, scale: m_scale);
    }

    private void DrawFullStatusBar(IHudRenderContext hud)
    {
        const int FullHudFaceX = 149;
        const int FullHudFaceY = 170;
        const string StatusBar = "STBAR";

        if (hud.Textures.TryGet(StatusBar, out var statusBarHandle))
            DrawStatusBarBackground(hud, statusBarHandle);

        hud.RenderStatusBar(StatusBar);

        hud.DoomVirtualResolution(() =>
        {
            DrawFullHudHealthArmorAmmo(hud);
            DrawFullHudWeaponSlots(hud);
            DrawFace(hud, (FullHudFaceX, FullHudFaceY), out var _);
            DrawFullHudKeys(hud);
            DrawFullTotalAmmo(hud);
        });
    }

    private void DrawStatusBarBackground(IHudRenderContext hud, Render.Common.Textures.IRenderableTextureHandle barHandle)
    {
        if (!hud.Textures.TryGet(m_config.Hud.BackgroundTexture, out var backgroundHandle))
            return;

        hud.DoomVirtualResolution(() =>
        {
            // NOTE: This is a terrible hack. The code to convert from
            // window space into the custom screen space, then figure out
            // the gutter, then translate back into the window space, can
            // be solved for all possible cases for the foreseeable future
            // by overdrawing 1000 pixels (in 320x200 resolution) on each
            // side. The only way this would become visible is if there
            // was a widescreen that was like 9280x1280. If this ever is
            // a thing, a proper fix can be added.
            const int Overflow = 1000;
            int xOffset = -Overflow;
            int yOffset = -barHandle.Dimension.Height + 1;
            int width = backgroundHandle.Dimension.Width;
            int iterations = ((Overflow + 320 + Overflow) / backgroundHandle.Dimension.Width) + 1;

            for (int i = 0; i < iterations; i++)
            {
                hud.Image(m_config.Hud.BackgroundTexture, (xOffset, yOffset), Align.BottomLeft);
                xOffset += width;
            }
        }, ResolutionScale.None);
    }

    private void DrawFullHudHealthArmorAmmo(IHudRenderContext hud)
    {
        const int OffsetY = 171;
        const int FontSize = 15;

        var weapon = Player.AnimationWeapon;
        if (weapon != null && weapon.Definition.Properties.Weapons.AmmoType.Length > 0)
        {
            int ammoAmount = Player.Inventory.Amount(weapon.Definition.Properties.Weapons.AmmoType);
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
        var keys = Player.Inventory.GetKeys();

        for (int i = 0; i < keys.Count; i++)
        {
            InventoryItem key = keys[i];
            DrawKeyIfOwned(hud, key, "BlueSkull", "BlueCard", OffsetX, 171);
            DrawKeyIfOwned(hud, key, "YellowSkull", "YellowCard", OffsetX, 181);
            DrawKeyIfOwned(hud, key, "RedSkull", "RedCard", OffsetX, 191);
        }
    }

    private static void DrawKeyIfOwned(IHudRenderContext hud, InventoryItem key, string skullKeyName,
        string keyName, int x, int y)
    {
        string imageName = key.Definition.Properties.Inventory.Icon;

        if (key.Definition.Name.EqualsIgnoreCase(skullKeyName) && hud.Textures.HasImage(imageName))
        {
            hud.Image(imageName, (x, y));
            return;
        }

        if (key.Definition.Name.EqualsIgnoreCase(keyName) && hud.Textures.HasImage(imageName))
            hud.Image(imageName, (x, y));
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

        LinkedListNode<ConsoleMessage>? node = m_console.Messages.First;
        while (node != null)
        {
            ConsoleMessage msg = node.Value;
            node = node.Next;

            if (messagesDrawn >= MaxHudMessages || MessageTooOldToDraw(msg, World, m_console))
                break;

            long timeSinceMessage = currentNanos - msg.TimeNanos;
            if (timeSinceMessage > MaxVisibleTimeNanos)
                break;

            m_messages.Add((msg.Message, CalculateFade(timeSinceMessage)));
            messagesDrawn++;
        }

        for (int i = m_messages.Count - 1; i >= 0; i--)
        {
            hud.Text(m_messages[i].message, SmallHudFont, 8, (LeftOffset, offsetY),
                out Dimension drawArea, window: Align.TopLeft, scale: m_scale, alpha: m_messages[i].alpha);
            offsetY += drawArea.Height + MessageSpacing;
        }

        m_messages.Clear();
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
