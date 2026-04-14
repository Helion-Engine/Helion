using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Layer.Worlds.StatusBar;
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
using Helion.Resources.Definitions.StatusBar;
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
using System;
using System.Collections.Generic;
using static Helion.Render.Common.RenderDimensions;

namespace Helion.Layer.Worlds;

public partial class WorldLayer
{
    readonly record struct WorldMessage(string Message, float Alpha, int MessageCount);

    private const double DoomVerticalScale = (320 / 200.0) / (640 / 480.0);
    private const int MapFontSize = 12;
    private const int DebugFontSize = 8;
    private const int LeftOffset = 1;
    private const int TopOffset = 1;
    private const int MessageSpacing = 1;
    private const int FpsMessageSpacing = 2;
    private const long MessageTransitionSpan = 350L * 1000L * 1000L;
    private const ResourceNamespace SpriteLookupNamespace = ResourceNamespace.Sprites;
    private static readonly Color PickupColor = (255, 255, 128);
    private static readonly Color DamageColor = (255, 0, 0);
    private const string SmallHudFont = Constants.Fonts.Small;
    private const string LargeHudFont = Constants.Fonts.LargeHud;
    private const string FixedNumberFont = Constants.Fonts.SmallGrayFixedWidthNumbers;
    private int m_padding = 4;
    private int m_hudPaddingX;
    private float m_scale = 1.0f;
    private float m_hudAlpha = 0.5f;
    private int m_infoFontSize = DebugFontSize;
    private int m_mapHeaderFontSize = MapFontSize;
    private Dimension m_viewport;
    private readonly List<WorldMessage> m_messages = [];

    private string m_weaponSprite = StringBuffer.GetStringExact(6);
    private string m_weaponFlashSprite = StringBuffer.GetStringExact(6);
    private string m_renderMessageBufferString = StringBuffer.GetString();
    private readonly SpanString m_weaponSpriteSpan = new("123456");
    private readonly SpanString m_weaponFlashSpriteSpan = new("123456");
    
    private readonly StatusBarRenderer m_statusBarRenderer; 
    
    private readonly SpanString m_fpsString = new();
    private readonly SpanString m_fpsMinString = new();
    private readonly SpanString m_fpsMaxString = new();
    private readonly SpanString m_timeString = new();
    private readonly SpanString m_renderMessageSpan = new(128);

    private readonly RenderableString m_renderFpsString;
    private readonly RenderableString m_renderFpsMinString;
    private readonly RenderableString m_renderFpsMaxString;
    private readonly RenderableString m_renderTimeString;

    private readonly RenderStat[] m_renderStats;

    private readonly List<StringSlice> m_lineWrapStrings = [];

