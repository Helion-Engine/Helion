using System;
using System.Collections.Generic;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Render.Common;
using Helion.Render.Common.Context;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Render.Common.Textures;
using Helion.Render.OpenGL.Texture.Fonts;
using Helion.Render.OpenGL.Util;
using Helion.Resources;
using Helion.Resources.Definitions.Decorate.States;
using Helion.Resources.Definitions.MapInfo;
using Helion.Strings;
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
using Helion.World.Geometry.Sectors;
using Helion.World.StatusBar;
using static Helion.Render.Common.RenderDimensions;

namespace Helion.Layer.Worlds;

public partial class WorldLayer
{
    private const int MapFontSize = 12;
    private const int DebugFontSize = 8;
    private const int LeftOffset = 1;
    private const int TopOffset = 1;
    private const int MessageSpacing = 1;
    private const int FpsMessageSpacing = 2;
    private const long MaxVisibleTimeNanos = 4 * 1000L * 1000L * 1000L;
    private const long FadingNanoSpan = 350L * 1000L * 1000L;
    private static readonly Color PickupColor = (255, 255, 128);
    private static readonly Color DamageColor = (255, 0, 0);
    private static readonly string SmallHudFont = Constants.Fonts.Small;
    private static readonly string LargeHudFont = Constants.Fonts.LargeHud;
    private static readonly string ConsoleFont = "Console";
    private int m_fontHeight = 16;
    private int m_padding = 4;
    private float m_scale = 1.0f;
    private int m_infoFontSize = DebugFontSize;
    private int m_mapHeaderFontSize = MapFontSize;
    private Dimension m_viewport;
    private readonly List<(string message, float alpha)> m_messages = new();
    private readonly Action<HudDrawWeapon> m_virtualDrawHudWeaponAction;

    private int m_healthWidth;
    private string m_weaponSprite = StringBuffer.GetStringExact(6);
    private string m_weaponFlashSprite = StringBuffer.GetStringExact(6);
    private SpanString m_weaponSpriteSpan = new("123456");
    private SpanString m_weaponFlashSpriteSpan = new("123456");

    private SpanString m_healthString = new();
    private SpanString m_armorString = new();
    private SpanString m_ammoString = new();
    private SpanString m_maxAmmoString = new();

    private RenderableString m_renderHealthString;
    private RenderableString m_renderArmorString;
    private RenderableString m_renderAmmoString;

    private SpanString m_fpsString = new();
    private SpanString m_fpsMinString = new();
    private SpanString m_fpsMaxString = new();

    private RenderableString m_renderFpsString;
    private RenderableString m_renderFpsMinString;
    private RenderableString m_renderFpsMaxString;

    private SpanString m_killString = new();
    private SpanString m_itemString = new();
    private SpanString m_secretString = new();
    private SpanString m_timeString = new();

    private RenderableString m_renderKillLabel;
    private RenderableString m_renderItemLabel;
    private RenderableString m_renderSecretLabel;
    private RenderableString m_renderKillString;
    private RenderableString m_renderItemString;
    private RenderableString m_renderSecretString;
    private RenderableString m_renderTimeString;

    private static readonly string[] StatLabels = new string[] { "Kills: ", "Items: ", "Secrets: " };
    private readonly SpanString[] StatValues;
    private readonly RenderableString[] RenderableStatLabels;
    private readonly RenderableString[] RenderableStatValues;

    private readonly record struct HudDrawWeapon(IHudRenderContext Hud, FrameState FrameState, int yOffset, bool Flash);

    private void DrawHud(HudRenderContext hudContext, IHudRenderContext hud, bool automapVisible)
    {
        m_scale = (float)m_config.Hud.Scale.Value;
        m_infoFontSize = Math.Max((int)(m_scale * DebugFontSize), 12);
        m_mapHeaderFontSize = Math.Max((int)(m_scale * MapFontSize), 20);
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

        if (automapVisible)
            DrawMapHeader(hud);
    }

