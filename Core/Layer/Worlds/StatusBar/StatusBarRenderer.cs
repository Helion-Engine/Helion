using System;
using System.Collections.Generic;
using System.Globalization;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Render.Common;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Resources;
using Helion.Resources.Definitions.StatusBar;
using Helion.Resources.Definitions.StatusBar.Enums;
using Helion.Resources.Definitions.MapInfo;
using Helion.Strings;
using Helion.Util;
using Helion.World.Entities.Inventories;
using Helion.World.Entities.Players;
using Helion.World.StatusBar;
using Helion.Resources.Archives.Collection;
using Helion.World;

namespace Helion.Layer.Worlds.StatusBar;

[Flags]
public enum StatusBarCoverage
{
    None = 0,
    Stats = 1 << 0,      // for stat_totals
    Time = 1 << 1,       // for time
    Messages = 1 << 2,   // for message
    MapTitle = 1 << 3,   // for level_title
    FPS = 1 << 4         // for fps_counter
}

public class StatusBarRenderer
{
    // Caches
    private static readonly Dictionary<string, int> Type1WidthCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, int> HudType1WidthCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> FontPatchCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> HudFontPatchCache = new(StringComparer.OrdinalIgnoreCase);
    
    // Mapping SBARDEF stems to Helion Internal Fonts to enable grayscale tinting and better rendering
    private static readonly Dictionary<string, string> StemToHelionFontMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "STCFN", Constants.Fonts.SmallGray },
        { "STT", Constants.Fonts.LargeHud },
        { "STG", Constants.Fonts.SmallGrayFixedWidthNumbers },
        { "STYS", "HudYellowNumbers" }
    };
    
    private static readonly Dictionary<string, Color> StandardTextColors = new(StringComparer.OrdinalIgnoreCase)
    {
        { "CRBRICK", new Color(154, 50, 50) },
        { "CRTAN", new Color(195, 175, 120) },
        { "CRGRAY", Color.Gray }, 
        { "CRGREY", Color.Gray },
        { "CRGREEN", new Color(0x90, 0xEE, 0x90) },
        { "CRBROWN", new Color(120, 80, 20) },
        { "CRGOLD", Color.Gold }, 
        { "CRRED", Color.Red },
        { "CRBLUE", Color.Blue },
        { "CRORANGE", Color.Orange },
        { "CRYELLOW", Color.Yellow },
        { "CRWHITE", Color.White },
        { "CRBLACK", Color.Black },
        { "CRUNTRANSLATED", Color.White }
    };

    private readonly IWorld m_world;
    private readonly ArchiveCollection m_archiveCollection;
    private readonly List<RenderGlyph> m_glyphCache = [];
    private readonly List<CoordData> m_coordPartsCache = [];
    private readonly Dictionary<string, StatusBarNumberFontDef> m_fontNumberLookup = [];
    private readonly Dictionary<string, StatusBarHudFontDef> m_hudFontLookup = [];
    private readonly SpanString m_fmtSpan = new();
    private readonly SpanString m_lookupKeySpan = new(64);

    private readonly record struct RenderGlyph(string Patch, int Width, int Offset);
    private readonly record struct CoordData(string Label, int Value, int LabelWidth, int ValWidth);

    public StatusBarRenderer(IWorld world)
    {
        m_world = world;
        m_archiveCollection = world.ArchiveCollection;
        var sbarDef = world.ArchiveCollection.Definitions.StatusBarDefinition;
        foreach (var f in sbarDef.NumberFonts)
            m_fontNumberLookup[f.Name] = f;

        foreach (var f in sbarDef.HudFonts)
            m_hudFontLookup[f.Name] = f;
    }

    public static StatusBarCoverage GetCoverage(StatusBarLayoutDef layout)
    {
        if (layout.Children.Count == 0) return StatusBarCoverage.None;
        return ScanChildren(layout.Children);
    }

    private static StatusBarCoverage ScanChildren(List<StatusBarElementWrapper> children)
    {
        StatusBarCoverage mask = StatusBarCoverage.None;
        foreach (var child in children)
        {
            if (child.Component != null)
            {
                switch (child.Component.ComponentType)
                {
                    case StatusBarComponentType.StatTotals: mask |= StatusBarCoverage.Stats; break;
                    case StatusBarComponentType.Time: mask |= StatusBarCoverage.Time; break;
                    case StatusBarComponentType.Message: mask |= StatusBarCoverage.Messages; break;
                    case StatusBarComponentType.LevelTitle: mask |= StatusBarCoverage.MapTitle; break;
                    case StatusBarComponentType.FpsCounter: mask |= StatusBarCoverage.FPS; break;
                }
                // Recurse into component children
                if (child.Component.Children != null)
                    mask |= ScanChildren(child.Component.Children);
            }
            
            // Recurse into other containers
            if (child.Canvas?.Children != null) mask |= ScanChildren(child.Canvas.Children);
            if (child.Graphic?.Children != null) mask |= ScanChildren(child.Graphic.Children);
            if (child.Face?.Children != null) mask |= ScanChildren(child.Face.Children);
            if (child.FaceBackground?.Children != null) mask |= ScanChildren(child.FaceBackground.Children);
            if (child.Animation?.Children != null) mask |= ScanChildren(child.Animation.Children);
            if (child.Carousel?.Children != null) mask |= ScanChildren(child.Carousel.Children);
            if (child.Number?.Children != null) mask |= ScanChildren(child.Number.Children);
            if (child.Percent?.Children != null) mask |= ScanChildren(child.Percent.Children);
        }
        return mask;
    }

    public void Draw(IHudRenderContext hud, StatusBarLayoutDef layout, StatusBarContext context)
    {
        int width = 320;
        int height = 200;
        
        hud.PushVirtualDimension((width, height), ResolutionScale.Center, Constants.DoomVirtualAspectRatio);

        int yOffset = layout.FullscreenRender ? 0 : (200 - layout.Height);
        Vec2I rootPos = (0, yOffset);
        
        float widescreenOffset = GetWidescreenOffset(hud);

        if (!layout.FullscreenRender)
        {
            string? fillFlat = layout.FillFlat;
            if (string.IsNullOrEmpty(fillFlat))
                fillFlat = m_world.GameInfo.BorderFlat;

            if (!string.IsNullOrEmpty(fillFlat) && hud.Textures.TryGet(fillFlat, out var bgHandle))
            {
                if (widescreenOffset > 0)
                {
                    int bgWidth = bgHandle.Dimension.Width;
                    int bgHeight = bgHandle.Dimension.Height;
                    if (bgHeight <= 0) bgHeight = 64;

                    // If the layout claims the full 200 height (like Woof's overlay), 
                    // we assume the "Solid" part is only the bottom 32 pixels (Standard Doom Bar).
                    // We clamp the border drawing to that area so we don't cover the 3D view on the sides.
                    int borderY = yOffset;
                    int borderHeight = layout.Height;

                    if (layout.Height >= 200)
                    {
                        borderHeight = 32;
                        borderY = 200 - 32;
                    }

                    int verticalTiles = (borderHeight + bgHeight - 1) / bgHeight; 
                    int iterations = (int)(widescreenOffset / bgWidth) + 1;

                    // Draw Left Pillars
                    int xPos = -bgWidth;
                    for (int i = 0; i < iterations; i++)
                    {
                        for (int y = 0; y < verticalTiles; y++)
                        {
                            hud.Image(fillFlat, (xPos, borderY + (y * bgHeight)), anchor: Align.TopLeft);
                        }
                        xPos -= bgWidth;
                    }

                    // Draw Right Pillars
                    xPos = 320;
                    for (int i = 0; i < iterations; i++)
                    {
                        for (int y = 0; y < verticalTiles; y++)
                        {
                            hud.Image(fillFlat, (xPos, borderY + (y * bgHeight)), anchor: Align.TopLeft);
                        }
                        xPos += bgWidth;
                    }
                }
            }
        }

        foreach (var child in layout.Children)
        {
            DrawElementWrapper(hud, child, rootPos, layout.Height, context, widescreenOffset);
        }

        hud.PopVirtualDimension();
    }

    private void DrawElementWrapper(IHudRenderContext hud, StatusBarElementWrapper wrapper, Vec2I parentPos, int containerHeight, StatusBarContext context, float widescreenOffset)
    {
        if (wrapper.Canvas != null)
            DrawBase(hud, wrapper.Canvas, parentPos, containerHeight, context, widescreenOffset);
        else if (wrapper.Graphic != null)
            DrawGraphic(hud, wrapper.Graphic, parentPos, containerHeight, context, widescreenOffset);
        else if (wrapper.Number != null)
            DrawNumber(hud, wrapper.Number, parentPos, containerHeight, context, isPercent: false, widescreenOffset);
        else if (wrapper.Percent != null)
            DrawNumber(hud, wrapper.Percent, parentPos, containerHeight, context, isPercent: true, widescreenOffset);
        else if (wrapper.Face != null)
            DrawFace(hud, wrapper.Face, parentPos, containerHeight, context, widescreenOffset);
        else if (wrapper.FaceBackground != null)
            DrawFaceBackground(hud, wrapper.FaceBackground, parentPos, containerHeight, context, widescreenOffset);
        else if (wrapper.Animation != null)
            DrawAnimation(hud, wrapper.Animation, parentPos, containerHeight, context, widescreenOffset);
        else if (wrapper.Component != null)
            DrawComponent(hud, wrapper.Component, parentPos, containerHeight, context, widescreenOffset);
        else if (wrapper.Carousel != null)
            DrawCarousel(hud, wrapper.Carousel, parentPos, containerHeight, context, widescreenOffset);
    }

    private void DrawBase(IHudRenderContext hud, StatusBarCanvasDef def, Vec2I parentPos, int containerHeight, StatusBarContext context, float widescreenOffset)
    {
        if (!StatusBarConditionResolver.Evaluate(context, def.Conditions))
            return;

        Vec2I currentPos = ResolvePosition(def, parentPos, widescreenOffset);
        DrawChildren(hud, def, currentPos, containerHeight, context, widescreenOffset);
    }

    private void DrawGraphic(IHudRenderContext hud, StatusBarGraphicDef graphic, Vec2I parentPos, int containerHeight, StatusBarContext context, float widescreenOffset)
    {
        if (!StatusBarConditionResolver.Evaluate(context, graphic.Conditions))
            return;

        Vec2I currentPos = ResolvePosition(graphic, parentPos, widescreenOffset);

        if (!string.IsNullOrEmpty(graphic.Patch))
        {
            Align align = ConvertAlignment(graphic.Alignment);
            
            if (graphic.MidOffset != 0)
            {
                currentPos.X += graphic.MidOffset;
            }

            // Waiting on Image Cropping, as Woof! dev plans on changing implementation.

            float alpha = graphic.Translucency ? 0.5f : 1.0f;
            DrawSBarTexture(hud, graphic.Patch, currentPos, align, useDoomOffsets: true, 
                translation: graphic.Translation, alpha: alpha);
        }

        DrawChildren(hud, graphic, currentPos, containerHeight, context, widescreenOffset);
    }

    private void DrawFace(IHudRenderContext hud, StatusBarFaceDef face, Vec2I parentPos, int containerHeight, StatusBarContext context, float widescreenOffset)
    {
        if (!StatusBarConditionResolver.Evaluate(context, face.Conditions))
            return;

        Vec2I currentPos = ResolvePosition(face, parentPos, widescreenOffset);
        string patch = context.Player.StatusBar.GetFacePatch();

        if (!string.IsNullOrEmpty(patch))
        {
            Align align = ConvertAlignment(face.Alignment);
            float alpha = face.Translucency ? 0.5f : 1.0f;
            DrawSBarTexture(hud, patch, currentPos, align, useDoomOffsets: true, 
                translation: face.Translation, alpha: alpha);
        }

        DrawChildren(hud, face, currentPos, containerHeight, context, widescreenOffset);
    }

    private void DrawFaceBackground(IHudRenderContext hud, StatusBarFaceDef faceBg, Vec2I parentPos, int containerHeight, StatusBarContext context, float widescreenOffset)
    {
        if (!StatusBarConditionResolver.Evaluate(context, faceBg.Conditions))
            return;

        Vec2I currentPos = ResolvePosition(faceBg, parentPos, widescreenOffset);
        string patch = GetFaceBackgroundPatch(context.Player);

        if (!string.IsNullOrEmpty(patch))
        {
            Align align = ConvertAlignment(faceBg.Alignment);
            float alpha = faceBg.Translucency ? 0.5f : 1.0f;
            DrawSBarTexture(hud, patch, currentPos, align, useDoomOffsets: true, 
                translation: faceBg.Translation, alpha: alpha);
        }

        DrawChildren(hud, faceBg, currentPos, containerHeight, context, widescreenOffset);
    }

    private void DrawAnimation(IHudRenderContext hud, StatusBarAnimationDef anim, Vec2I parentPos, int containerHeight, StatusBarContext context, float widescreenOffset)
    {
        if (!StatusBarConditionResolver.Evaluate(context, anim.Conditions))
            return;

        Vec2I currentPos = ResolvePosition(anim, parentPos, widescreenOffset);

        if (anim.Frames.Count > 0)
        {
            double totalDuration = 0;
            foreach (var frame in anim.Frames)
                totalDuration += frame.Duration;

            if (totalDuration > 0)
            {
                double timePerLoop = totalDuration * Constants.TicksPerSecond;
                long currentTick = m_world.LevelTime;
                double animTime = currentTick % timePerLoop;

                string patch = anim.Frames[0].Lump;
                double timeAccumulator = 0;

                foreach (var frame in anim.Frames)
                {
                    timeAccumulator += frame.Duration * Constants.TicksPerSecond;
                    if (animTime < timeAccumulator)
                    {
                        patch = frame.Lump;
                        break;
                    }
                }

                Align align = ConvertAlignment(anim.Alignment);
                DrawSBarTexture(hud, patch, currentPos, align, useDoomOffsets: true, 
                    translation: anim.Translation, alpha: 1.0f);
            }
        }

        DrawChildren(hud, anim, currentPos, containerHeight, context, widescreenOffset);
    }

    private void DrawComponent(IHudRenderContext hud, StatusBarComponentDef comp, Vec2I parentPos, int containerHeight, StatusBarContext context, float widescreenOffset)
    {
        if (!StatusBarConditionResolver.Evaluate(context, comp.Conditions))
            return;

        Vec2I pos = ResolvePosition(comp, parentPos, widescreenOffset);
        var config = m_world.Config.Hud;
        
        float alpha = comp.Translucency ? 0.5f : 1.0f;
        StatusBarAlignment alignment = comp.Alignment;

        m_hudFontLookup.TryGetValue(comp.Font, out var fontDef);       
        
        int fontHeight = 8;
        if (fontDef != null && StemToHelionFontMap.TryGetValue(fontDef.Stem, out var helionFontName))
        {
            int h = hud.GetFontMaxHeight(helionFontName);
            if (h > 0) fontHeight = h;
        }
        
        switch (comp.ComponentType)
        {
            case StatusBarComponentType.StatTotals:
                if (!config.ShowStats.Value) return;
                DrawStatTotals(hud, comp, pos, fontDef, fontHeight, alpha);
                break;

            case StatusBarComponentType.Time:
                TimeSpan t = TimeSpan.FromSeconds(m_world.LevelTime / 35.0);
                m_fmtSpan.Clear();
                m_fmtSpan.Append((int)t.TotalHours, 2);
                m_fmtSpan.Append(':');
                m_fmtSpan.Append(t.Minutes, 2);
                m_fmtSpan.Append(':');
                m_fmtSpan.Append(t.Seconds, 2);
                RenderLines(hud, m_fmtSpan.AsSpan(), pos, fontDef, fontHeight, alignment, comp.Translation, alpha);
                break;
            case StatusBarComponentType.Coordinates:
                DrawCoordinates(hud, comp, pos, fontDef, fontHeight, alpha, context);
                break;
            case StatusBarComponentType.Speedometer:
                // A joke case for Woof!, handled anyway!
                string speedText = GetSpeedometerText(context.Player);
                RenderLines(hud, speedText.AsSpan(), pos, fontDef, fontHeight, alignment, comp.Translation, alpha);
                break;
            case StatusBarComponentType.LevelTitle:
                string levelTitle = m_world.MapInfo.GetDisplayNameWithPrefix(m_archiveCollection.Language);
                RenderLines(hud, levelTitle.AsSpan(), pos, fontDef, fontHeight, alignment, comp.Translation, alpha);
                break;
            case StatusBarComponentType.FpsCounter:
                if (!config.ShowFPS.Value) return;
                m_fmtSpan.Clear();
                m_fmtSpan.Append(context.Fps);
                RenderLines(hud, m_fmtSpan.AsSpan(), pos, fontDef, fontHeight, alignment, comp.Translation, alpha);
                break;
            case StatusBarComponentType.Message:
                string msg = context.ConsoleMessage ?? string.Empty;
                if (context.IsMessageCentered)
                {
                    pos = (160, 66); 
                    alignment = StatusBarAlignment.HCenter;
                }
                RenderLines(hud, msg.AsSpan(), pos, fontDef, fontHeight, alignment, comp.Translation, alpha);
                break;
            case StatusBarComponentType.AnnounceLevelTitle:
                double duration = comp.Duration > 0 ? comp.Duration : 2.5;
                double fadeInTime = 0.25;
                double fadeOutTime = 1.0;
                double timeSinceStart = m_world.LevelTime / Constants.TicksPerSecond;

                if (timeSinceStart > duration + fadeOutTime) 
                    return; 

                if (timeSinceStart < fadeInTime)
                {
                    alpha *= (float)(timeSinceStart / fadeInTime);
                }
                else if (timeSinceStart > duration)
                {
                    double progress = (timeSinceStart - duration) / fadeOutTime;
                    alpha *= (float)(1.0 - progress);
                }
                
                string annTitle = m_world.MapInfo.GetDisplayNameWithPrefix(m_archiveCollection.Language);
                RenderLines(hud, annTitle.AsSpan(), pos, fontDef, fontHeight, alignment, comp.Translation, alpha);
                break;
            case StatusBarComponentType.RenderStats: 
            case StatusBarComponentType.CommandHistory: 
            case StatusBarComponentType.Chat:
                // Helion is singleplayer, no chat functionality.
                break;
        }

        DrawChildren(hud, comp, pos, containerHeight, context, widescreenOffset);
    }
    
    private void RenderLines(IHudRenderContext hud, ReadOnlySpan<char> text, Vec2I pos, 
        StatusBarHudFontDef? fontDef, int fontHeight, StatusBarAlignment alignment, string? translation, float alpha)
    {
        if (text.IsEmpty) return;

        Vec2I drawPos = pos;
        
        int lineStart = 0;
        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] == '\n')
            {
                var line = text.Slice(lineStart, i - lineStart);
                DrawSingleLine(hud, line, drawPos, fontDef, alignment, translation, alpha);
                drawPos.Y += fontHeight;
                lineStart = i + 1;
            }
        }
        
        if (lineStart <= text.Length)
        {
            var line = text.Slice(lineStart);
            if (line.Length > 0 || lineStart < text.Length)
                DrawSingleLine(hud, line, drawPos, fontDef, alignment, translation, alpha);
        }
    }

    private void DrawSingleLine(IHudRenderContext hud, ReadOnlySpan<char> line, Vec2I drawPos, 
        StatusBarHudFontDef? fontDef, StatusBarAlignment alignment, string? translation, float alpha)
    {
        if (line.IsEmpty) return;

        int drawnWidth = 0;
        if (fontDef != null)
        {
            drawnWidth = DrawHudText(hud, line, fontDef, drawPos, alignment, translation, alpha);
        }
        
        // Fallback
        if (drawnWidth == 0)
        {
            Align align = ConvertAlignment(alignment);
            hud.Text(line, Constants.Fonts.Small, 8, drawPos, both: align, alpha: alpha);
        }
    }

    /// <summary>
    /// Draws text using SBARDEF HudFont. Returns the total width of the drawn text in pixels.
    /// </summary>
    private int DrawHudText(IHudRenderContext hud, ReadOnlySpan<char> text, StatusBarHudFontDef fontDef, 
        Vec2I pos, StatusBarAlignment alignment, string? translation, float alpha, bool draw = true)
    {
        Color? drawColor = null;
        if (!string.IsNullOrEmpty(translation))
        {
            if (StandardTextColors.TryGetValue(translation, out var c))
                drawColor = c;
        }

        if (StemToHelionFontMap.TryGetValue(fontDef.Stem, out string? helionFont))
        {
            if (draw)
            {
                Align anchor;
                TextAlign textAlign = TextAlign.Left;
                
                bool hCenter = (alignment & StatusBarAlignment.HCenter) != 0;
                bool right = (alignment & StatusBarAlignment.Right) != 0;
                bool vCenter = (alignment & StatusBarAlignment.VCenter) != 0;
                bool bottom = (alignment & StatusBarAlignment.Bottom) != 0;

                if (hCenter) 
                {
                    anchor = bottom ? Align.BottomMiddle : (vCenter ? Align.Center : Align.TopMiddle);
                    textAlign = TextAlign.Center;
                }
                else if (right)
                {
                    anchor = bottom ? Align.BottomRight : (vCenter ? Align.MiddleRight : Align.TopRight);
                    textAlign = TextAlign.Right;
                }
                else
                {
                    anchor = bottom ? Align.BottomLeft : (vCenter ? Align.MiddleLeft : Align.TopLeft);
                }

                hud.Text(text, helionFont, 8, pos, textAlign: textAlign, 
                    window: Align.TopLeft, anchor: anchor, 
                    color: drawColor, alpha: alpha);
            }
                
            return hud.MeasureText(text, helionFont, 8).Width;
        }

        int totalWidth = 0;
        int maxHeight = 0;
        
        m_glyphCache.Clear();
        
        int monoWidth = 0;
        // Type 1: Monospaced, based off the widest glyph
        if (fontDef.Type == 1)
        {
            if (!HudType1WidthCache.TryGetValue(fontDef.Stem, out monoWidth))
            {
                monoWidth = 0;
                for (char c = '!'; c <= '_'; c++)
                {
                    string patch = GetHudFontPatch(fontDef, c);
                    if (ResolveGlyph(hud, patch, out int w, out _))
                    {
                        monoWidth = Math.Max(monoWidth, w);
                    }
                }
                HudType1WidthCache[fontDef.Stem] = monoWidth;
            }
        }
        else if (fontDef.Type == 0) // Type 0: Monospaced based on '0'
        {
            string zero = GetHudFontPatch(fontDef, '0');
            if (hud.Textures.TryGet(zero, out var zh)) monoWidth = zh.Dimension.Width;
        }

        foreach (char originalChar in text)
        {
            int width = 0;
            string patch = string.Empty;
            char c = originalChar;

            if (c == ' ')
            {
                string bang = GetHudFontPatch(fontDef, '!');
                if (hud.Textures.HasImage(bang)) 
                {
                    if (hud.Textures.TryGet(bang, out var h)) width = h.Dimension.Width;
                }
                else width = 4;
            }
            else
            {
                if (char.IsLower(c)) c = char.ToUpper(c, CultureInfo.InvariantCulture);

                patch = GetHudFontPatch(fontDef, c);
                bool found = ResolveGlyph(hud, patch, out width, out int height);

                if (!found && c != originalChar)
                {
                    string rawPatch = GetHudFontPatch(fontDef, originalChar);
                    if (ResolveGlyph(hud, rawPatch, out int rawWidth, out int rawHeight))
                    {
                        patch = rawPatch;
                        width = rawWidth;
                        height = rawHeight;
                        found = true;
                    }
                }

                if (found)
                {
                    maxHeight = Math.Max(maxHeight, height);
                }
                else
                {
                    patch = string.Empty;
                }
            }

            if (monoWidth > 0)
            {
                 width = monoWidth;
            }

            m_glyphCache.Add(new RenderGlyph(patch, width, 0));
            totalWidth += width;
        }

        if (m_glyphCache.Count == 0 && text.Length > 0) return 0;
        if (!draw) return totalWidth;

        int drawX = pos.X;
        int drawY = pos.Y;

        if ((alignment & StatusBarAlignment.HCenter) != 0) drawX -= totalWidth / 2;
        else if ((alignment & StatusBarAlignment.Right) != 0) drawX -= totalWidth;

        if ((alignment & StatusBarAlignment.Bottom) != 0) drawY -= maxHeight;
        else if ((alignment & StatusBarAlignment.VCenter) != 0) drawY -= maxHeight / 2;

        Align align = Align.TopLeft;

        foreach (var g in m_glyphCache)
        {
            if (!string.IsNullOrEmpty(g.Patch))
            {
                DrawSBarTexture(hud, g.Patch, (drawX, drawY), align, useDoomOffsets: false, 
                    translation: translation, alpha: alpha);
            }
            drawX += g.Width;
        }

        return totalWidth;
    }

    private void DrawCarousel(IHudRenderContext hud, StatusBarCarouselDef carousel, Vec2I parentPos, int containerHeight, StatusBarContext context, float widescreenOffset)
    {
        if (!StatusBarConditionResolver.Evaluate(context, carousel.Conditions))
            return;

        Vec2I pos = ResolvePosition(carousel, parentPos, widescreenOffset);
        pos.X = 160 + carousel.X; 
        
        if (context.Player.Weapon != null)
        {
            string icon = context.Player.Weapon.Definition.Properties.Inventory.Icon;
            if (!string.IsNullOrEmpty(icon))
            {
                Align align = ConvertAlignment(carousel.Alignment);
                float alpha = carousel.Translucency ? 0.5f : 1.0f;
                DrawSBarTexture(hud, icon, pos, align, useDoomOffsets: true, 
                    translation: carousel.Translation, alpha: alpha);
            }
        }

        DrawChildren(hud, carousel, pos, containerHeight, context, widescreenOffset);
    }

    private static void DrawSBarTexture(IHudRenderContext hud, string patch, Vec2I pos, Align align, 
        bool useDoomOffsets, string? translation = null, float alpha = 1.0f)
    {
        bool isRight = align == Align.TopRight || align == Align.MiddleRight || align == Align.BottomRight;
        bool isBottom = align == Align.BottomLeft || align == Align.BottomMiddle || align == Align.BottomRight;

        if (isRight) pos.X -= 1;
        if (isBottom) pos.Y -= 1;

        if (!hud.Textures.TryGet(patch, out var handle))
        {
            if (!hud.Textures.TryGet(patch, out handle, ResourceNamespace.Sprites))
                return;
        }

        Vec2I drawPos = pos;
        if (useDoomOffsets)
            drawPos += RenderDimensions.TranslateDoomOffset(handle.Offset);

        Color? drawColor = null;
        if (!string.IsNullOrEmpty(translation))
        {
            if (StandardTextColors.TryGetValue(translation, out var c))
                drawColor = c;
        }

        if (hud.Textures.HasImage(patch))
        {
            hud.Image(patch, drawPos, anchor: align, alpha: alpha, color: drawColor);
        }
        else if (hud.Textures.HasImage(patch, ResourceNamespace.Sprites))
        {
            hud.Image(patch, drawPos, anchor: align, resourceNamespace: ResourceNamespace.Sprites, alpha: alpha, color: drawColor);
        }
    }

    private void DrawNumber(IHudRenderContext hud, StatusBarNumberDef number, Vec2I parentPos, int containerHeight, 
        StatusBarContext context, bool isPercent, float widescreenOffset)
    {
        if (!StatusBarConditionResolver.Evaluate(context, number.Conditions))
            return;

        int value = ResolveNumberValue(context.Player, number.Type, number.Param);
        
        if (number.MaxLength > 0)
        {
            int maxVal = (int)Math.Pow(10, number.MaxLength) - 1;
            int minVal = -(int)Math.Pow(10, number.MaxLength - 1) + 1;
            if (value > maxVal) value = maxVal;
            if (value < minVal) value = minVal;
        }

        if (!m_fontNumberLookup.TryGetValue(number.Font, out var fontDef))
            return;

        m_fmtSpan.Clear();
        m_fmtSpan.Append(value);
        if (isPercent) m_fmtSpan.Append('%');
        
        ReadOnlySpan<char> text = m_fmtSpan.AsSpan();

        Vec2I pos = ResolvePosition(number, parentPos, widescreenOffset);

        Color? drawColor = null;
        if (!string.IsNullOrEmpty(number.Translation))
        {
            if (StandardTextColors.TryGetValue(number.Translation, out var c))
                drawColor = c;
        }

        float alpha = number.Translucency ? 0.5f : 1.0f;

        int totalWidth = 0;
        int monoWidth = 0; 

        if (fontDef.Type == 0)
        {
            string zeroPatch = GetFontPatch(hud, fontDef, '0');
            if (hud.Textures.TryGet(zeroPatch, out var zeroHandle))
                monoWidth = zeroHandle.Dimension.Width;
        }
        else if (fontDef.Type == 1)
        {
            if (!Type1WidthCache.TryGetValue(fontDef.Stem, out monoWidth))
            {
                monoWidth = 0;
                for (char d = '0'; d <= '9'; d++)
                {
                    string dPatch = GetFontPatch(hud, fontDef, d);
                    if (hud.Textures.TryGet(dPatch, out var dHandle))
                        monoWidth = Math.Max(monoWidth, dHandle.Dimension.Width);
                }
                Type1WidthCache[fontDef.Stem] = monoWidth;
            }
        }
        
        m_glyphCache.Clear();

        foreach (char c in text)
        {
            string patch = GetFontPatch(hud, fontDef, c);
            int width;
            int xOffset = 0;

            if (hud.Textures.TryGet(patch, out var handle))
                width = handle.Dimension.Width;
            else
            {
                 if (hud.Textures.TryGet(patch, out handle, ResourceNamespace.Sprites))
                     width = handle.Dimension.Width;
                 else
                     continue;
            }
            
            if (fontDef.Type == 0 || fontDef.Type == 1)
            {
                if (monoWidth > 0)
                {
                    if (width < monoWidth)
                        xOffset = (monoWidth - width) / 2;
                    width = monoWidth;
                }
            }

            m_glyphCache.Add(new RenderGlyph(patch, width, xOffset));
            totalWidth += width;
        }

        int drawX = pos.X;
        int drawY = pos.Y;

        if ((number.Alignment & StatusBarAlignment.HCenter) != 0)
            drawX -= totalWidth / 2;
        else if ((number.Alignment & StatusBarAlignment.Right) != 0)
            drawX -= totalWidth;

        Align yAnchor = Align.TopLeft;
        if ((number.Alignment & StatusBarAlignment.Bottom) != 0) yAnchor = Align.BottomLeft;
        
        bool isBottomAnchor = yAnchor == Align.BottomLeft;

        foreach (var g in m_glyphCache)
        {
            Vec2I drawPos = (drawX + g.Offset, drawY);
            
            if (isBottomAnchor) drawPos.Y -= 1;

            if (hud.Textures.HasImage(g.Patch))
                hud.Image(g.Patch, drawPos, anchor: yAnchor, color: drawColor, alpha: alpha);
            else if (hud.Textures.HasImage(g.Patch, ResourceNamespace.Sprites))
                hud.Image(g.Patch, drawPos, anchor: yAnchor, resourceNamespace: ResourceNamespace.Sprites, color: drawColor, alpha: alpha);
            
            drawX += g.Width;
        }

        DrawChildren(hud, number, pos, containerHeight, context, widescreenOffset);
    }
    
    private void DrawStatTotals(IHudRenderContext hud, StatusBarComponentDef comp, Vec2I pos, 
        StatusBarHudFontDef? fontDef, int fontHeight, float alpha)
    {
        var stats = m_world.LevelStats;
        
        DrawStatPart(hud, "K: ", stats.KillCount, stats.TotalMonsters, ref pos, comp, fontDef, fontHeight, alpha);
        DrawStatPart(hud, "I: ", stats.ItemCount, stats.TotalItems, ref pos, comp, fontDef, fontHeight, alpha);
        DrawStatPart(hud, "S: ", stats.SecretCount, stats.TotalSecrets, ref pos, comp, fontDef, fontHeight, alpha);
    }

    private void DrawStatPart(IHudRenderContext hud, string label, int count, int total, ref Vec2I cursor, 
        StatusBarComponentDef comp, StatusBarHudFontDef? fontDef, int fontHeight, float alpha)
    {
        string? defaultColor = comp.Translation;
        string? valueColor = defaultColor;

        if (total != int.MinValue && total > 0 && count >= total) 
            valueColor = "CRGREEN";
        
        int labelWidth = DrawTextPart(hud, label.AsSpan(), cursor, defaultColor, comp.Alignment, fontDef, alpha);
        
        Vec2I valuePos = cursor;
        valuePos.X += labelWidth;
        
        m_fmtSpan.Clear();
        m_fmtSpan.Append(count);
        m_fmtSpan.Append('/');
        m_fmtSpan.Append(total);

        int valueWidth = DrawTextPart(hud, m_fmtSpan.AsSpan(), valuePos, valueColor, comp.Alignment, fontDef, alpha);

        if (comp.Vertical) 
            cursor.Y += fontHeight;
        else 
            cursor.X += labelWidth + valueWidth + 8; // Padding
    }
    
    private void DrawCoordinates(IHudRenderContext hud, StatusBarComponentDef comp, Vec2I pos, 
        StatusBarHudFontDef? fontDef, int fontHeight, float alpha, StatusBarContext context)
    {
        var playerPos = context.Player.Position;
        m_coordPartsCache.Clear();
        
        m_coordPartsCache.Add(new CoordData("X: ", (int)playerPos.X, 0, 0));
        m_coordPartsCache.Add(new CoordData("Y: ", (int)playerPos.Y, 0, 0));
        m_coordPartsCache.Add(new CoordData("Z: ", (int)playerPos.Z, 0, 0));

        int totalHorizontalWidth = 0;
        
        for (int i = 0; i < m_coordPartsCache.Count; i++)
        {
            var data = m_coordPartsCache[i];
            
            int lw = MeasureSpan(hud, data.Label.AsSpan(), "CRGREEN", fontDef, alpha);
            
            m_fmtSpan.Clear();
            m_fmtSpan.Append(data.Value);
            int vw = MeasureSpan(hud, m_fmtSpan.AsSpan(), comp.Translation, fontDef, alpha);
            
            m_coordPartsCache[i] = data with { LabelWidth = lw, ValWidth = vw };
            
            totalHorizontalWidth += lw + vw;
            if (i < m_coordPartsCache.Count - 1) totalHorizontalWidth += 8; 
        }

        Vec2I cursor = pos;
        int startX = pos.X;

        if (!comp.Vertical)
        {
            if ((comp.Alignment & StatusBarAlignment.Right) != 0) startX -= totalHorizontalWidth;
            else if ((comp.Alignment & StatusBarAlignment.HCenter) != 0) startX -= totalHorizontalWidth / 2;
        }

        cursor.X = startX;
        
        foreach (var data in m_coordPartsCache)
        {
            if (comp.Vertical)
            {
                int lineWidth = data.LabelWidth + data.ValWidth;
                int lineX = pos.X;
                
                if ((comp.Alignment & StatusBarAlignment.Right) != 0) lineX -= lineWidth;
                else if ((comp.Alignment & StatusBarAlignment.HCenter) != 0) lineX -= lineWidth / 2;
                
                DrawTextPart(hud, data.Label.AsSpan(), (lineX, cursor.Y), "CRGREEN", StatusBarAlignment.Left, fontDef, alpha);
                
                m_fmtSpan.Clear();
                m_fmtSpan.Append(data.Value);
                DrawTextPart(hud, m_fmtSpan.AsSpan(), (lineX + data.LabelWidth, cursor.Y), comp.Translation, StatusBarAlignment.Left, fontDef, alpha);
                
                cursor.Y += fontHeight;
            }
            else
            {
                DrawTextPart(hud, data.Label.AsSpan(), cursor, "CRGREEN", StatusBarAlignment.Left, fontDef, alpha);
                cursor.X += data.LabelWidth;
                
                m_fmtSpan.Clear();
                m_fmtSpan.Append(data.Value);
                DrawTextPart(hud, m_fmtSpan.AsSpan(), cursor, comp.Translation, StatusBarAlignment.Left, fontDef, alpha);
                cursor.X += data.ValWidth + 8; 
            }
        }
    }

    private int MeasureSpan(IHudRenderContext hud, ReadOnlySpan<char> t, string? trans, StatusBarHudFontDef? fontDef, float alpha)
    {
        if (fontDef != null)
            return DrawHudText(hud, t, fontDef, (0,0), StatusBarAlignment.Left, trans, alpha, draw: false);
        return hud.MeasureText(t, Constants.Fonts.Small, 8).Width;
    }
    
    private int DrawTextPart(IHudRenderContext hud, ReadOnlySpan<char> text, Vec2I position, string? translation, 
        StatusBarAlignment alignment, StatusBarHudFontDef? fontDef, float alpha)
    {
        int width;
        if (fontDef != null)
        {
            width = DrawHudText(hud, text, fontDef, position, alignment, translation, alpha, draw: true);
        }
        else
        {
            Align align = ConvertAlignment(alignment);
            Color? color = null;
            if (!string.IsNullOrEmpty(translation) && StandardTextColors.TryGetValue(translation, out var c))
                color = c;
            
            hud.Text(text, Constants.Fonts.Small, 8, position, both: align, alpha: alpha, color: color);
            width = hud.MeasureText(text, Constants.Fonts.Small, 8).Width;
        }
        return width;
    }

    private void DrawChildren(IHudRenderContext hud, StatusBarBaseDef def, Vec2I pos, int containerHeight, StatusBarContext context, float widescreenOffset)
    {
        if (def.Children != null)
        {
            foreach (var child in def.Children)
            {
                DrawElementWrapper(hud, child, pos, containerHeight, context, widescreenOffset);
            }
        }
    }

    private static Vec2I ResolvePosition(StatusBarBaseDef def, Vec2I parentPos, float widescreenOffset)
    {
        Vec2I pos = parentPos;
        pos.X += def.X;
        pos.Y += def.Y;
        
        if (widescreenOffset > 0)
        {
            bool dynLeft = (def.Alignment & StatusBarAlignment.DynamicLeft) != 0;
            bool dynRight = (def.Alignment & StatusBarAlignment.DynamicRight) != 0;
            int offset = (int)Math.Ceiling(widescreenOffset);

            if (dynLeft && dynRight) { }
            else if (dynLeft) pos.X -= offset;
            else if (dynRight) pos.X += offset;
        }
        return pos;
    }

    private static float GetWidescreenOffset(IHudRenderContext hud)
    {
        float currentAspect = hud.WindowDimension.AspectRatio;
        if (currentAspect > Constants.DoomVirtualAspectRatio)
        {
            float widthInDoomUnits = 320.0f * (currentAspect / Constants.DoomVirtualAspectRatio);
            return (widthInDoomUnits - 320.0f) / 2.0f;
        }
        return 0f;
    }
    
    private static bool ResolveGlyph(IHudRenderContext hud, string patch, out int width, out int height)
    {
        if (hud.Textures.TryGet(patch, out var handle))
        {
            width = handle.Dimension.Width;
            height = handle.Dimension.Height;
            return true;
        }
        else if (hud.Textures.TryGet(patch, out handle, ResourceNamespace.Sprites))
        {
            width = handle.Dimension.Width;
            height = handle.Dimension.Height;
            return true;
        }
        width = 0; 
        height = 0;
        return false;
    }
    
    private string GetHudFontPatch(StatusBarHudFontDef font, char c)
    {
        m_lookupKeySpan.Clear();
        m_lookupKeySpan.Append(font.Stem);
        m_lookupKeySpan.Append(c);
        
        var lookup = HudFontPatchCache.GetAlternateLookup<ReadOnlySpan<char>>();
        if (lookup.TryGetValue(m_lookupKeySpan.AsSpan(), out var cached))
            return cached;
        
        string result = font.Stem + ((int)c).ToString("D3", CultureInfo.InvariantCulture);
        HudFontPatchCache[font.Stem + c] = result;
        return result;
    }
    
    private string GetFontPatch(IHudRenderContext hud, StatusBarNumberFontDef font, char c)
    {
        m_lookupKeySpan.Clear();
        m_lookupKeySpan.Append(font.Stem);
        m_lookupKeySpan.Append(c);

        var lookup = FontPatchCache.GetAlternateLookup<ReadOnlySpan<char>>();
        if (lookup.TryGetValue(m_lookupKeySpan.AsSpan(), out var cached))
            return cached;

        string result;
        if (c == '-') 
        {
            string p = font.Stem + "MINUS";
            if (hud.Textures.HasImage(p)) result = p;
            else result = font.Stem + "-";
        }
        else if (c == '%') 
        {
            string p = font.Stem + "PRCNT";
            if (hud.Textures.HasImage(p)) result = p;
            else
            {
                p = font.Stem + "PRCN";
                if (hud.Textures.HasImage(p)) result = p;
                else
                {
                    p = font.Stem + "PERCENT";
                    if (hud.Textures.HasImage(p)) result = p;
                    else result = font.Stem + "%";
                }
            }
        }
        else if (char.IsDigit(c))
        {
            string p = font.Stem + "NUM" + c;
            if (hud.Textures.HasImage(p)) result = p;
            else result = font.Stem + c;
        }
        else
        {
            result = font.Stem + c;
        }

        FontPatchCache[font.Stem + c] = result;
        return result;
    }

    private int ResolveNumberValue(Player player, StatusBarNumberType type, int param)
    {
        var composer = m_archiveCollection.EntityDefinitionComposer;
        var dehacked = m_archiveCollection.Definitions.DehackedDefinition;

        switch (type)
        {
            case StatusBarNumberType.Health:
                return Math.Max(0, player.Health);
            case StatusBarNumberType.Armor:
                return player.Armor;
            case StatusBarNumberType.Frags:
                return GetFrags(player); 
            
            case StatusBarNumberType.Ammo: 
                if (StatusBarConditionResolver.TryGetId24AmmoType(composer, param, out var ammoDef))
                {
                    return player.Inventory.Amount(ammoDef.Name);
                }
                return 0;

            case StatusBarNumberType.AmmoSelected:
                string? a = player.AnimationWeapon?.Definition.Properties.Weapons.AmmoType;
                return !string.IsNullOrEmpty(a) ? player.Inventory.Amount(a) : 0;

            case StatusBarNumberType.MaxAmmo:
                if (StatusBarConditionResolver.TryGetId24AmmoType(composer, param, out var maxAmmoDef))
                {
                    return GetMaxAmount(player, maxAmmoDef.Name);
                }
                return 0;

            case StatusBarNumberType.AmmoWeapon: 
                if (dehacked != null && dehacked.TryGetId24PickupType(composer, param, out var weaponDef))
                {
                    string weaponAmmo = weaponDef.Properties.Weapons.AmmoType;
                    return !string.IsNullOrEmpty(weaponAmmo) ? player.Inventory.Amount(weaponAmmo) : 0;
                }
                return 0;
            
            case StatusBarNumberType.MaxAmmoWeapon: 
                if (dehacked != null && dehacked.TryGetId24PickupType(composer, param, out var maxWeaponDef))
                {
                    string maxWeaponAmmo = maxWeaponDef.Properties.Weapons.AmmoType;
                    return !string.IsNullOrEmpty(maxWeaponAmmo) ? GetMaxAmount(player, maxWeaponAmmo) : 0;
                }
                return 0;

            default:
                return 0;
        }
    }
    
    private static int GetFrags(Player player)
    {
        _ = player;
        return 0;
    }
    
    private static string GetSpeedometerText(Player player)
    {
        _ = player;
        return string.Empty;
    }

    private int GetMaxAmount(Player player, string name)
    {
        var composer = m_archiveCollection.EntityDefinitionComposer;
        var def = composer.GetByName(name);
        if (def == null) return 0;
        
        string baseName = Inventory.GetBaseInventoryName(def);
        var baseDef = composer.GetByName(baseName);
        if (baseDef != null) def = baseDef;

        int max = def.Properties.Inventory.MaxAmount;
        if (player.Inventory.HasItemOfClass(Inventory.BackPackBaseClassName) 
            && def.IsType(Inventory.AmmoClassName) 
            && def.Properties.Ammo.BackpackMaxAmount > max)
        {
            max = def.Properties.Ammo.BackpackMaxAmount;
        }
        return max;
    }

    private static Align ConvertAlignment(StatusBarAlignment sbarAlign)
    {
        bool hCenter = (sbarAlign & StatusBarAlignment.HCenter) != 0;
        bool right = (sbarAlign & StatusBarAlignment.Right) != 0;
        bool vCenter = (sbarAlign & StatusBarAlignment.VCenter) != 0;
        bool bottom = (sbarAlign & StatusBarAlignment.Bottom) != 0;

        if (bottom)
        {
            if (hCenter) return Align.BottomMiddle;
            if (right) return Align.BottomRight;
            return Align.BottomLeft;
        }
        
        if (vCenter)
        {
            if (hCenter) return Align.Center; 
            if (right) return Align.MiddleRight;
            return Align.MiddleLeft;
        }

        if (hCenter) return Align.TopMiddle;
        if (right) return Align.TopRight;
        return Align.TopLeft;
    }
    
    private static string GetFaceBackgroundPatch(Player player)
    {
        _ = player;
        return "STFB0";
    }
}