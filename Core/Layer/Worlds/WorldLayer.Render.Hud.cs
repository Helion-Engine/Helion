using System;
using System.Collections.Generic;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Graphics.Fonts;
using Helion.Render;
using Helion.Render.Common;
using Helion.Render.Common.Context;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Render.Common.Textures;
using Helion.Render.OpenGL.Renderers.Legacy.World.Shader;
using Helion.Render.OpenGL.Texture.Fonts;
using Helion.Render.OpenGL.Util;
using Helion.Resources;
using Helion.Resources.Definitions.Decorate.States;
using Helion.Resources.Definitions.MapInfo;
using Helion.Strings;
using Helion.Util;
using Helion.Util.Configs.Components;
using Helion.Util.Configs.Extensions;
using Helion.Util.Consoles;
using Helion.Util.Extensions;
using Helion.Util.Timing;
using Helion.World;
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
    private const double DoomVerticalScale = (320 / 200.0) / (640 / 480.0);
    private const int MapFontSize = 12;
    private const int DebugFontSize = 8;
    private const int LeftOffset = 1;
    private const int TopOffset = 1;
    private const int MessageSpacing = 1;
    private const int FpsMessageSpacing = 2;
    private const long MaxVisibleTimeNanos = 4 * 1000L * 1000L * 1000L;
    private const long MessageTransitionSpan = 350L * 1000L * 1000L;
    private const ResourceNamespace LookupNamespace = ResourceNamespace.Undefined;
    private static readonly Color PickupColor = (255, 255, 128);
    private static readonly Color DamageColor = (255, 0, 0);
    private const string SmallHudFont = Constants.Fonts.Small;
    private const string LargeHudFont = Constants.Fonts.LargeHud;
    private const string ConsoleFont = Constants.Fonts.Console;
    private int m_fontHeight = 16;
    private int m_padding = 4;
    private int m_hudPaddingX = 0;
    private float m_scale = 1.0f;
    private float m_hudAlpha = 0.5f;
    private int m_infoFontSize = DebugFontSize;
    private int m_mapHeaderFontSize = MapFontSize;
    private Dimension m_viewport;
    private readonly List<(string message, float alpha)> m_messages = new();

    private int m_healthWidth;
    private string m_weaponSprite = StringBuffer.GetStringExact(6);
    private string m_weaponFlashSprite = StringBuffer.GetStringExact(6);
    private string m_weaponNumberTexture = StringBuffer.GetString(16);
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

    private static readonly string[] StatLabels = ["Kills: ", "Items: ", "Secrets: "];
    private readonly SpanString[] StatValues;
    private readonly RenderableString[] RenderableStatLabels;
    private readonly RenderableString[] RenderableStatValues;
    private readonly Font m_largeHudFont;

    private readonly record struct HudDrawWeapon(IHudRenderContext Hud, FrameState FrameState, int yOffset, bool Flash);

    private void DrawHud(HudRenderContext hudContext, IHudRenderContext hud, bool automapVisible)
    {
        m_scale = (float)m_config.Hud.Scale.Value;
        m_hudAlpha = 1f - (float)m_config.Hud.Transparency.Value;
        m_infoFontSize = Math.Max((int)(m_scale * DebugFontSize), 12);
        m_mapHeaderFontSize = Math.Max((int)(m_scale * MapFontSize), 20);
        m_padding = (int)(4 * m_scale);
        m_fontHeight = (int)(16 * m_scale);
        m_viewport = hud.Dimension;

        if ((m_renderHudOptions & RenderHudOptions.Weapon) != 0)
            DrawWeapon(hud, hudContext);

        if ((m_renderHudOptions & RenderHudOptions.Crosshair) != 0 && m_config.Hud.Crosshair)
            DrawCrosshair(hud);

        if ((m_renderHudOptions & RenderHudOptions.Hud) != 0)
        {
            SetHudPadding(hud);

            int topRightY = m_padding / 2;
            DrawFPS(hud, ref topRightY);
            DrawPosition(hud, ref topRightY);
            DrawStatInfo(hud, automapVisible, (0, topRightY), ref topRightY);
            DrawBottomHud(hud, topRightY, hudContext);
            DrawHudEffects(hud);
            hud.DrawPalette(false);
            DrawRecentConsoleMessages(hud);
            DrawPause(hud);

            if (automapVisible && m_config.Hud.AutoMap.MapTitle)
                DrawMapHeader(hud);

            hud.DrawPalette(true);
        }

        if ((m_renderHudOptions & RenderHudOptions.BackDrop) != 0)
        {
            var color = new Color(m_config.Hud.AutoMap.OverlayBackdropColor.Value);
            var alpha = 1 - (float)m_config.Hud.AutoMap.OverlayBackdropTransparency;
            hud.FillBox((0, 0, hud.Width, hud.Height), color, alpha: alpha);
        }
    }

    private void SetHudPadding(IHudRenderContext hud)
    {
        m_hudPaddingX = (int)(hud.Dimension.Width * m_config.Hud.HorizontalMargin);
        if (!m_config.Window.Virtual.Enable || m_config.Window.Virtual.Stretch)
            return;

        var virtualDim = m_config.Window.Virtual.Dimension.Value;
        m_hudPaddingX = (int)(virtualDim.Width * m_config.Hud.HorizontalMargin);
        m_hudPaddingX += Math.Max(0, (int)(hud.Dimension.Height * (hud.Dimension.AspectRatio - virtualDim.AspectRatio)) / 2);
    }

    private void DrawMapHeader(IHudRenderContext hud)
    {
        const int StatusBarSize = 32;
        const int FaceSize = 24;
        float doomScale = hud.Dimension.Height / 200f;
        int padding = (int)(2 * m_scale);
        Vec2I pos = new(padding + m_hudPaddingX, -padding);
        var offsetY = m_statusBarSizeType switch
        {
            StatusBarSizeType.Hidden => 0,
            StatusBarSizeType.Minimal => (int)(FaceSize * doomScale) + (int)(8 * doomScale),
            _ => (int)(StatusBarSize * doomScale),
        };
        pos.Y -= offsetY;

        string text = World.MapInfo.GetDisplayNameWithPrefix(World.ArchiveCollection);
        hud.Text(text, SmallHudFont, m_mapHeaderFontSize, pos, both: Align.BottomLeft);
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

        start.X = -m_padding - m_hudPaddingX;
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
                RenderableStatLabels[i] = SetRenderableString(StatLabels[i], RenderableStatLabels[i], ConsoleFont, m_infoFontSize, 
                    useDoomScale: false);

            RenderableStatValues[0] = SetRenderableString(m_killString.AsSpan(), m_renderKillString, ConsoleFont, m_infoFontSize,
                GetStatColor(World.LevelStats.KillCount, World.LevelStats.TotalMonsters), useDoomScale: false);
            RenderableStatValues[1] = SetRenderableString(m_itemString.AsSpan(), m_renderItemString, ConsoleFont, m_infoFontSize,
                GetStatColor(World.LevelStats.ItemCount, World.LevelStats.TotalItems), useDoomScale: false);
            RenderableStatValues[2] = SetRenderableString(m_secretString.AsSpan(), m_renderSecretString, ConsoleFont, m_infoFontSize,
                GetStatColor(World.LevelStats.SecretCount, World.LevelStats.TotalSecrets), useDoomScale: false);
        }

        for (int i = 0; i < RenderableStatValues.Length; i++)
        {
            maxLabelWidth = Math.Max(RenderableStatLabels[i].DrawArea.Width, maxLabelWidth);
            maxValueWidth = Math.Max(RenderableStatValues[i].DrawArea.Width, maxValueWidth);
        }

        labelPos.X = -(maxValueWidth + m_padding + m_hudPaddingX);

        for (int i = 0; i < RenderableStatLabels.Length; i++)
        {
            var str = RenderableStatLabels[i];
            hud.Text(RenderableStatLabels[i], labelPos, both: align, alpha: m_hudAlpha);
            labelPos.Y += str.DrawArea.Height;
        }

        labelPos = start;
        for (int i = 0; i < RenderableStatValues.Length; i++)
        {
            var str = RenderableStatValues[i];
            hud.Text(RenderableStatValues[i], labelPos, both: align, alpha: m_hudAlpha);
            labelPos.Y += str.DrawArea.Height;
        }
        labelPos.Y += m_padding;

        if (HasTicks)
        {
            TimeSpan ts = TimeSpan.FromSeconds(World.LevelTime / 35);
            m_timeString.Clear();
            m_timeString.Append(ts.Hours, 2);
            m_timeString.Append(':');
            m_timeString.Append(ts.Minutes, 2);
            m_timeString.Append(':');
            m_timeString.Append(ts.Seconds, 2);

            SetRenderableString(m_timeString.AsSpan(), m_renderTimeString, ConsoleFont, m_infoFontSize, useDoomScale: false);
        }

        hud.Text(m_renderTimeString, labelPos, both: align, alpha: m_hudAlpha);
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
        if (!WorldStatic.World.DrawHud || ShaderVars.PaletteColorMode)
            return;

        IPowerup? powerup = Player.Inventory.PowerupEffectColor;
        if (powerup?.DrawColor != null && powerup.DrawPowerupEffect)
            hud.Clear(powerup.DrawColor.Value, powerup.DrawAlpha);

        if (Player.BonusCount > 0)
            hud.Clear(PickupColor, 0.2f);

        if (Player.DamageCount > 0)
            hud.Clear(DamageColor, Player.DamageCount * 0.01f * (float)m_config.Game.PainIntensity);
    }

    private void DrawFPS(IHudRenderContext hud, ref int topRightY)
    {
        if (!m_config.Hud.ShowFPS && !m_config.Hud.ShowMinMaxFPS)   
            return;

        if (m_config.Hud.ShowFPS)
            DrawFpsValue(hud, "", m_fpsTracker.AverageFramesPerSecond, ref topRightY, m_fpsString, m_renderFpsString);

        if (m_config.Hud.ShowMinMaxFPS)
        {
            DrawFpsValue(hud, "Max ", m_fpsTracker.MaxFramesPerSecond, ref topRightY, m_fpsMaxString, m_renderFpsMaxString);
            DrawFpsValue(hud, "Min ", m_fpsTracker.MinFramesPerSecond, ref topRightY, m_fpsMinString, m_renderFpsMinString);
        }

        topRightY += m_padding;
    }

    void DrawFpsValue(IHudRenderContext hud, string prefix, double fps, ref int y, SpanString str, RenderableString renderableString)
    {
        str.Clear();
        str.Append(prefix);
        str.Append("FPS: ");
        str.Append((int)Math.Round(fps));

        SetRenderableString(str.AsSpan(), renderableString, ConsoleFont, m_infoFontSize, useDoomScale: false);
        hud.Text(renderableString, (-m_padding - m_hudPaddingX, y), both: Align.TopRight, alpha: m_hudAlpha);

        y += renderableString.DrawArea.Height + FpsMessageSpacing;
    }

    private void DrawPosition(IHudRenderContext hud, ref int topRightY)
    {
        if (!Player.Cheats.IsCheatActive(Helion.World.Cheats.CheatType.ShowPosition))
            return;

        DrawCoordinate(hud, 'X', Player.Position.X, ref topRightY);
        DrawCoordinate(hud, 'Y', Player.Position.Y, ref topRightY);
        DrawCoordinate(hud, 'Z', Player.Position.Z, ref topRightY);
        DrawCoordinate(hud, 'A', Player.AngleRadians % Math.PI * 180 / MathHelper.Pi, ref topRightY);
        DrawCoordinate(hud, 'P', Player.PitchRadians % Math.PI * 180 / MathHelper.Pi, ref topRightY);
        topRightY += m_padding;
    }

    void DrawCoordinate(IHudRenderContext hud, char axis, double position, ref int y)
    {
        hud.Text($"{axis}: {Math.Floor(position * 10000)/10000}", ConsoleFont, m_infoFontSize,
            (-m_padding - m_hudPaddingX, y), out Dimension area, TextAlign.Right, both: Align.TopRight,
            color: Color.White, alpha: m_hudAlpha);
        y += area.Height + FpsMessageSpacing;
    }

    private void DrawBottomHud(IHudRenderContext hud, int topRightY, HudRenderContext hudContext)
    {
        if (!WorldStatic.World.DrawHud)
            return;

        m_statusBarSizeType = m_config.Hud.StatusBarSize.Value;
        switch (m_statusBarSizeType)
        {
            case StatusBarSizeType.Minimal:
                DrawMinimalStatusBar(hud, topRightY);
                break;
            case StatusBarSizeType.Hidden:
                break;
            default:
                DrawFullStatusBar(hud);
                break;
        }
    }

    private void DrawWeapon(IHudRenderContext hud, HudRenderContext hudContext)
    {
        if (!WorldStatic.World.DrawHud)
            return;

        if (Player.AnimationWeapon != null)
        {
            // When using palette mode disable boom colormaps for weapons
            if (ShaderVars.PaletteColorMode)
                hudContext.DrawColorMap = true;
            else
                hudContext.DrawColorMap = Player.DrawInvulnerableColorMap();
            IPowerup? powerup = Player.Inventory.GetPowerup(PowerupType.Invisibility);
            if (powerup != null && powerup.DrawPowerupEffect)
                hudContext.DrawFuzz = true;

            // Doom pushes the gun sprite up when the status bar is showing
            int yOffset = m_config.Hud.StatusBarSize == StatusBarSizeType.Full ? HudView.FullSizeHudOffsetY : 0;
            DrawHudWeapon(hud, Player.AnimationWeapon.FrameState, yOffset, flash: false);
            if (Player.AnimationWeapon.FlashState.Frame.BranchType != ActorStateBranch.Stop)
                DrawHudWeapon(hud, Player.AnimationWeapon.FlashState, yOffset, flash: true);

            hudContext.DrawColorMap = false;
            hudContext.DrawFuzz = false;
        }
    }

    private static short GetLightLevel(Player player)
    {
        // TODO this should probably use RenderInfo
        Sector sector = player.Sector.GetRenderSector(player.Sector, player.Position.Z + player.ViewHeight);
        return (short)((sector.TransferFloorLightSector.LightLevel + sector.TransferCeilingLightSector.LightLevel) / 2);
    }

    private void DrawHudWeapon(IHudRenderContext hud, FrameState frameState, int yOffset, bool flash)
    {
        int lightLevel;
        int colorMapIndex;
        if (frameState.Frame.Properties.Bright || Player.DrawFullBright())
        {
            lightLevel = 255;
            colorMapIndex = 0;
        }
        else
        {
            int extraLight = Player.GetExtraLightRender();
            lightLevel = GetLightLevel(Player);
            colorMapIndex = GLHelper.ColorMapIndex(lightLevel, extraLight);
            lightLevel = GLHelper.DoomLightLevelToColor(lightLevel, extraLight);
        }

        var camera = World.GetCameraPlayer().GetCamera(m_lastTickInfo.Fraction);
        var colorMixUniforms = Renderer.GetColorMix(Player, camera);
        var colorMix = colorMixUniforms.Global;

        if (colorMixUniforms.Sector != Vec3F.One)
            colorMix = colorMixUniforms.Sector;

        Color lightLevelColor = ShaderVars.PaletteColorMode ? Color.White :            
            ((byte)Math.Min(lightLevel * colorMix.X, 255), 
            (byte)Math.Min(lightLevel * colorMix.Y, 255), 
            (byte)Math.Min(lightLevel * colorMix.Z, 255));

        string sprite = GetHudWeaponSpriteString(frameState, flash);

        if (!hud.Textures.TryGet(sprite, out var handle, LookupNamespace))
            return;

        var offset = handle.Offset;
        offset.Y += yOffset;
        offset = TranslateDoomOffset(offset);
        var hudBox = GetInterpolatePlayerWeaponBox(hud, handle, offset);

        hud.Image(sprite, hudBox, color: lightLevelColor, colorMapIndex: colorMapIndex);
    }

    private HudBox GetInterpolatePlayerWeaponBox(IHudRenderContext hud, IRenderableTextureHandle handle, Vec2I offset)
    {
        Vec2D scale = GetDoomScale(hud, out int centeredOffsetX);
        var prevWeaponOffset = Player.PrevWeaponOffset + Player.PrevWeaponBobOffset;
        var weaponOffset = Player.WeaponOffset + Player.WeaponBobOffset;

        var prevBox = TranslateDoomImageDimensions(offset.X + prevWeaponOffset.X,
            offset.Y + prevWeaponOffset.Y,
            handle.Dimension.Width,
            handle.Dimension.Height,
            scale);

        var box = TranslateDoomImageDimensions(offset.X + weaponOffset.X,
            offset.Y + weaponOffset.Y,
            handle.Dimension.Width,
            handle.Dimension.Height,
            scale);

        var prevBoxMin = prevBox.Min.Double;
        var prevBoxMax = prevBox.Max.Double;
        var boxMin = box.Min.Double;
        var boxMax = box.Max.Double;
        var interpolatedMin = prevBoxMin.Interpolate(boxMin, m_lastTickInfo.Fraction).Int;
        var interpolatedMax = prevBoxMax.Interpolate(boxMax, m_lastTickInfo.Fraction).Int;

        var centeredOffset = new Vec2I(centeredOffsetX, 0);
        return new HudBox(interpolatedMin + centeredOffset, interpolatedMax + centeredOffset);
    }

    private HudBox TranslateDoomImageDimensions(double x, double y, int width, int height, Vec2D scale)
    {
        var start = (new Vec2D(x, y) * scale).Int;
        var end = (new Vec2D(x + width, y + height) * scale).Int;
        return new HudBox((start.X, start.Y), (end.X, end.Y));
    }

    private Vec2D GetDoomScale(IHudRenderContext ctx, out int centeredOffsetX)
    {
        var dimension = ctx.Dimension;
        var virtualDimensions = new Dimension(320, 200);
        if (dimension == virtualDimensions)
        {
            centeredOffsetX = 0;
            return new Vec2D(1, 1);
        }

        double viewWidth = ctx.Dimension.Height * Constants.DoomVirtualAspectRatio;
        double scaleWidth = viewWidth / virtualDimensions.Width;
        double scaleHeight = ctx.Dimension.Height / (double)virtualDimensions.Height;
        var scale = new Vec2D(scaleWidth, scaleHeight);
        centeredOffsetX = (ctx.Dimension.Width - (int)(virtualDimensions.Width * scale.X)) / 2;
        return scale;
    }

    private string GetHudWeaponSpriteString(FrameState frameState, bool flash)
    {
        string sprite = flash ? m_weaponFlashSprite : m_weaponSprite;
        SpanString spriteSpan = flash ? m_weaponFlashSpriteSpan : m_weaponSpriteSpan;
        int oldLength = spriteSpan.Length;
        spriteSpan.Clear();
        spriteSpan.Append(frameState.Frame.Sprite);
        spriteSpan.Append((char)(frameState.Frame.Frame + 'A'));
        spriteSpan.Append('0');

        // This buffer string needs to have the exact length of the sprite. All these lookups are dependent on GetHashCode which changes with string.Length...
        int newLength = spriteSpan.Length;
        if (newLength != oldLength)
        {
            string exactSpriteString = StringBuffer.ToStringExact(sprite);
            if (!ReferenceEquals(exactSpriteString, sprite))
                StringBuffer.FreeString(sprite);
            if (flash)
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
        int Length = (int)(5 * m_scale * m_config.Hud.CrosshairScale.Value);

        Color color = ToColor(m_config.Hud.CrosshairColor);
        bool target = Player.CrosshairTarget.Entity != null;
        int crosshairLength = target ? (int)(Length * 0.8f) : Length;

        if (m_config.Hud.CrosshairHealthIndicator.Value)
        {
            // If the crosshair color and crosshair target color are not the same, then the player clearly wants it to change color
            // if we've detected a target.  Else, render a health indicator using hue angle (240 is blue, 120 green, 0 red).
            color = target && m_config.Hud.CrosshairColor != m_config.Hud.CrosshairTargetColor
                ? ToColor(m_config.Hud.CrosshairTargetColor.Value)
                : Color.FromHSV((int)(Math.Clamp(Player.Health, 0, 200) * 1.2f), 100, 100);
        }
        else
        {
            color = target ? ToColor(m_config.Hud.CrosshairTargetColor.Value) : ToColor(m_config.Hud.CrosshairColor);
        }

        int totalCrosshairLength = crosshairLength * 2;
        if (Width == 1)
            totalCrosshairLength += 1;

        Vec2I center = m_viewport.Vector / 2;
        center -= HudView.GetViewPortOffset(m_config.Hud.StatusBarSize, m_viewport);

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

        float alpha = 1f - (float)m_config.Hud.CrosshairTransparency;
        var lengthFraction = totalCrosshairLength;
        switch (m_config.Hud.CrosshairType.Value)
        {
            case CrosshairStyle.Cross1:
                lengthFraction /= 3;
                hud.FillBox((horizontal.X, horizontal.Y, horizontal.X + lengthFraction, horizontal.Y + HalfWidth), color, alpha: alpha);
                hud.FillBox((horizontal.X + totalCrosshairLength - lengthFraction, horizontal.Y, horizontal.X + totalCrosshairLength, horizontal.Y + HalfWidth), color, alpha: alpha);
                hud.FillBox((vertical.X, vertical.Y, vertical.X + HalfWidth, vertical.Y + lengthFraction), color, alpha: alpha);
                hud.FillBox((vertical.X, vertical.Y + totalCrosshairLength - lengthFraction, vertical.X + HalfWidth, vertical.Y + totalCrosshairLength), color, alpha: alpha);
                break;
            case CrosshairStyle.Cross2:
                lengthFraction = lengthFraction / 2 - 2;
                hud.FillBox((horizontal.X, horizontal.Y, horizontal.X + lengthFraction, horizontal.Y + HalfWidth), color, alpha: alpha);
                hud.FillBox((horizontal.X + totalCrosshairLength - lengthFraction, horizontal.Y, horizontal.X + totalCrosshairLength, horizontal.Y + HalfWidth), color, alpha: alpha);
                hud.FillBox((vertical.X, vertical.Y, vertical.X + HalfWidth, vertical.Y + lengthFraction), color, alpha: alpha);
                hud.FillBox((vertical.X, vertical.Y + totalCrosshairLength - lengthFraction, vertical.X + HalfWidth, vertical.Y + totalCrosshairLength), color, alpha: alpha);
                break;
            case CrosshairStyle.Cross3:
                hud.FillBox((horizontal.X, horizontal.Y, horizontal.X + totalCrosshairLength, horizontal.Y + HalfWidth), color, alpha: alpha);
                hud.FillBox((vertical.X, vertical.Y, vertical.X + HalfWidth, vertical.Y + totalCrosshairLength), color, alpha: alpha);
                break;
            case CrosshairStyle.Dot:
                var size = target ? 1 : 1.5;
                totalCrosshairLength = Math.Max((int)(size * m_scale * m_config.Hud.CrosshairScale.Value), 1);
                hud.FillBox((center.X, center.Y, center.X + totalCrosshairLength, center.Y + totalCrosshairLength), color, alpha: alpha);
                break;
        }
    }

    private static Color ToColor(CrossColor c) => c switch
    {
        CrossColor.Green => Color.LawnGreen,
        CrossColor.Blue => Color.Blue,
        CrossColor.Red => Color.Red,
        CrossColor.Yellow => Color.Yellow,
        _ => Color.White
    };

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

    private RenderableString SetRenderableString(ReadOnlySpan<char> charSpan, RenderableString renderableString, string font, int fontSize, Color? drawColor = null,
        bool useDoomScale = true)
    {
        if (!HasTicks)
            return renderableString;

        renderableString.Set(World.ArchiveCollection.DataCache, charSpan, GetFontOrDefault(font),
            fontSize, TextAlign.Left, drawColor: drawColor);
        if (useDoomScale)
            renderableString.DrawArea = new(renderableString.DrawArea.Width, (int)(renderableString.DrawArea.Height * DoomVerticalScale));
        return renderableString;
    }

    private bool HasTicks => m_lastTickInfo.Ticks > 0;

    private void DrawMinimalHudHealthAndArmor(IHudRenderContext hud)
    {
        const string Medkit = "MEDIA0";

        // We will draw the medkit slightly higher so it looks like it
        // aligns with the font.
        int x = m_padding + m_hudPaddingX;
        int y = -m_padding;

        bool hasArmorImage = false;
        var armorProp = Player.ArmorProperties;
        var armorDimension = new Dimension(0, 0);
        if (armorProp != null && hud.Textures.HasImage(armorProp.Inventory.Icon))
        {
            armorDimension = GetDoomScaledImageArea(hud, armorProp.Inventory.Icon);
            hasArmorImage = true;
        }

        // Force the column width to the maximum of armor / medkit images
        // Custom images can change the dimensions and cause it to bump whem armor is picked up. Assume a 32 width for armor by default.
        var medkitDimension = GetDoomScaledImageArea(hud, Medkit);
        int setWidth = Math.Max(armorDimension.Width, medkitDimension.Width);
        setWidth = Math.Max(setWidth, (int)(32 * m_scale));
        int textStartX = m_hudPaddingX + setWidth + m_padding * 2;

        x += (setWidth - medkitDimension.Width) / 2;
        DrawDoomScaledImage(hud, Medkit, (x, y), out var medkitArea, both: Align.BottomLeft, alpha: m_hudAlpha);
        x = textStartX;

        m_healthString.Clear();
        m_healthString.Append(Math.Max(0, Player.Health));

        SetRenderableString(m_healthString.AsSpan(), m_renderHealthString, LargeHudFont, m_fontHeight);
        hud.Text(m_renderHealthString, (x, y), both: Align.BottomLeft, alpha: m_hudAlpha);

        // This is to make sure the face never moves (even if the health changes).
        if (HasTicks)
            m_healthWidth = hud.MeasureText("9999", LargeHudFont, m_fontHeight).Width;
        x += m_healthWidth;
        int highestX = x;

        DrawDoomScaledImage(hud, Player.StatusBar.GetFacePatch(), (x, y), out var faceArea, both: Align.BottomLeft, alpha: m_hudAlpha);

        if (Player.Armor > 0)
        {
            x = m_hudPaddingX + m_padding + (setWidth - armorDimension.Width) / 2;
            y -= medkitArea.Height + m_padding;

            if (armorProp != null && hasArmorImage)
                DrawDoomScaledImage(hud, armorProp.Inventory.Icon, (x, y), out var armorArea, both: Align.BottomLeft, alpha: m_hudAlpha);

            x = textStartX;

            m_armorString.Clear();
            m_armorString.Append(Player.Armor);
            SetRenderableString(m_armorString.AsSpan(), m_renderArmorString, LargeHudFont, m_fontHeight);

            hud.Text(m_renderArmorString, (x, y), both: Align.BottomLeft, alpha: m_hudAlpha);
        }
    }

    private void DrawDoomScaledImage(IHudRenderContext hud, string image, Vec2I origin, out HudBox area, Align? both = null, float alpha = 1)
    {
        if (!hud.Textures.TryGet(image, out var handle, LookupNamespace))
        {
            area = default;
            return;
        }

        var verticalScale = hud.Dimension.Width == 320 && hud.Dimension.Height == 200 ? 1 : DoomVerticalScale;
        var scale = new Vec2D(1 * m_scale, verticalScale * m_scale);
        var imageArea = new Box2D(handle.Area.Min.Double * scale, handle.Area.Max.Double * scale).Int;
        area = new HudBox(origin + imageArea.Min, origin + imageArea.Max);
        hud.Image(image, area, both: both, alpha: alpha);
    }

    private Dimension GetDoomScaledImageArea(IHudRenderContext hud, string image)
    {
        if (!hud.Textures.TryGet(image, out var handle, LookupNamespace))
            return default;

        var scale = new Vec2D(1 * m_scale, DoomVerticalScale * m_scale);
        return new Dimension((int)(handle.Area.Width * scale.X), (int)(handle.Area.Height * scale.Y));
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
            DrawDoomScaledImage(hud, icon, (-m_padding - m_hudPaddingX, y), out var drawArea, both: Align.TopRight, alpha: m_hudAlpha);
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

        int x = -m_padding - m_hudPaddingX;
        int y = -m_padding;

        if (HasTicks)
        {
            int ammo = Player.Inventory.Amount(ammoType);
            m_ammoString.Clear();
            m_ammoString.Append(ammo);

            SetRenderableString(m_ammoString.AsSpan(), m_renderAmmoString, LargeHudFont, m_fontHeight);
        }

        hud.Text(m_renderAmmoString, (x, y), both: Align.BottomRight, alpha: m_hudAlpha);

        x -= m_renderAmmoString.DrawArea.Width + m_padding;
        if (weapon.AmmoSprite.Length <= 0 || !hud.Textures.TryGet(weapon.AmmoSprite, out var handle))
            return;

        x -= (int)(handle.Dimension.Width * m_scale);
        DrawDoomScaledImage(hud, weapon.AmmoSprite, (x, y), out _, both: Align.BottomRight, alpha: m_hudAlpha);
    }

    private void DrawFullStatusBar(IHudRenderContext hud)
    {
        const string StatusBar = "STBAR";
        if (m_statusBarSizeType == StatusBarSizeType.Full)
        {
            if (hud.Textures.TryGet(StatusBar, out var statusBarHandle))
                DrawStatusBarBackground(hud, statusBarHandle);
            hud.RenderStatusBar(StatusBar);
        }

        hud.DoomVirtualResolution(m_virtualDrawFullStatusBarAction, hud);
    }

    private void VirtualDrawFullStatusBar(IHudRenderContext hud)
    {
        var hudAlpha = m_hudAlpha;
        if (m_statusBarSizeType == StatusBarSizeType.Full)
            m_hudAlpha = 1;

        DrawFullHudHealthArmorAmmo(hud);
        DrawFullHudWeaponSlots(hud);
        if (m_statusBarSizeType == StatusBarSizeType.Full)
            HudImageWithOffset(hud, Player.StatusBar.GetFacePatch(), (143, 168));
        DrawFullHudKeys(hud);
        DrawFullTotalAmmo(hud);

        m_hudAlpha = hudAlpha;
    }

    private void HudImageWithOffset(IHudRenderContext hud, string image, Vec2I pos, Align? both = null)
    {
        if (!hud.Textures.TryGet(image, out var faceHandle))
            return;
        pos += TranslateDoomOffset(faceHandle.Offset);
        hud.Image(image, pos, both: both, alpha: m_hudAlpha);
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
        var aspectRatio = m_viewport.AspectRatio;
        int yOffset = -hud.BarHandle.Dimension.Height + 1;
        int backgroundHandleWidth = hud.BackgroundHandle.Dimension.Width;
        int calcWidth = (int)((aspectRatio) / 1.6f * 320) + hud.BarHandle.Dimension.Width;
        int iterations = (calcWidth / hud.BackgroundHandle.Dimension.Width) + 1;

        int xOffset = 0;
        for (int i = 0; i < iterations; i++)
        {
            hud.Hud.Image(m_config.Hud.BackgroundTexture, (xOffset, yOffset), Align.BottomLeft);
            xOffset += backgroundHandleWidth;
        }
    }

    private void DrawFullHudHealthArmorAmmo(IHudRenderContext hud)
    {
        // Note: This area is already drawn using Doom's stretched scale so useDoomScale needs to be false.
        const int OffsetY = 171;
        int fontSize = hud.GetFontMaxHeight(LargeHudFont);

        const int HealthX = 90;
        const int ArmorX = 221;
        const int AmmoX = 43;

        var weapon = Player.AnimationWeapon;
        if (weapon != null && weapon.Definition.Properties.Weapons.AmmoType.Length > 0)
        {
            int ammoAmount = Player.Inventory.Amount(weapon.Definition.Properties.Weapons.AmmoType);
            m_ammoString.Clear();
            m_ammoString.Append(Math.Clamp(ammoAmount, 0, 999));
            SetRenderableString(m_ammoString.AsSpan(), m_renderAmmoString, LargeHudFont, fontSize, useDoomScale: false);
            hud.Text(m_renderAmmoString, (AmmoX, OffsetY), anchor: Align.TopRight, alpha: m_hudAlpha);
        }

        m_healthString.Clear();
        m_healthString.Append(Math.Clamp(Player.Health, 0, 999));
        m_healthString.Append('%');

        SetRenderableString(m_healthString.AsSpan(), m_renderHealthString, LargeHudFont, fontSize, useDoomScale: false);
        hud.Text(m_renderHealthString, (CalcPercentStartOffsetX(HealthX, m_largeHudFont), OffsetY), anchor: Align.TopRight, alpha: m_hudAlpha);

        m_armorString.Clear();
        m_armorString.Append(Math.Clamp(Player.Armor, 0, 999));
        m_armorString.Append('%');

        SetRenderableString(m_armorString.AsSpan(), m_renderArmorString, LargeHudFont, fontSize, useDoomScale: false);
        hud.Text(m_renderArmorString, (CalcPercentStartOffsetX(ArmorX, m_largeHudFont), OffsetY), anchor: Align.TopRight, alpha: m_hudAlpha);
    }

    private static int CalcPercentStartOffsetX(int offsetX, Font font)
    {
        if (!font.FixedWidthChar.HasValue)
            return offsetX - 1;
        return offsetX + font.FixedWidthChar.Value.Area.Width - 1;
    }

    private void DrawFullHudWeaponSlots(IHudRenderContext hud)
    {
        if (m_statusBarSizeType ==  StatusBarSizeType.Full)
            HudImageWithOffset(hud, "STARMS", (104, 168), both: Align.TopLeft);

        for (int slot = 2; slot <= 7; slot++)
            DrawWeaponNumber(hud, slot);
    }

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

        HudImageWithOffset(hud, image, slot switch
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

    private void DrawKeyIfOwned(IHudRenderContext hud, InventoryItem key, string skullKeyName,
        string keyName, int x, int y)
    {
        string imageName = key.Definition.Properties.Inventory.Icon;

        if (key.Definition.Name.EqualsIgnoreCase(skullKeyName) && hud.Textures.HasImage(imageName))
        {
            HudImageWithOffset(hud, imageName, (x, y));
            return;
        }

        if (key.Definition.Name.EqualsIgnoreCase(keyName) && hud.Textures.HasImage(imageName))
            HudImageWithOffset(hud, imageName, (x, y));
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
        hud.Text(m_ammoString.AsSpan(), YellowFontName, FontSize, (287, y), anchor: Align.TopRight, alpha: m_hudAlpha);
        hud.Text(m_maxAmmoString.AsSpan(), YellowFontName, FontSize, (313, y), anchor: Align.TopRight, alpha: m_hudAlpha);
    }

    private int m_lastMessageCount = 0;

    private void DrawRecentConsoleMessages(IHudRenderContext hud)
    {
        int maxHudMessages = m_config.Hud.MaxMessages.Value;

        long currentNanos = Ticker.NanoTime();
        int messagesDrawn = 0;
        int offsetY = TopOffset;

        // We want to draw the ones that are less recent at the top first,
        // so when we iterate and see most recent to least recent, pushing
        // most recent onto the stack means when we iterate over this we
        // will draw the later ones at the top. Otherwise if we were to do
        // forward iteration without the stack, then they get drawn in the
        // reverse order and fading begins at the wrong end.

        long lastMessageTime = 0;
        lock (m_console.Messages)
        {
            LinkedListNode<ConsoleMessage>? node = m_console.Messages.First;
            while (node != null)
            {
                ConsoleMessage msg = node.Value;
                node = node.Next;

                if (messagesDrawn >= maxHudMessages || MessageTooOldToDraw(msg, World, m_console, m_parent.OptionsLastClosedNanos))
                    break;

                long timeSinceMessage = currentNanos - msg.TimeNanos;
                if (timeSinceMessage > MaxVisibleTimeNanos || m_parent.ConsoleLayer != null)
                    break;

                m_messages.Add((msg.Message, CalculateFade(timeSinceMessage)));
                messagesDrawn++;
                lastMessageTime = timeSinceMessage;
            }

            int fontSize = hud.GetFontMaxHeight(SmallHudFont);
            int slideOffsetY = m_messages.Count <= 1 ? 0 : CalculateSlide(hud, lastMessageTime);
            for (int i = m_messages.Count - 1; i >= 0; i--)
            {
                hud.Text(m_messages[i].message, SmallHudFont, fontSize, (LeftOffset + m_hudPaddingX, offsetY + slideOffsetY),
                    out Dimension drawArea, window: Align.TopLeft, scale: m_scale, alpha: m_messages[i].alpha * m_hudAlpha);
                offsetY += drawArea.Height + MessageSpacing;
            }

            m_lastMessageCount = m_messages.Count;

            m_messages.Clear();
        }
    }

    private static bool MessageTooOldToDraw(ConsoleMessage msg, WorldBase world, HelionConsole console, long optionsLastClosedNanos)
    {
        if (optionsLastClosedNanos > msg.TimeNanos)
            return true;

        return msg.TimeNanos < world.CreationTimeNanos || msg.TimeNanos < console.LastClosedNanos;
    }

    private int CalculateSlide(IHudRenderContext hud, long timeSinceMessage)
    {
        const long SlideNanoRange = MaxVisibleTimeNanos - MessageTransitionSpan;        
        if (timeSinceMessage < SlideNanoRange)
            return 0;

        var dim = hud.MeasureText("I", SmallHudFont, 8);
        double frac = 1.0 - (double)(MaxVisibleTimeNanos - timeSinceMessage) / MessageTransitionSpan;
        return (int)(-(dim.Height + MessageSpacing) * frac * m_config.Hud.GetScaled(1));
    }

    private static float CalculateFade(long timeSinceMessage)
    {
        const long OpaqueNanoRange = MaxVisibleTimeNanos - MessageTransitionSpan;
        if (timeSinceMessage < OpaqueNanoRange)
            return 1.0f;

        double fractionIntoFadeRange = (double)(timeSinceMessage - OpaqueNanoRange) / MessageTransitionSpan;
        return 1.0f - (float)fractionIntoFadeRange;
    }
}