    private void DrawMapHeader(IHudRenderContext hud)
    {
        string text = World.MapInfo.GetDisplayNameWithPrefix(World.ArchiveCollection);
        Vec2I pos = new((int)(2 * m_scale), (int)(2 * m_scale));
        hud.Text(text, SmallHudFont, m_mapHeaderFontSize, pos);
    }

    private void DrawPause(IHudRenderContext hud)
    {
        if (!WorldStatic.World.DrawPause)
            return;

        hud.DoomVirtualResolution(m_virtualDrawPauseAction, hud);
    }

    private void VirtualDrawPause(IHudRenderContext hud)
    {
        hud.Image("M_PAUSE", (0, 8), both: Align.TopMiddle);
    }

    private void DrawStatInfo(IHudRenderContext hud, bool automapVisible, Vec2I start, ref int topRightY)
    {
        if (!m_config.Hud.ShowStats && !automapVisible)
            return;

        start.X = -m_padding;
        Vec2I labelPos = start;
        int maxLabelWidth = 0;
        int maxValueWidth = 0;
        var align = Align.TopRight;

        if (HasTicks)
        {
            m_killString.Clear();
            m_itemString.Clear();
            m_secretString.Clear();

            StatValues[0] = AppendStatString(m_killString, World.LevelStats.KillCount, World.LevelStats.TotalMonsters);
            StatValues[1] = AppendStatString(m_itemString, World.LevelStats.ItemCount, World.LevelStats.TotalItems);
            StatValues[2] = AppendStatString(m_secretString, World.LevelStats.SecretCount, World.LevelStats.TotalSecrets);

            for (int i = 0; i < RenderableStatLabels.Length; i++)
                RenderableStatLabels[i] = SetRenderableString(StatLabels[i], RenderableStatLabels[i], ConsoleFont, m_infoFontSize);

            RenderableStatValues[0] = SetRenderableString(m_killString.AsSpan(), m_renderKillString, ConsoleFont, m_infoFontSize,
                GetStatColor(World.LevelStats.KillCount, World.LevelStats.TotalMonsters));
            RenderableStatValues[1] = SetRenderableString(m_itemString.AsSpan(), m_renderItemString, ConsoleFont, m_infoFontSize,
                GetStatColor(World.LevelStats.ItemCount, World.LevelStats.TotalItems));
            RenderableStatValues[2] = SetRenderableString(m_secretString.AsSpan(), m_renderSecretString, ConsoleFont, m_infoFontSize,
                GetStatColor(World.LevelStats.SecretCount, World.LevelStats.TotalSecrets));
        }

        for (int i = 0; i < RenderableStatValues.Length; i++)
        {
            maxLabelWidth = Math.Max(RenderableStatLabels[i].DrawArea.Width, maxLabelWidth);
            maxValueWidth = Math.Max(RenderableStatValues[i].DrawArea.Width, maxValueWidth);
        }

        labelPos.X = -(maxValueWidth + m_padding);

        for (int i = 0; i < RenderableStatLabels.Length; i++)
        {
            var str = RenderableStatLabels[i];
            hud.Text(RenderableStatLabels[i], labelPos, both: align);
            labelPos.Y += str.DrawArea.Height;
        }

        labelPos = start;
        for (int i = 0; i < RenderableStatValues.Length; i++)
        {
            var str = RenderableStatValues[i];
            hud.Text(RenderableStatValues[i], labelPos, both: align);
            labelPos.Y += str.DrawArea.Height;
        }
        labelPos.Y += m_padding;

        if (HasTicks)
        {
            TimeSpan ts = TimeSpan.FromSeconds(World.LevelTime / 35);
            m_timeString.Clear();
            m_timeString.Append((int)ts.Hours, 2);
            m_timeString.Append(':');
            m_timeString.Append((int)ts.Minutes, 2);
            m_timeString.Append(':');
            m_timeString.Append((int)ts.Seconds, 2);

            SetRenderableString(m_timeString.AsSpan(), m_renderTimeString, ConsoleFont, m_infoFontSize);
        }

        hud.Text(m_renderTimeString, labelPos, both: align);
        labelPos.Y += m_renderTimeString.DrawArea.Height;

        topRightY = labelPos.Y;
    }