    private void DrawHud(HudRenderContext hudContext, IHudRenderContext hud, bool automapVisible)
    {
        m_scale = (float)m_config.Hud.Scale.Value;
        m_hudAlpha = 1f - (float)m_config.Hud.Transparency.Value;
        m_infoFontSize = Math.Max((int)(m_scale * DebugFontSize), 16);
        m_mapHeaderFontSize = Math.Max((int)(m_scale * MapFontSize), 20);
        m_padding = (int)(4 * m_scale);
        m_viewport = hud.Dimension;

        StatusBarLayoutDef? activeSbarLayout = GetActiveStatusBarLayout();

        StatusBarCoverage sbarCoverage = StatusBarCoverage.None;
        if (activeSbarLayout != null)
        {
            sbarCoverage = StatusBarRenderer.GetCoverage(activeSbarLayout);
        }

        if ((m_renderHudOptions & RenderHudOptions.Weapon) != 0)
            DrawWeapon(hud, hudContext);

        if ((m_renderHudOptions & RenderHudOptions.Crosshair) != 0 && m_config.Hud.Crosshair)
            DrawCrosshair(hud);

        if ((m_renderHudOptions & RenderHudOptions.Hud) != 0)
        {
            SetHudPadding(hud);

            int topRightY = m_padding / 2;
            
            if ((sbarCoverage & StatusBarCoverage.FPS) == 0)
                DrawFPS(hud, ref topRightY);
                
            DrawPosition(hud, ref topRightY);

            DrawStatInfo(hud, automapVisible, (0, topRightY), ref topRightY, 
                suppressStats: (sbarCoverage & StatusBarCoverage.Stats) != 0,
                suppressTime: (sbarCoverage & StatusBarCoverage.Time) != 0);

            DrawBottomHud(hud, automapVisible, activeSbarLayout);
            
            DrawHudEffects(hud);
            hud.DrawPalette(false);

            bool hasCenteredMessage = false;
            long currentNanos = Ticker.NanoTime();
            lock (m_console.Messages)
            {
                var node = m_console.Messages.First;
                while (node != null)
                {
                    var msg = node.Value;
                    if (currentNanos - msg.TimeNanos > Constants.MaxMessageVisibleTimeNanos)
                        break;
                    if (msg.IsCentered)
                    {
                        hasCenteredMessage = true;
                        break;
                    }
                    node = node.Next;
                }
            }

            if (hasCenteredMessage)
            {
                DrawCenterMessages(hud);
            }
            else if ((sbarCoverage & StatusBarCoverage.Messages) == 0)
            {
                DrawRecentConsoleMessages(hud);
                DrawCenterMessages(hud);
            }
            
            DrawPause(hud);

            if (automapVisible && m_config.Hud.AutoMap.MapTitle && (sbarCoverage & StatusBarCoverage.MapTitle) == 0)
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
    
    public StatusBarLayoutDef? GetActiveStatusBarLayout()
    {
        if (!WorldStatic.World.DrawHud)
            return null;
        
        var sbarDef = World.ArchiveCollection.Definitions.StatusBarDefinition;
        if (sbarDef.StatusBars.Count == 0)
            return null;

        var layoutName = m_config.Hud.StatusBarLayout.Value;
        
        if (!string.IsNullOrEmpty(layoutName))
        {
            for (int i = 0; i < sbarDef.StatusBars.Count; i++)
            {
                if (sbarDef.StatusBars[i].Name.Equals(layoutName, StringComparison.OrdinalIgnoreCase))
                    return sbarDef.StatusBars[i];
            }
        }
        
        int manualIndex = m_config.Hud.SbarHudMode.Value;
        if (manualIndex >= 0 && manualIndex < sbarDef.StatusBars.Count)
            return sbarDef.StatusBars[manualIndex];
        
        for (int i = 0; i < sbarDef.StatusBars.Count; i++)
        {
            if (!sbarDef.StatusBars[i].FullscreenRender) return sbarDef.StatusBars[i];
        }
        
        return sbarDef.StatusBars[0];
    }
    
    private void SetHudPadding(IHudRenderContext hud)
    {
        if (m_config.Hud.Width == 0)
        {
            m_hudPaddingX = 0;
            return;
        }

        var scale = GetDoomScale(hud, out _);
        var width = m_config.Hud.Width.Value * 320.0 * scale.X;
        m_hudPaddingX = Math.Max((hud.Dimension.Width - (int)width) / 2, 0);
    }

    private void DrawMapHeader(IHudRenderContext hud)
    {
        const int StatusBarSize = 32;
        float doomScale = hud.Dimension.Height / 200f;
        int padding = (int)(2 * m_scale);
        Vec2I pos = new(padding + m_hudPaddingX, -padding);
        
        var activeLayout = GetActiveStatusBarLayout();
        int offsetY = 0;
        if (activeLayout != null)
        {
            offsetY = activeLayout.FullscreenRender ? 0 : (int)(StatusBarSize * doomScale);
        }
        pos.Y -= offsetY;

        string text = World.MapInfo.GetDisplayNameWithPrefix(World.ArchiveCollection.Language);
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

    private void DrawStatInfo(IHudRenderContext hud, bool automapVisible, Vec2I start, ref int topRightY, 
        bool suppressStats = false, bool suppressTime = false)
    {
        if (!m_config.Hud.ShowStats && (!automapVisible || !m_config.Hud.AutoMap.ShowStats))
            return;

        start.X = -m_padding - m_hudPaddingX;
        Vec2I labelPos = start;
        
        if (!suppressStats)
        {
            int maxLabelWidth = 0;
            int maxValueWidth = 0;
            var align = Align.TopRight;

            if (HasTicks)
            {
                for (int i = 0; i < m_renderStats.Length; i++)
                {
                    var renderStat = m_renderStats[i];
                    renderStat.String.Clear();
                    (int current, int max) = renderStat.GetValues(World);
                    renderStat.String = AppendStatString(renderStat.String, current, max);
                    renderStat.RenderLabel = SetRenderableString(renderStat.Label, renderStat.RenderLabel, FixedNumberFont, m_infoFontSize, useDoomScale: false);
                    renderStat.RenderValue = SetRenderableString(renderStat.String.AsSpan(), renderStat.RenderValue, FixedNumberFont, m_infoFontSize,
                        GetStatColor(current, max), useDoomScale: false);
                }
            }

            for (int i = 0; i < m_renderStats.Length; i++)
            {
                var renderStat = m_renderStats[i];
                maxLabelWidth = Math.Max(renderStat.RenderLabel.DrawArea.Width, maxLabelWidth);
                maxValueWidth = Math.Max(renderStat.RenderValue.DrawArea.Width, maxValueWidth);
            }

            labelPos.X = -(maxValueWidth + m_padding + m_hudPaddingX);
            for (int i = 0; i < m_renderStats.Length; i++)
            {
                var renderStat = m_renderStats[i];
                if (!renderStat.ShouldRender(World))
                    continue;
                hud.Text(renderStat.RenderLabel, labelPos, both: align, alpha: m_hudAlpha);
                labelPos.Y += renderStat.RenderLabel.DrawArea.Height;
            }

            labelPos = start;

            for (int i = 0; i < m_renderStats.Length; i++)
            {
                var renderStat = m_renderStats[i];
                if (!renderStat.ShouldRender(World))
                    continue;
                hud.Text(renderStat.RenderValue, labelPos, both: align, alpha: m_hudAlpha);
                labelPos.Y += renderStat.RenderValue.DrawArea.Height;
            }
            labelPos.Y += m_padding;
        }

        if (!suppressTime)
        {
            if (HasTicks)
            {
                TimeSpan ts = TimeSpan.FromSeconds(World.LevelTime / 35);
                m_timeString.Clear();
                m_timeString.Append(ts.Hours, 2);
                m_timeString.Append(':');
                m_timeString.Append(ts.Minutes, 2);
                m_timeString.Append(':');
                m_timeString.Append(ts.Seconds, 2);

                SetRenderableString(m_timeString.AsSpan(), m_renderTimeString, FixedNumberFont, m_infoFontSize, useDoomScale: false);
            }

            hud.Text(m_renderTimeString, labelPos, both: Align.TopRight, alpha: m_hudAlpha);
            labelPos.Y += m_renderTimeString.DrawArea.Height;
        }

        topRightY = labelPos.Y;
    }

    private static SpanString AppendStatString(SpanString str, int current, int max)
    {
        str.Append(current);
        if (max == int.MinValue)
            return str;
        str.Append(" / ");
        str.Append(max);
        return str;
    }

    private Box2I GetNativeDrawBox()
    {
        if (Renderer.GetNativeLetterBoxes(m_config, m_viewport, out var left, out var right))
            return new Box2I(new Vec2I(left.Max.X, left.Min.Y), new Vec2I(right.Min.X, right.Max.Y));

        return new Box2I(new Vec2I(0, 0), new Vec2I(m_viewport.Width, m_viewport.Height));
    }

    private void DrawHudEffects(IHudRenderContext hud)
    {
        if (!WorldStatic.World.DrawHud || (ShaderVars.PaletteColorMode && !m_config.Window.PaletteTrueColorOverlay))
            return;

        var box = GetNativeDrawBox();
        IPowerup? powerup = Player.Inventory.PowerupEffectColor;
        if (powerup is { DrawColor: not null, DrawPowerupEffect: true })
        {
            var alpha = powerup.DrawAlpha;
            if (powerup.PowerupType == PowerupType.Strength)
                alpha *= (float)m_config.Game.BerserkIntensity;

            hud.Clear(box, powerup.DrawColor.Value, alpha);
        }

        if (Player.BonusCount > 0)
        {
            const float PickupScaleAmount = 3f;
            var pickupScale = (Player.BonusCount + 7) / PickupScaleAmount;
            pickupScale *= 1 / PickupScaleAmount;
            hud.Clear(box, PickupColor, Math.Min(pickupScale, 0.2f));
        }

        if (Player.DamageCount > 0)
        {
            const float DamageScaleAmount = 8f;
            var damageScale = Math.Min(Player.DamageCount + 7, 100) / DamageScaleAmount;
            damageScale *= 1 / DamageScaleAmount;
            hud.Clear(box, DamageColor, Math.Min(damageScale, 0.89f) * (float)m_config.Game.PainIntensity);
        }
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
        str.Append($"{Math.Min((int)Math.Round(fps), 9999),4:####}");

        SetRenderableString(str.AsSpan(), renderableString, FixedNumberFont, m_infoFontSize, useDoomScale: false);
        hud.Text(renderableString, (-m_padding - m_hudPaddingX, y), both: Align.TopRight, alpha: m_hudAlpha);

        y += renderableString.DrawArea.Height + FpsMessageSpacing;
    }

    private void DrawPosition(IHudRenderContext hud, ref int topRightY)
    {
        if (!Player.Cheats.IsCheatActive(Helion.World.Cheats.CheatType.ShowPosition))
            return;

        var player = Player.World.GetCameraPlayer();
        var velocity = player.CalcLastVelocity();
        DrawCoordinate(hud, "X", player.Position.X, ref topRightY);
        DrawCoordinate(hud, "Y", player.Position.Y, ref topRightY);
        DrawCoordinate(hud, "Z", player.Position.Z, ref topRightY);
        DrawCoordinate(hud, "A", player.AngleRadians % Math.PI * 180 / MathHelper.Pi, ref topRightY);
        DrawCoordinate(hud, "P", player.PitchRadians % Math.PI * 180 / MathHelper.Pi, ref topRightY);
        DrawCoordinate(hud, "VX", velocity.X, ref topRightY);
        DrawCoordinate(hud, "VY", velocity.Y, ref topRightY);
        DrawCoordinate(hud, "VZ", velocity.Z, ref topRightY);
        topRightY += m_padding;
    }

    void DrawCoordinate(IHudRenderContext hud, string axis, double position, ref int y)
    {
        hud.Text($"{axis}: {Math.Floor(position * 10000) / 10000,9:#####.000}", FixedNumberFont, m_infoFontSize,
            (-m_padding - m_hudPaddingX, y), out Dimension area, TextAlign.Right, both: Align.TopRight,
            color: Color.White, alpha: m_hudAlpha);
        y += area.Height + FpsMessageSpacing;
    }

        
    private void DrawBottomHud(IHudRenderContext hud, bool automapVisible, StatusBarLayoutDef? activeLayout)
    {
        if (!WorldStatic.World.DrawHud)
            return;

        if (activeLayout == null) 
            return;

        bool isWidescreen = hud.WindowDimension.AspectRatio > 4.0f / 3.0f;
        int fps = (int)Math.Round(m_fpsTracker.AverageFramesPerSecond);

        string? consoleMsg = null;
        bool isCentered = false; 

        lock (m_console.Messages)
        {
            if (m_console.Messages.First != null)
            {
                var msg = m_console.Messages.First.Value;
                if (Ticker.NanoTime() - msg.TimeNanos < Constants.MaxMessageVisibleTimeNanos)
                {
                    isCentered = msg.IsCentered;

                    if (!isCentered)
                    {
                        if (msg.StackCount > 1)
                        {
                            var worldMsg = new WorldMessage(msg.Message, 1.0f, msg.StackCount);
                            var span = GetRenderMessageWithCount(worldMsg);
                            consoleMsg = span.ToString(); 
                        }
                        else consoleMsg = msg.Message;
                    }
                }
            }
        }

        var context = new StatusBarContext(World, Player, World.MapInfo, activeLayout, automapVisible, isWidescreen, fps, consoleMsg,
            isCentered, Player.Inventory.HasItemOfClass(Inventory.BackPackBaseClassName), HasTicks);
        m_statusBarRenderer.Draw(hud, activeLayout, context, m_hudPaddingX);
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
            if (powerup is { DrawPowerupEffect: true })
                hudContext.DrawFuzz = true;

            // Push the gun sprite up based on the status bar height
            int yOffset = HudView.GetWeaponOffset(GetActiveStatusBarLayout());
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
        var sector = player.LightCeilingSector3D ?? player.Sector.GetRenderSector(player.Sector, player.Position.Z + player.ViewHeight);
        return (short)((sector.TransferFloorLightSector.LightLevel + sector.TransferCeilingLightSector.LightLevel) / 2);
    }

    private void DrawHudWeapon(IHudRenderContext hud, in FrameState frameState, int yOffset, bool flash)
    {
        string sprite = GetHudWeaponSpriteString(frameState, flash);

        if (!hud.Textures.TryGet(sprite, out var handle, SpriteLookupNamespace))
            return;

        if (!hud.ArchiveCollection.TryGetWeaponFullBrightLookup(frameState.Frame, sprite, out var brightmap))
            brightmap = hud.ArchiveCollection.GetBrightmapFor(sprite, ResourceNamespace.Sprites);
        
        int lightLevel;
        int colorMapIndex;
        bool disableFullbright = m_config.Render.Brightmaps && brightmap?.DisableFullbright == true;
        if ((frameState.Frame.Properties.Bright && !disableFullbright) || Player.DrawFullBright())
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

        if (Player.HasLightAmp())
            colorMapIndex = Constants.ColorMapIndex.LightAmp;

        var camera = World.GetCameraPlayer().GetCamera(m_lastTickInfo.Fraction);
        var colorMixUniforms = Renderer.GetColorMix(Player, camera);
        var colorMix = colorMixUniforms.Global;

        if (colorMixUniforms.Sector != Vec3F.One)
            colorMix = colorMixUniforms.Sector;

        var lightLevelColor =
            ((byte)Math.Min(lightLevel * colorMix.X, 255),
            (byte)Math.Min(lightLevel * colorMix.Y, 255),
            (byte)Math.Min(lightLevel * colorMix.Z, 255));

        var offset = handle.Offset;
        offset.Y += yOffset;
        offset = TranslateDoomOffset(offset);
        var hudBox = GetInterpolatePlayerWeaponBox(hud, handle, offset);

        hud.Image(sprite, hudBox, color: lightLevelColor, colorMapIndex: colorMapIndex, resourceNamespace: SpriteLookupNamespace, brightmapName: brightmap?.BrightmapName);
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

    private static HudBox TranslateDoomImageDimensions(double x, double y, int width, int height, Vec2D scale)
    {
        var start = (new Vec2D(x, y) * scale).Int;
        var end = (new Vec2D(x + width, y + height) * scale).Int;
        return new HudBox((start.X, start.Y), (end.X, end.Y));
    }

    private static Vec2D GetDoomScale(IHudRenderContext ctx, out int centeredOffsetX)
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

        Color color;
        bool target = Player.CrosshairTarget.Get() != null;
        bool shouldShrink = m_config.Hud.CrosshairTargetShrink.Value && target;
        int crosshairLength = shouldShrink ? (int)(Length * 0.8f) : Length;

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
        center -= HudView.GetViewPortOffset(GetActiveStatusBarLayout(), m_viewport);

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
                var size = shouldShrink ? 1 : 1.5;
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

    private static Color GetStatColor(int current, int total)
    {
        if (current >= total && total != int.MinValue)
            return Color.LightGreen;
        return Color.White;
    }

    private RenderableString SetRenderableString(ReadOnlySpan<char> charSpan, RenderableString renderableString, string font, int fontSize, Color? drawColor = null,
        bool useDoomScale = true)
    {
        if (!HasTicks)
            return renderableString;

        renderableString.Set(World.ArchiveCollection.DataCache, charSpan, GetFontOrDefault(font),
            fontSize, drawColor: drawColor);
        if (useDoomScale)
            renderableString.DrawArea = new(renderableString.DrawArea.Width, (int)(renderableString.DrawArea.Height * DoomVerticalScale));
        return renderableString;
    }

    private bool HasTicks => m_lastTickInfo.Ticks > 0;

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
                
                if (msg.IsCentered)
                    continue;

                if (messagesDrawn >= maxHudMessages || MessageTooOldToDraw(msg, World, m_console, m_parent.OptionsLastClosedNanos))
                    break;

                long timeSinceMessage = currentNanos - msg.TimeNanos;
                if (timeSinceMessage > Constants.MaxMessageVisibleTimeNanos || m_parent.ConsoleLayer != null)
                    break;
                                
                m_messages.Add(new(msg.Message, CalculateFade(timeSinceMessage), msg.StackCount));
                messagesDrawn++;
                lastMessageTime = timeSinceMessage;
            }

            int fontSize = (int)(1.25 * hud.GetFontMaxHeight(SmallHudFont));
            int slideOffsetY = m_messages.Count <= 1 ? 0 : CalculateSlide(hud, lastMessageTime);
            var scale = GetDoomScale(hud, out _);
            var width = m_config.Hud.Width.Value * 320.0 * scale.X / m_scale;

            if (m_config.Hud.Width.Value == 0)
                width = hud.Dimension.Width / m_scale;

            for (int i = m_messages.Count - 1; i >= 0; i--)
            {
                var msg = m_messages[i];
                var renderMessageString = msg.Message;
                var renderMessageLength = msg.Message.Length;
                if (msg.MessageCount > 1)
                {
                    var renderMessage = GetRenderMessageWithCount(msg);
                    StringBuffer.Clear(m_renderMessageBufferString);
                    m_renderMessageBufferString = StringBuffer.Append(m_renderMessageBufferString, renderMessage.AsSpan());
                    renderMessageString = m_renderMessageBufferString;
                    renderMessageLength = StringBuffer.StringLength(m_renderMessageBufferString);
                }

                hud.LineWrap(renderMessageString, SmallHudFont, fontSize, (int)width, m_lineWrapStrings, out int _, length: renderMessageLength);

                foreach (var line in m_lineWrapStrings)
                {
                    hud.Text(line.Source.AsSpan(line.Start, line.Length), SmallHudFont, fontSize, (LeftOffset + m_hudPaddingX, offsetY + slideOffsetY),
                        out Dimension drawArea, window: Align.TopLeft, scale: m_scale, alpha: msg.Alpha * m_hudAlpha);
                    offsetY += drawArea.Height + MessageSpacing;
                }               
            }

            m_messages.Clear();
        }
    }

    private SpanString GetRenderMessageWithCount(WorldMessage msg)
    {
        m_renderMessageSpan.Clear();
        m_renderMessageSpan.Append(msg.Message);
        m_renderMessageSpan.Append(" (x");
        m_renderMessageSpan.Append(msg.MessageCount);
        m_renderMessageSpan.Append(')');
        return m_renderMessageSpan;
    }

    private void DrawCenterMessages(IHudRenderContext hud)
    {
        ConsoleMessage? centerMsg = null;
        long currentNanos = Ticker.NanoTime();
        
        lock (m_console.Messages)
        {
            var node = m_console.Messages.First;
            while (node != null)
            {
                var msg = node.Value;

                if (currentNanos - msg.TimeNanos > Constants.MaxMessageVisibleTimeNanos)
                    break;

                if (msg.IsCentered)
                {
                    centerMsg = msg;
                    break;
                }
                node = node.Next;
            }
        }

        if (centerMsg == null)
            return;

        long timeSinceMessage = currentNanos - centerMsg.TimeNanos;
        float alpha = CalculateFade(timeSinceMessage);
        
        int yPos = m_viewport.Height / 3;

        hud.LineWrap(centerMsg.Message, SmallHudFont, m_infoFontSize, m_viewport.Width, m_lineWrapStrings, out _);

        foreach (var line in m_lineWrapStrings)
        {
            hud.Text(line.Source.AsSpan(line.Start, line.Length), SmallHudFont, m_infoFontSize, (0, yPos),
                out Dimension drawArea, both: Align.TopMiddle, alpha: alpha * m_hudAlpha);
            yPos += drawArea.Height + MessageSpacing;
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
        const long SlideNanoRange = Constants.MaxMessageVisibleTimeNanos - MessageTransitionSpan;
        if (timeSinceMessage < SlideNanoRange)
            return 0;

        var dim = hud.MeasureText("I", SmallHudFont, 8);
        double frac = 1.0 - (double)(Constants.MaxMessageVisibleTimeNanos - timeSinceMessage) / MessageTransitionSpan;
        return (int)(-(dim.Height + MessageSpacing) * frac * m_config.Hud.GetHudScaled(1));
    }

    private static float CalculateFade(long timeSinceMessage)
    {
        const long OpaqueNanoRange = Constants.MaxMessageVisibleTimeNanos - MessageTransitionSpan;
        if (timeSinceMessage < OpaqueNanoRange)
            return 1.0f;

        double fractionIntoFadeRange = (double)(timeSinceMessage - OpaqueNanoRange) / MessageTransitionSpan;
        return 1.0f - (float)fractionIntoFadeRange;
    }
}