    private static SpanString AppendStatString(SpanString str, int current, int max)
    {
        str.Append(current);
        str.Append(" / ");
        str.Append(max);
        return str;
    }

    private void DrawHudEffects(IHudRenderContext hud)
    {
        if (!WorldStatic.World.DrawHud)
            return;

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

        if (!m_config.Hud.ShowFPS && !m_config.Hud.ShowMinMaxFPS)
            return;

        if (m_config.Hud.ShowFPS)
            DrawFpsValue(hud, "", m_fpsTracker.AverageFramesPerSecond, ref topRightY, m_fpsString, m_renderFpsString);

        if (m_config.Hud.ShowMinMaxFPS)
        {
            DrawFpsValue(hud, "Max ", m_fpsTracker.MaxFramesPerSecond, ref topRightY, m_fpsMaxString, m_renderFpsMaxString);
            DrawFpsValue(hud, "Min ", m_fpsTracker.MinFramesPerSecond, ref topRightY, m_fpsMinString, m_renderFpsMinString);
        }
    }

    void DrawFpsValue(IHudRenderContext hud, string prefix, double fps, ref int y, SpanString str, RenderableString renderableString)
    {
        str.Clear();
        str.Append(prefix);
        str.Append("FPS: ");
        str.Append((int)Math.Round(fps));

        SetRenderableString(str.AsSpan(), renderableString, ConsoleFont, m_infoFontSize);
        hud.Text(renderableString, (-m_padding, y), both: Align.TopRight);

        y += renderableString.DrawArea.Height + FpsMessageSpacing;
    }

    private void DrawPosition(IHudRenderContext hud, ref int topRightY)
    {
        if (!Player.Cheats.IsCheatActive(Helion.World.Cheats.CheatType.ShowPosition))
            return;

        DrawCoordinate(hud, 'X', Player.Position.X, ref topRightY);
        DrawCoordinate(hud, 'Y', Player.Position.Y, ref topRightY);
        DrawCoordinate(hud, 'Z', Player.Position.Z, ref topRightY);
        DrawCoordinate(hud, 'A', Player.AngleRadians * 180 / Math.PI, ref topRightY);
    }

    void DrawCoordinate(IHudRenderContext hud, char axis, double position, ref int y)
    {
        hud.Text($"{axis}: {Math.Round(position, 4)}", ConsoleFont, m_infoFontSize,
            (-m_padding, y), out Dimension area, TextAlign.Right, both: Align.TopRight,
            color: Color.White);
        y += area.Height + FpsMessageSpacing;
    }

    private void DrawBottomHud(IHudRenderContext hud, int topRightY, HudRenderContext hudContext)
    {
        if (!WorldStatic.World.DrawHud)
            return;

        if (Player.AnimationWeapon != null && !m_drawAutomap)
        {
            hudContext.DrawInvul = Player.DrawInvulnerableColorMap();

            // Doom pushes the gun sprite up when the status bar is showing
            int yOffset = m_config.Hud.StatusBarSize == StatusBarSizeType.Full ? m_config.Hud.FullSizeGunOffset : 0;
            DrawHudWeapon(hud, Player.AnimationWeapon.FrameState, yOffset, flash: false);
            if (Player.AnimationWeapon.FlashState.Frame.BranchType != ActorStateBranch.Stop)
                DrawHudWeapon(hud, Player.AnimationWeapon.FlashState, yOffset, flash: true);

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
        Sector sector = player.Sector;
        return (short)((sector.TransferFloorLightSector.LightLevel + sector.TransferCeilingLightSector.LightLevel) / 2);
    }

    private void DrawHudWeapon(IHudRenderContext hud, FrameState frameState, int yOffset, bool flash)
    {
        hud.DoomVirtualResolution(m_virtualDrawHudWeaponAction, new HudDrawWeapon(hud, frameState, yOffset, flash));
    }

    private void VirtualDrawHudWeapon(HudDrawWeapon hud)
    {
        int lightLevel;

        if (hud.FrameState.Frame.Properties.Bright || Player.DrawFullBright())
        {
            lightLevel = 255;
        }
        else
        {
            int extraLight = Player.GetExtraLightRender();
            lightLevel = GetLightLevel(Player);
            lightLevel = GLHelper.DoomLightLevelToColor(lightLevel, extraLight);
        }

        Color lightLevelColor = ((byte)lightLevel, (byte)lightLevel, (byte)lightLevel);
        string sprite = GetHudWeaponSpriteString(hud);

        if (!hud.Hud.Textures.TryGet(sprite, out var handle, ResourceNamespace.Sprites))
            return;

        Vec2I offset = handle.Offset;
        float tickFraction = m_lastTickInfo.Fraction;

        offset.Y += hud.yOffset;
        Vec2I weaponOffset = Player.PrevWeaponOffset.Interpolate(Player.WeaponOffset, tickFraction).Int +
            Player.PrevBobOffset.Interpolate(Player.BobOffset, tickFraction).Int;

        float alpha = 1.0f;
        IPowerup? powerup = Player.Inventory.GetPowerup(PowerupType.Invisibility);
        if (powerup != null && powerup.DrawPowerupEffect)
            alpha = 0.3f;

        offset = TranslateDoomOffset(offset);
        hud.Hud.Image(sprite, offset + weaponOffset, color: lightLevelColor, alpha: alpha);
    }

    private string GetHudWeaponSpriteString(HudDrawWeapon hud)
    {
        string sprite = hud.Flash ? m_weaponFlashSprite : m_weaponSprite;
        SpanString spriteSpan = hud.Flash ? m_weaponFlashSpriteSpan : m_weaponSpriteSpan;
        int oldLength = spriteSpan.Length;
        spriteSpan.Clear();
        spriteSpan.Append(hud.FrameState.Frame.Sprite);
        spriteSpan.Append((char)(hud.FrameState.Frame.Frame + 'A'));
        spriteSpan.Append('0');

        // This buffer string needs to have the exact length of the sprite. All these lookups are dependent on GetHashCode which changes with string.Length...
        int newLength = spriteSpan.Length;
        if (newLength != oldLength)
        {
            string exactSpriteString = StringBuffer.ToStringExact(sprite);
            if (!ReferenceEquals(exactSpriteString, sprite))
                StringBuffer.FreeString(sprite);
            if (hud.Flash)
                m_weaponFlashSprite = exactSpriteString;
            else
                m_weaponSprite = exactSpriteString;
        }
        else
        {
            StringBuffer.Clear(sprite);
            StringBuffer.Append(sprite, spriteSpan.AsSpan());
        }

        return sprite;
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

    private RenderableString SetRenderableString(ReadOnlySpan<char> charSpan, RenderableString renderableString, string font, int fontSize, Color? drawColor = null)
    {
        if (!HasTicks)
            return renderableString;

        renderableString.Set(World.ArchiveCollection.DataCache, charSpan, GetFontOrDefault(font),
            fontSize, TextAlign.Left, drawColor: drawColor);
        return renderableString;
    }

    private bool HasTicks => m_lastTickInfo.Ticks > 0;

    private void DrawMinimalHudHealthAndArmor(IHudRenderContext hud)
    {
        const string Medkit = "MEDIA0";

        // We will draw the medkit slightly higher so it looks like it
        // aligns with the font.
        int x = m_padding;
        int y = -m_padding;

        hud.Image(Medkit, (x, y), out var medkitArea, both: Align.BottomLeft, scale: m_scale);
        x += medkitArea.Width + m_padding;

        m_healthString.Clear();
        m_healthString.Append(Math.Max(0, Player.Health));

        SetRenderableString(m_healthString.AsSpan(), m_renderHealthString, LargeHudFont, m_fontHeight);
        hud.Text(m_renderHealthString, (x, y), both: Align.BottomLeft);

        // This is to make sure the face never moves (even if the health changes).
        if (HasTicks)
            m_healthWidth = hud.MeasureText("9999", LargeHudFont, m_fontHeight).Width;
        x += m_healthWidth;
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

            m_armorString.Clear();
            m_armorString.Append(Player.Armor);
            SetRenderableString(m_armorString.AsSpan(), m_renderArmorString, LargeHudFont, m_fontHeight);

            hud.Text(m_renderArmorString, (x, y), both: Align.BottomLeft);
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

        if (HasTicks)
        {
            int ammo = Player.Inventory.Amount(ammoType);
            m_ammoString.Clear();
            m_ammoString.Append(ammo);

            SetRenderableString(m_ammoString.AsSpan(), m_renderAmmoString, LargeHudFont, m_fontHeight);
        }

        hud.Text(m_renderAmmoString, (x, y), both: Align.BottomRight);

        x -= m_renderAmmoString.DrawArea.Width + m_padding;
        if (weapon.AmmoSprite.Length <= 0 || !hud.Textures.TryGet(weapon.AmmoSprite, out var handle))
            return;

        x -= (int)(handle.Dimension.Width * m_scale);
        hud.Image(weapon.AmmoSprite, (x, y), both: Align.BottomRight, scale: m_scale);
    }

    private void DrawFullStatusBar(IHudRenderContext hud)
    {
        const string StatusBar = "STBAR";

        if (hud.Textures.TryGet(StatusBar, out var statusBarHandle))
            DrawStatusBarBackground(hud, statusBarHandle);

        hud.RenderStatusBar(StatusBar);

        hud.DoomVirtualResolution(m_virtualDrawFullStatusBarAction, hud);
    }

    private void VirtualDrawFullStatusBar(IHudRenderContext hud)
    {
        const int FullHudFaceX = 149;
        const int FullHudFaceY = 170;
        DrawFullHudHealthArmorAmmo(hud);
        DrawFullHudWeaponSlots(hud);
        DrawFace(hud, (FullHudFaceX, FullHudFaceY), out var _);
        DrawFullHudKeys(hud);
        DrawFullTotalAmmo(hud);
    }

    private readonly record struct HudStatusBarbackground(IHudRenderContext Hud, IRenderableTextureHandle BarHandle, IRenderableTextureHandle BackgroundHandle);

    private void DrawStatusBarBackground(IHudRenderContext hud, IRenderableTextureHandle barHandle)
    {
        if (!hud.Textures.TryGet(m_config.Hud.BackgroundTexture, out var backgroundHandle))
            return;

        hud.DoomVirtualResolution(m_virtualStatusBarBackgroundAction, new HudStatusBarbackground(hud, barHandle, backgroundHandle), 
            ResolutionScale.None);
    }

    private void VirtualStatusBarBackground(HudStatusBarbackground hud)
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
        int yOffset = -hud.BarHandle.Dimension.Height + 1;
        int width = hud.BackgroundHandle.Dimension.Width;
        int iterations = ((Overflow + 320 + Overflow) / hud.BackgroundHandle.Dimension.Width) + 1;

        for (int i = 0; i < iterations; i++)
        {
            hud.Hud.Image(m_config.Hud.BackgroundTexture, (xOffset, yOffset), Align.BottomLeft);
            xOffset += width;
        }
    }

    private void DrawFullHudHealthArmorAmmo(IHudRenderContext hud)
    {
        const int OffsetY = 171;
        const int FontSize = 15;

        var weapon = Player.AnimationWeapon;
        if (weapon != null && weapon.Definition.Properties.Weapons.AmmoType.Length > 0)
        {
            int ammoAmount = Player.Inventory.Amount(weapon.Definition.Properties.Weapons.AmmoType);
            m_ammoString.Clear();
            m_ammoString.Append(Math.Clamp(ammoAmount, 0, 999));

            SetRenderableString(m_ammoString.AsSpan(), m_renderAmmoString, LargeHudFont, FontSize);
            hud.Text(m_renderAmmoString, (43, OffsetY), anchor: Align.TopRight);
        }

        m_healthString.Clear();
        m_healthString.Append(Math.Clamp(Player.Health, 0, 999));
        m_healthString.Append( '%');

        SetRenderableString(m_healthString.AsSpan(), m_renderHealthString, LargeHudFont, FontSize);
        hud.Text(m_renderHealthString, (102, OffsetY), anchor: Align.TopRight);

        m_armorString.Clear();
        m_armorString.Append(Math.Clamp(Player.Armor, 0, 999));
        m_armorString.Append('%');

        SetRenderableString(m_armorString.AsSpan(), m_renderArmorString, LargeHudFont, FontSize);
        hud.Text(m_renderArmorString, (233, OffsetY), anchor: Align.TopRight);
    }

    private void DrawFullHudWeaponSlots(IHudRenderContext hud)
    {
        hud.Image("STARMS", (104, 0), both: Align.BottomLeft);

        for (int slot = 2; slot <= 7; slot++)
            DrawWeaponNumber(hud, slot);
    }

    private string m_weaponNumberTexture = StringBuffer.GetString(16);

    private static readonly string[] HasWeaponImages = new[]
    {
        string.Empty,
        "STYSNUM1",
        "STYSNUM2",
        "STYSNUM3",
        "STYSNUM4",
        "STYSNUM5",
        "STYSNUM6",
        "STYSNUM7",
    };

    private static readonly string[] WeaponImages = new[]
    {
        string.Empty,
        "STGNUM1",
        "STGNUM2",
        "STGNUM3",
        "STGNUM4",
        "STGNUM5",
        "STGNUM6",
        "STGNUM7",
    };

    private void DrawWeaponNumber(IHudRenderContext hud, int slot)
    {
        Weapon? weapon = Player.Inventory.Weapons.GetWeapon(Player, slot, 0);
        if (slot == 3 && weapon == null)
            weapon = Player.Inventory.Weapons.GetWeapon(Player, slot, 1);

        if (slot < 0 || slot >= WeaponImages.Length || slot >= HasWeaponImages.Length)
            return;

        string image = weapon != null ? HasWeaponImages[slot] : WeaponImages[slot];
        if (string.IsNullOrEmpty(image))
            return;

        hud.Image(image, slot switch
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
        bool backpack = Player.Inventory.HasItemOfClass(Inventory.BackPackBaseClassName);
        DrawFullTotalAmmoText(hud, "Clip", backpack ? 400 : 200, 173);
        DrawFullTotalAmmoText(hud, "Shell", backpack ? 100 : 50, 179);
        DrawFullTotalAmmoText(hud, "RocketAmmo", backpack ? 100 : 50, 185);
        DrawFullTotalAmmoText(hud, "Cell", backpack ? 600 : 300, 191);
    }

    void DrawFullTotalAmmoText(IHudRenderContext hud, string ammoName, int maxAmmo, int y)
    {
        const int FontSize = 6;
        const string YellowFontName = "HudYellowNumbers";
        int ammo = Player.Inventory.Amount(ammoName);
        m_ammoString.Clear();
        m_maxAmmoString.Clear();
        m_ammoString.Append(ammo);
        m_maxAmmoString.Append(maxAmmo);
        hud.Text(m_ammoString.AsSpan(), YellowFontName, FontSize, (287, y), anchor: Align.TopRight);
        hud.Text(m_maxAmmoString.AsSpan(), YellowFontName, FontSize, (315, y), anchor: Align.TopRight);
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
