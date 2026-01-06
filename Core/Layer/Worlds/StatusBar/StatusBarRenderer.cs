using System;
using System.Collections.Generic;
using System.Globalization;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Graphics.Geometry;
using Helion.Render.Common;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Render.Common.Textures;
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
using Helion.World.Entities.Inventories.Powerups;

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
    private readonly SpanString m_lookupKeySpan = new(128); 

    private bool m_texturesResolved;

    private readonly record struct RenderGlyph(string Patch, int Width, int Offset, IRenderableTextureHandle? Handle = null);
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

                if (child.Component.Children != null)
                    mask |= ScanChildren(child.Component.Children);
            }

            if (child.Canvas?.Children != null) mask |= ScanChildren(child.Canvas.Children);
            if (child.List?.Children != null) mask |= ScanChildren(child.List.Children);
            if (child.Graphic?.Children != null) mask |= ScanChildren(child.Graphic.Children);
            if (child.Face?.Children != null) mask |= ScanChildren(child.Face.Children);
            if (child.FaceBackground?.Children != null) mask |= ScanChildren(child.FaceBackground.Children);
            if (child.Animation?.Children != null) mask |= ScanChildren(child.Animation.Children);
            if (child.Carousel?.Children != null) mask |= ScanChildren(child.Carousel.Children);
            if (child.Number?.Children != null) mask |= ScanChildren(child.Number.Children);
            if (child.Percent?.Children != null) mask |= ScanChildren(child.Percent.Children);
            if (child.String?.Children != null) mask |= ScanChildren(child.String.Children);
        }
        return mask;
    }

    public void Draw(IHudRenderContext hud, StatusBarLayoutDef layout, StatusBarContext context)
    {
        if (!m_texturesResolved)
        {
            EnsureTexturesResolved(hud, layout);
            m_texturesResolved = true;
        }

        const int width = 320;
        const int height = 200;

        hud.PushVirtualDimension((width, height), ResolutionScale.Center, Constants.DoomVirtualAspectRatio);

        int yOffset = layout.FullscreenRender ? 0 : (200 - layout.Height);
        Vec2I rootPos = (0, yOffset);

        float windowAspect = (float)hud.WindowDimension.Width / hud.WindowDimension.Height;
        const float virtualAspect = 320f / 200f;

        float hOffset = 0;
        float vOffset = 0;

        if (windowAspect > virtualAspect)
            hOffset = (200f * windowAspect - 320f) / 2f;
        else if (windowAspect < virtualAspect)
            vOffset = (320f / windowAspect - 200f) / 2f;

        if (!layout.FullscreenRender)
        {
            string? fillFlat = layout.FillFlat;
            if (string.IsNullOrEmpty(fillFlat))
                fillFlat = m_world.GameInfo.BorderFlat;

            if (!string.IsNullOrEmpty(fillFlat) && hud.Textures.TryGet(fillFlat, out var bgHandle))
            {
                int bgWidth = bgHandle.Dimension.Width;
                int bgHeight = bgHandle.Dimension.Height;
                if (bgHeight <= 0) bgHeight = 64;

                int startX = (int)-Math.Ceiling(hOffset);
                int endX = 320 + (int)Math.Ceiling(hOffset);
                int startY = yOffset;
                int endY = 200 + (int)Math.Ceiling(vOffset);

                for (int x = (startX / bgWidth - 1) * bgWidth; x < endX; x += bgWidth)
                {
                    for (int y = startY; y < endY; y += bgHeight)
                    {
                        hud.Image(fillFlat, (x, y), anchor: Align.TopLeft);
                    }
                }
            }
        }

        foreach (var child in layout.Children)
        {
            DrawElementWrapper(hud, child, rootPos, layout.Height, context, hOffset, rootPos);
        }

        hud.PopVirtualDimension();
    }

    private void EnsureTexturesResolved(IHudRenderContext hud, StatusBarLayoutDef layout)
    {
        foreach (var t in layout.Children)
        {
            ResolveElementTextures(hud, t);
        }
    }

    private void ResolveElementTextures(IHudRenderContext hud, StatusBarElementWrapper wrapper)
    {
        if (wrapper.Graphic != null)
        {
            wrapper.Graphic.ResolvedPatchName = ResolvePatchName(wrapper.Graphic.Patch);
            
            if (hud.Textures.TryGet(wrapper.Graphic.ResolvedPatchName, out var handle) || 
                hud.Textures.TryGet(wrapper.Graphic.ResolvedPatchName, out handle, ResourceNamespace.Sprites))
            {
                wrapper.Graphic.Handle = handle;
                wrapper.Graphic.ResolvedHeight = handle.Dimension.Height;
            }
        }

        if (wrapper.Animation != null)
        {
            for (int i = 0; i < wrapper.Animation.Frames.Count; i++)
            {
                var frame = wrapper.Animation.Frames[i];
                frame.ResolvedPatchName = ResolvePatchName(frame.Lump);
                
                if (hud.Textures.TryGet(frame.ResolvedPatchName, out var handle) || 
                    hud.Textures.TryGet(frame.ResolvedPatchName, out handle, ResourceNamespace.Sprites))
                {
                    frame.Handle = handle;
                }
                
                wrapper.Animation.Frames[i] = frame; 
            }
        }

        if (wrapper.String != null)
        {
            m_hudFontLookup.TryGetValue(wrapper.String.Font, out var f);
            if (f != null)
            {
                string zeroPatch = GetHudFontPatch(f, '0');
                wrapper.String.ResolvedHeight = hud.Textures.TryGet(zeroPatch, out var h) ? h.Dimension.Height : hud.GetFontMaxHeight(f.Stem);
            }
            else wrapper.String.ResolvedHeight = 8;
        }
        else if (wrapper.Number != null || wrapper.Percent != null)
        {
            var num = (StatusBarBaseDef?)wrapper.Number ?? wrapper.Percent;
            m_fontNumberLookup.TryGetValue(wrapper.Number?.Font ?? wrapper.Percent?.Font ?? string.Empty, out var nf);
            if (nf != null)
            {
                string zeroPatch = GetFontPatch(hud, nf, '0');
                num!.ResolvedHeight = hud.Textures.TryGet(zeroPatch, out var h) ? h.Dimension.Height : 8;
            }
            else num!.ResolvedHeight = 8;
        }
        else if (wrapper.Face != null || wrapper.FaceBackground != null)
        {
            var face = (StatusBarBaseDef?)wrapper.Face ?? wrapper.FaceBackground;
            
            if (hud.Textures.TryGet("STFST00", out var h) || 
                hud.Textures.TryGet("STFST00", out h, ResourceNamespace.Sprites))
            {
                face!.ResolvedHeight = h.Dimension.Height;
            }
            else
            {
                face!.ResolvedHeight = 32;
            }
        }

        if (wrapper.FaceBackground != null)
        {
            if (hud.Textures.TryGet("STFB0", out var handle) || 
                hud.Textures.TryGet("STFB0", out handle, ResourceNamespace.Sprites))
            {
                wrapper.FaceBackground.Handle = handle;
            }
        }

        StatusBarBaseDef? baseDef = null;
        if (wrapper.Canvas != null) baseDef = wrapper.Canvas;
        else if (wrapper.List != null) baseDef = wrapper.List;
        else if (wrapper.Graphic != null) baseDef = wrapper.Graphic;
        else if (wrapper.Face != null) baseDef = wrapper.Face;
        else if (wrapper.Animation != null) baseDef = wrapper.Animation;
        else if (wrapper.Carousel != null) baseDef = wrapper.Carousel;
        else if (wrapper.Number != null) baseDef = wrapper.Number;
        else if (wrapper.Percent != null) baseDef = wrapper.Percent;
        else if (wrapper.String != null) baseDef = wrapper.String;
        else if (wrapper.Component != null) baseDef = wrapper.Component;
        else if (wrapper.FaceBackground != null) baseDef = wrapper.FaceBackground;

        if (baseDef?.Children == null) return;
        foreach (var t in baseDef.Children)
            ResolveElementTextures(hud, t);
    }

    private void DrawElementWrapper(IHudRenderContext hud, StatusBarElementWrapper wrapper, Vec2I parentPos,
        int containerHeight, StatusBarContext context, float widescreenOffset, Vec2I rootPos)
    {
        Vec2I effectiveParentPos = parentPos;
        bool isHudWidget = wrapper.Component != null || wrapper.Carousel != null;
        if (isHudWidget && parentPos == rootPos)
        {
            effectiveParentPos = (0, 0);
        }

        if (wrapper.Canvas != null)
            DrawBase(hud, wrapper.Canvas, parentPos, containerHeight, context, widescreenOffset, rootPos);
        else if (wrapper.List != null)
            DrawList(hud, wrapper.List, parentPos, containerHeight, context, widescreenOffset, rootPos);
        else if (wrapper.Graphic != null)
            DrawGraphic(hud, wrapper.Graphic, parentPos, containerHeight, context, widescreenOffset);
        else if (wrapper.Number != null)
            DrawNumber(hud, wrapper.Number, parentPos, containerHeight, context, false, widescreenOffset);
        else if (wrapper.Percent != null)
            DrawNumber(hud, wrapper.Percent, parentPos, containerHeight, context, true, widescreenOffset);
        else if (wrapper.String != null)
            DrawString(hud, wrapper.String, parentPos, containerHeight, context, widescreenOffset);
        else if (wrapper.Face != null)
            DrawFace(hud, wrapper.Face, parentPos, containerHeight, context, widescreenOffset);
        else if (wrapper.FaceBackground != null)
            DrawFaceBackground(hud, wrapper.FaceBackground, parentPos, containerHeight, context, widescreenOffset);
        else if (wrapper.Animation != null)
            DrawAnimation(hud, wrapper.Animation, parentPos, containerHeight, context, widescreenOffset);
        else if (wrapper.Component != null)
            DrawComponent(hud, wrapper.Component, effectiveParentPos, containerHeight, context, widescreenOffset, rootPos);
        else if (wrapper.Carousel != null)
            DrawCarousel(hud, wrapper.Carousel, effectiveParentPos, containerHeight, context, widescreenOffset, rootPos);
    }

    private void DrawBase(IHudRenderContext hud, StatusBarCanvasDef def, Vec2I parentPos, int containerHeight,
        StatusBarContext context, float widescreenOffset, Vec2I rootPos)
    {
        if (!StatusBarConditionResolver.Evaluate(context, def.Conditions))
            return;

        Vec2I currentPos = ResolvePosition(def, parentPos, widescreenOffset);
        DrawChildren(hud, def, currentPos, containerHeight, context, widescreenOffset, rootPos);
    }

    private void DrawList(IHudRenderContext hud, StatusBarListDef def, Vec2I parentPos, int containerHeight,
        StatusBarContext context, float widescreenOffset, Vec2I rootPos)
    {
        if (!StatusBarConditionResolver.Evaluate(context, def.Conditions) || def.Children == null)
            return;

        int childCount = def.Children.Count;
        Span<Vec2I> sizes = stackalloc Vec2I[childCount];
        int activeCount = 0;
        int totalWidth = 0;
        int totalHeight = 0;

        foreach (var child in def.Children)
        {
            if (!EvaluateWrapperConditions(child, context))
                continue;

            Vec2I size = MeasureElement(hud, child, context);
            sizes[activeCount] = size;

            if (def.Horizontal)
            {
                totalWidth += size.X;
                if (activeCount > 0) totalWidth += def.Spacing;
                totalHeight = Math.Max(totalHeight, size.Y);
            }
            else
            {
                totalHeight += size.Y;
                if (activeCount > 0) totalHeight += def.Spacing;
                totalWidth = Math.Max(totalWidth, size.X);
            }
            activeCount++;
        }

        if (activeCount == 0) return;

        Vec2I listPos = ResolvePosition(def, parentPos, widescreenOffset);

        if ((def.Alignment & StatusBarAlignment.HCenter) != 0) listPos.X -= totalWidth / 2;
        else if ((def.Alignment & StatusBarAlignment.Right) != 0) listPos.X -= totalWidth;

        if ((def.Alignment & StatusBarAlignment.Bottom) != 0) listPos.Y -= totalHeight;
        else if ((def.Alignment & StatusBarAlignment.VCenter) != 0) listPos.Y -= totalHeight / 2;

        int currentIdx = 0;
        Vec2I cursor = listPos;
        foreach (var child in def.Children)
        {
            if (!EvaluateWrapperConditions(child, context))
                continue;

            Vec2I size = sizes[currentIdx];
            Vec2I childPos = cursor;

            if (def.Horizontal)
            {
                if ((def.Alignment & StatusBarAlignment.Bottom) != 0)
                    childPos.Y += (totalHeight - size.Y);
                else if ((def.Alignment & StatusBarAlignment.VCenter) != 0)
                    childPos.Y += (totalHeight - size.Y) / 2;

                DrawElementWrapper(hud, child, childPos, containerHeight, context, widescreenOffset, rootPos);
                cursor.X += size.X + def.Spacing;
            }
            else
            {
                if ((def.Alignment & StatusBarAlignment.Right) != 0)
                    childPos.X += (totalWidth - size.X);
                else if ((def.Alignment & StatusBarAlignment.HCenter) != 0)
                    childPos.X += (totalWidth - size.X) / 2;

                DrawElementWrapper(hud, child, childPos, containerHeight, context, widescreenOffset, rootPos);
                cursor.Y += size.Y + def.Spacing;
            }
            currentIdx++;
        }
    }

    private void DrawGraphic(IHudRenderContext hud, StatusBarGraphicDef graphic, Vec2I parentPos,
        int containerHeight, StatusBarContext context, float widescreenOffset)
    {
        if (!StatusBarConditionResolver.Evaluate(context, graphic.Conditions))
            return;

        Vec2I currentPos = ResolvePosition(graphic, parentPos, widescreenOffset);

        if (graphic.Handle != null || !string.IsNullOrEmpty(graphic.Patch))
        {
            Align align = ConvertAlignment(graphic.Alignment);
            if (graphic.MidOffset != 0) currentPos.X += graphic.MidOffset;

            float alpha = graphic.Translucency ? 0.5f : 1.0f;
        
            DrawSBarTexture(hud, graphic.ResolvedPatchName ?? graphic.Patch, graphic.Handle, currentPos, align,
                graphic.Alignment, graphic.Translation, alpha, graphic.Crop);
        }

        DrawChildren(hud, graphic, currentPos, containerHeight, context, widescreenOffset, (0, 0));
    }

    private void DrawFace(IHudRenderContext hud, StatusBarFaceDef face, Vec2I parentPos,
        int containerHeight, StatusBarContext context, float widescreenOffset)
    {
        if (!StatusBarConditionResolver.Evaluate(context, face.Conditions))
            return;

        Vec2I currentPos = ResolvePosition(face, parentPos, widescreenOffset);
        string patch = context.Player.StatusBar.GetFacePatch();

        if (!string.IsNullOrEmpty(patch))
        {
            Align align = ConvertAlignment(face.Alignment);
            float alpha = face.Translucency ? 0.5f : 1.0f;
            
            DrawSBarTexture(hud, patch, null, currentPos, align,
                face.Alignment, face.Translation, alpha, face.Crop);
        }

        DrawChildren(hud, face, currentPos, containerHeight, context, widescreenOffset, (0, 0));
    }

    private void DrawFaceBackground(IHudRenderContext hud, StatusBarFaceDef faceBg, Vec2I parentPos,
        int containerHeight, StatusBarContext context, float widescreenOffset)
    {
        if (!StatusBarConditionResolver.Evaluate(context, faceBg.Conditions))
            return;

        Vec2I currentPos = ResolvePosition(faceBg, parentPos, widescreenOffset);

        if (faceBg.Handle != null)
        {
            Align align = ConvertAlignment(faceBg.Alignment);
            float alpha = faceBg.Translucency ? 0.5f : 1.0f;
        
            DrawSBarTexture(hud, "STFB0", faceBg.Handle, currentPos, align,
                faceBg.Alignment, faceBg.Translation, alpha);
        }

        DrawChildren(hud, faceBg, currentPos, containerHeight, context, widescreenOffset, (0, 0));
    }

    private void DrawAnimation(IHudRenderContext hud, StatusBarAnimationDef anim, Vec2I parentPos,
        int containerHeight, StatusBarContext context, float widescreenOffset)
    {
        if (!StatusBarConditionResolver.Evaluate(context, anim.Conditions))
            return;

        Vec2I currentPos = ResolvePosition(anim, parentPos, widescreenOffset);

        if (anim.Frames.Count > 0)
        {
            double totalDuration = 0;
            foreach (var frame1 in anim.Frames)
                totalDuration += frame1.Duration;

            if (totalDuration > 0)
            {
                double timePerLoop = totalDuration * Constants.TicksPerSecond;
                long currentTick = m_world.LevelTime;
                double animTime = currentTick % timePerLoop;

                var frame = anim.Frames[0];
                double timeAccumulator = 0;

                foreach (var f in anim.Frames)
                {
                    timeAccumulator += f.Duration * Constants.TicksPerSecond;
                    if (animTime < timeAccumulator)
                    {
                        frame = f;
                        break;
                    }
                }

                Align align = ConvertAlignment(anim.Alignment);
                DrawSBarTexture(hud, frame.ResolvedPatchName ?? frame.Lump, frame.Handle, currentPos, align, anim.Alignment, anim.Translation);
            }
        }

        DrawChildren(hud, anim, currentPos, containerHeight, context, widescreenOffset, (0, 0));
    }

    private void DrawString(IHudRenderContext hud, StatusBarStringDef def, Vec2I parentPos,
        int containerHeight, StatusBarContext context, float widescreenOffset)
    {
        if (!StatusBarConditionResolver.Evaluate(context, def.Conditions))
            return;

        Vec2I pos = ResolvePosition(def, parentPos, widescreenOffset);
        ReadOnlySpan<char> text = GetStringValue(def);
        if (text.IsEmpty) return;

        m_hudFontLookup.TryGetValue(def.Font, out var fontDef);
        float alpha = def.Translucency ? 0.5f : 1.0f;
        int fontHeight = fontDef != null ? hud.GetFontMaxHeight(fontDef.Stem) : 8;
        if (fontHeight <= 0) fontHeight = 8;

        RenderLines(hud, text, pos, fontDef, fontHeight, def.Alignment, def.Translation, alpha);
        DrawChildren(hud, def, pos, containerHeight, context, widescreenOffset, (0, 0));
    }

    private void DrawComponent(IHudRenderContext hud, StatusBarComponentDef comp, Vec2I parentPos,
        int containerHeight, StatusBarContext context, float widescreenOffset, Vec2I rootPos)
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
                const double fadeInTime = 0.25;
                const double fadeOutTime = 1.0;
                double timeSinceStart = m_world.LevelTime / Constants.TicksPerSecond;

                if (timeSinceStart > duration + fadeOutTime)
                    return;

                if (timeSinceStart < fadeInTime) alpha *= (float)(timeSinceStart / fadeInTime);
                else if (timeSinceStart > duration)
                {
                    double progress = (timeSinceStart - duration) / fadeOutTime;
                    alpha *= (float)(1.0 - progress);
                }

                string annTitle = m_world.MapInfo.GetDisplayNameWithPrefix(m_archiveCollection.Language);
                RenderLines(hud, annTitle.AsSpan(), pos, fontDef, fontHeight, alignment, comp.Translation, alpha);
                break;
        }

        DrawChildren(hud, comp, pos, containerHeight, context, widescreenOffset, rootPos);
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

        if (lineStart < text.Length)
        {
            var line = text.Slice(lineStart);
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

        if (drawnWidth == 0)
        {
            Align align = ConvertAlignment(alignment);
            hud.Text(line, Constants.Fonts.Small, 8, drawPos, both: align, alpha: alpha);
        }
    }

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
        if (fontDef.Type == 1)
        {
            if (!HudType1WidthCache.TryGetValue(fontDef.Stem, out monoWidth))
            {
                monoWidth = 0;
                for (char c1 = '!'; c1 <= '_'; c1++)
                {
                    string patch1 = GetHudFontPatch(fontDef, c1);
                    if (ResolveGlyph(hud, patch1, out int w, out _))
                        monoWidth = Math.Max(monoWidth, w);
                }
                HudType1WidthCache[fontDef.Stem] = monoWidth;
            }
        }
        else if (fontDef.Type == 0)
        {
            string zero = GetHudFontPatch(fontDef, '0');
            if (hud.Textures.TryGet(zero, out var zh)) monoWidth = zh.Dimension.Width;
        }

        foreach (var originalChar in text)
        {
            int width;
            string patch = string.Empty;
            char c = originalChar;

            if (c == ' ')
            {
                string bang = GetHudFontPatch(fontDef, '!');
                width = hud.Textures.TryGet(bang, out var h) ? h.Dimension.Width : 4;
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

                if (found) maxHeight = Math.Max(maxHeight, height);
                else patch = string.Empty;
            }

            if (monoWidth > 0) width = monoWidth;

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

        foreach (var g in m_glyphCache)
        {
            if (!string.IsNullOrEmpty(g.Patch))
            {
                DrawSBarTexture(hud, g.Patch, null, (drawX, drawY), Align.TopLeft, alignment, translation, alpha);
            }
            drawX += g.Width;
        }

        return totalWidth;
    }

    private void DrawCarousel(IHudRenderContext hud, StatusBarCarouselDef carousel, Vec2I parentPos,
        int containerHeight, StatusBarContext context, float widescreenOffset, Vec2I rootPos)
    {
        if (!StatusBarConditionResolver.Evaluate(context, carousel.Conditions))
            return;

        Vec2I pos = ResolvePosition(carousel, parentPos, widescreenOffset);
        pos.X = 160;

        if (context.Player.Weapon != null)
        {
            string icon = context.Player.Weapon.Definition.Properties.Inventory.Icon;
            if (!string.IsNullOrEmpty(icon))
            {
                Align align = ConvertAlignment(carousel.Alignment);
                float alpha = carousel.Translucency ? 0.5f : 1.0f;
                DrawSBarTexture(hud, icon, null, pos, align, carousel.Alignment, carousel.Translation, alpha);
            }
        }

        DrawChildren(hud, carousel, pos, containerHeight, context, widescreenOffset, rootPos);
    }

        
    private static void DrawSBarTexture(IHudRenderContext hud, string patch, IRenderableTextureHandle? handle, Vec2I pos, Align align,
        StatusBarAlignment sbarAlign, string? translation = null, float alpha = 1.0f, StatusBarCropDef? cropDef = null)
    {
        string pName = handle == null ? ResolvePatchName(patch) : patch;
        ResourceNamespace ns = ResourceNamespace.Global;
        
        if (handle == null)
        {
            if (!hud.Textures.TryGet(pName, out handle))
            {
                ns = ResourceNamespace.Sprites;
                if (!hud.Textures.TryGet(pName, out handle, ns))
                    return;
            }
        }

        ImageBox2I? cropArea = GetCropArea(handle, cropDef);

        bool ignoreX = (sbarAlign & StatusBarAlignment.IgnoreLeftOffset) != 0;
        bool ignoreY = (sbarAlign & StatusBarAlignment.IgnoreTopOffset) != 0;

        Vec2I drawPos = pos;
        if (!ignoreX) drawPos.X += RenderDimensions.TranslateDoomOffset(handle.Offset).X;
        if (!ignoreY) drawPos.Y += RenderDimensions.TranslateDoomOffset(handle.Offset).Y;

        Color? drawColor = null;
        if (!string.IsNullOrEmpty(translation))
        {
            if (StandardTextColors.TryGetValue(translation, out var c))
                drawColor = c;
        }

        hud.Image(pName, drawPos, anchor: align, resourceNamespace: ns, alpha: alpha, color: drawColor, crop: cropArea);
    }

    private void DrawNumber(IHudRenderContext hud, StatusBarNumberDef number, Vec2I parentPos,
        int containerHeight, StatusBarContext context, bool isPercent, float widescreenOffset)
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

        foreach (var c in text)
        {
            string patch = GetFontPatch(hud, fontDef, c);
            int width;
            int xOffset = 0;

            if (hud.Textures.TryGet(patch, out var handle)) width = handle.Dimension.Width;
            else if (hud.Textures.TryGet(patch, out handle, ResourceNamespace.Sprites)) width = handle.Dimension.Width;
            else continue;

            if (fontDef.Type == 0 || fontDef.Type == 1)
            {
                if (monoWidth > 0)
                {
                    if (width < monoWidth) xOffset = (monoWidth - width) / 2;
                    width = monoWidth;
                }
            }

            m_glyphCache.Add(new RenderGlyph(patch, width, xOffset, handle));
            totalWidth += width;
        }

        int drawX = pos.X;
        int drawY = pos.Y;

        if ((number.Alignment & StatusBarAlignment.HCenter) != 0) drawX -= totalWidth / 2;
        else if ((number.Alignment & StatusBarAlignment.Right) != 0) drawX -= totalWidth;

        Align yAnchor = (number.Alignment & StatusBarAlignment.Bottom) != 0 ? Align.BottomLeft : Align.TopLeft;

        foreach (var g in m_glyphCache)
        {
            Vec2I drawPos = (drawX + g.Offset, drawY);
            DrawSBarTexture(hud, g.Patch, g.Handle, drawPos, yAnchor, number.Alignment, number.Translation, alpha);
            drawX += g.Width;
        }

        DrawChildren(hud, number, pos, containerHeight, context, widescreenOffset, (0, 0));
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

        if (total != int.MinValue && total > 0 && count >= total) valueColor = "CRGREEN";

        int labelWidth = DrawTextPart(hud, label.AsSpan(), cursor, defaultColor, comp.Alignment, fontDef, alpha);
        Vec2I valuePos = cursor;
        valuePos.X += labelWidth;

        m_fmtSpan.Clear();
        m_fmtSpan.Append(count);
        m_fmtSpan.Append('/');
        m_fmtSpan.Append(total);

        int valueWidth = DrawTextPart(hud, m_fmtSpan.AsSpan(), valuePos, valueColor, comp.Alignment, fontDef, alpha);

        if (comp.Vertical) cursor.Y += fontHeight;
        else cursor.X += labelWidth + valueWidth + 8;
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
        if (!comp.Vertical)
        {
            if ((comp.Alignment & StatusBarAlignment.Right) != 0) cursor.X -= totalHorizontalWidth;
            else if ((comp.Alignment & StatusBarAlignment.HCenter) != 0) cursor.X -= totalHorizontalWidth / 2;
        }

        foreach (var data in m_coordPartsCache)
        {
            if (comp.Vertical)
            {
                int lineWidth = data.LabelWidth + data.ValWidth;
                int lineX = pos.X;
                if ((comp.Alignment & StatusBarAlignment.Right) != 0) lineX -= lineWidth;
                else if ((comp.Alignment & StatusBarAlignment.HCenter) != 0) lineX -= lineWidth / 2;

                DrawTextPart(hud, data.Label.AsSpan(), (lineX, cursor.Y), "CRGREEN",
                    StatusBarAlignment.Left, fontDef, alpha);
                m_fmtSpan.Clear();
                m_fmtSpan.Append(data.Value);
                DrawTextPart(hud, m_fmtSpan.AsSpan(), (lineX + data.LabelWidth, cursor.Y),
                    comp.Translation, StatusBarAlignment.Left, fontDef, alpha);
                cursor.Y += fontHeight;
            }
            else
            {
                DrawTextPart(hud, data.Label.AsSpan(), cursor, "CRGREEN",
                    StatusBarAlignment.Left, fontDef, alpha);
                cursor.X += data.LabelWidth;
                m_fmtSpan.Clear();
                m_fmtSpan.Append(data.Value);
                DrawTextPart(hud, m_fmtSpan.AsSpan(), cursor,
                    comp.Translation, StatusBarAlignment.Left, fontDef, alpha);
                cursor.X += data.ValWidth + 8;
            }
        }
    }

    private int MeasureSpan(IHudRenderContext hud, ReadOnlySpan<char> t, string? trans, StatusBarHudFontDef? fontDef, float alpha)
    {
        if (fontDef != null)
            return DrawHudText(hud, t, fontDef, (0, 0), StatusBarAlignment.Left, trans, alpha, false);
        return hud.MeasureText(t, Constants.Fonts.Small, 8).Width;
    }

    private int DrawTextPart(IHudRenderContext hud, ReadOnlySpan<char> text, Vec2I position, string? translation,
        StatusBarAlignment alignment, StatusBarHudFontDef? fontDef, float alpha)
    {
        if (fontDef != null)
            return DrawHudText(hud, text, fontDef, position, alignment, translation, alpha);

        Align align = ConvertAlignment(alignment);
        Color? color = !string.IsNullOrEmpty(translation) && StandardTextColors.TryGetValue(translation, out var c) ? c : null;
        hud.Text(text, Constants.Fonts.Small, 8, position, both: align, alpha: alpha, color: color);
        return hud.MeasureText(text, Constants.Fonts.Small, 8).Width;
    }

    private void DrawChildren(IHudRenderContext hud, StatusBarBaseDef def, Vec2I pos, int containerHeight,
        StatusBarContext context, float widescreenOffset, Vec2I rootPos)
    {
        if (def.Children == null) return;

        foreach (var child in def.Children)
        {
            if (!EvaluateWrapperConditions(child, context))
                continue;

            DrawElementWrapper(hud, child, pos, containerHeight, context, widescreenOffset, rootPos);
        }
    }

    private Vec2I MeasureElement(IHudRenderContext hud, StatusBarElementWrapper wrapper, StatusBarContext context)
    {
        if (wrapper.Graphic != null) return MeasureGraphic(wrapper.Graphic);
        if (wrapper.Number != null) return MeasureNumber(hud, wrapper.Number, context, false);
        if (wrapper.Percent != null) return MeasureNumber(hud, wrapper.Percent, context, true);
        if (wrapper.Face != null) return MeasureFace(hud, wrapper.Face, context);
        if (wrapper.String != null) return MeasureString(hud, wrapper.String);
        if (wrapper.Canvas != null) return MeasureCanvas(hud, wrapper.Canvas, context);
        if (wrapper.List != null) return MeasureList(hud, wrapper.List, context);

        return Vec2I.Zero;
    }

    private static Vec2I MeasureGraphic(StatusBarGraphicDef def)
    {
        var handle = def.Handle;
        if (handle == null) return Vec2I.Zero;

        Vec2I size = handle.Dimension.Vector;
        int posX = def.X;
        int posY = def.Y;

        if ((def.Alignment & StatusBarAlignment.IgnoreLeftOffset) == 0)
            posX += RenderDimensions.TranslateDoomOffset(handle.Offset).X;
        if ((def.Alignment & StatusBarAlignment.IgnoreTopOffset) == 0)
            posY += RenderDimensions.TranslateDoomOffset(handle.Offset).Y;

        return ApplyAlignment(size, posX, posY, def.Alignment);
    }

    private Vec2I MeasureNumber(IHudRenderContext hud, StatusBarNumberDef def, StatusBarContext context, bool isPercent)
    {
        m_fmtSpan.Clear();
        m_fmtSpan.Append(ResolveNumberValue(context.Player, def.Type, def.Param));
        if (isPercent) m_fmtSpan.Append('%');
        
        int width = MeasureSpan(hud, m_fmtSpan.AsSpan(), null, null, 1.0f);
        int height = def.ResolvedHeight > 0 ? def.ResolvedHeight : 8;

        return ApplyAlignment(new Vec2I(width, height), def.X, def.Y, def.Alignment);
    }

    private static Vec2I MeasureFace(IHudRenderContext hud, StatusBarFaceDef def, StatusBarContext context)
    {
        string p = context.Player.StatusBar.GetFacePatch();
        if (!hud.Textures.TryGet(p, out var h)) return Vec2I.Zero;

        Vec2I size = h.Dimension.Vector;
        int posX = def.X;
        int posY = def.Y;

        if ((def.Alignment & StatusBarAlignment.IgnoreLeftOffset) == 0)
            posX += RenderDimensions.TranslateDoomOffset(h.Offset).X;
        if ((def.Alignment & StatusBarAlignment.IgnoreTopOffset) == 0)
            posY += RenderDimensions.TranslateDoomOffset(h.Offset).Y;

        return ApplyAlignment(size, posX, posY, def.Alignment);
    }

    private Vec2I MeasureString(IHudRenderContext hud, StatusBarStringDef def)
    {
        var text = GetStringValue(def);
        m_hudFontLookup.TryGetValue(def.Font, out var f);
        
        int width = MeasureSpan(hud, text, null, f, 1.0f);
        int height = def.ResolvedHeight > 0 ? def.ResolvedHeight : 8;

        return ApplyAlignment(new Vec2I(width, height), def.X, def.Y, def.Alignment);
    }

    private Vec2I MeasureCanvas(IHudRenderContext hud, StatusBarCanvasDef def, StatusBarContext context)
    {
        if (def.Children == null) return Vec2I.Zero;
        int maxX = 0, maxY = 0;
        foreach (var t in def.Children)
        {
            var cSize = MeasureElement(hud, t, context);
            if (cSize.X > maxX) maxX = cSize.X;
            if (cSize.Y > maxY) maxY = cSize.Y;
        }
        return ApplyAlignment(new Vec2I(maxX, maxY), def.X, def.Y, def.Alignment);
    }

    private Vec2I MeasureList(IHudRenderContext hud, StatusBarListDef def, StatusBarContext context)
    {
        if (def.Children == null) return Vec2I.Zero;

        int totalW = 0;
        int totalH = 0;
        int count = 0;

        foreach (var child in def.Children)
        {
            if (!EvaluateWrapperConditions(child, context))
                continue;

            var size = MeasureElement(hud, child, context);
            if (def.Horizontal)
            {
                totalW += size.X;
                if (count > 0) totalW += def.Spacing;
                if (size.Y > totalH) totalH = size.Y;
            }
            else
            {
                totalH += size.Y;
                if (count > 0) totalH += def.Spacing;
                if (size.X > totalW) totalW = size.X;
            }
            count++;
        }

        return ApplyAlignment(new Vec2I(totalW, totalH), def.X, def.Y, def.Alignment);
    }

    private static Vec2I ApplyAlignment(Vec2I size, int posX, int posY, StatusBarAlignment alignment)
    {
        _ = alignment;
        return new Vec2I(size.X + Math.Max(0, posX), size.Y + Math.Max(0, posY));
    }

    private static string ResolvePatchName(string patch)
    {
        if (string.IsNullOrEmpty(patch)) return patch;
        if (patch.Contains('/') || patch.Contains('.'))
        {
            int lastSlash = patch.LastIndexOf('/') + 1;
            int lastDot = patch.LastIndexOf('.');
            if (lastDot < lastSlash) lastDot = patch.Length;
            return patch.Substring(lastSlash, lastDot - lastSlash);
        }
        return patch;
    }

    private ReadOnlySpan<char> GetStringValue(StatusBarStringDef def)
    {
        switch (def.Type)
        {
            case 0: return def.Data.AsSpan();
            case 1: return m_world.MapInfo.GetDisplayNameWithPrefix(m_archiveCollection.Language).AsSpan();
            case 2: return m_world.MapInfo.Label.AsSpan();
            case 3: return m_world.MapInfo.Author.AsSpan();
            default: return ReadOnlySpan<char>.Empty;
        }
    }

    private static bool EvaluateWrapperConditions(StatusBarElementWrapper wrapper, StatusBarContext context)
    {
        if (wrapper.Canvas != null) return StatusBarConditionResolver.Evaluate(context, wrapper.Canvas.Conditions);
        if (wrapper.Graphic != null) return StatusBarConditionResolver.Evaluate(context, wrapper.Graphic.Conditions);
        if (wrapper.Number != null) return StatusBarConditionResolver.Evaluate(context, wrapper.Number.Conditions);
        if (wrapper.Percent != null) return StatusBarConditionResolver.Evaluate(context, wrapper.Percent.Conditions);
        if (wrapper.Face != null) return StatusBarConditionResolver.Evaluate(context, wrapper.Face.Conditions);
        if (wrapper.Animation != null) return StatusBarConditionResolver.Evaluate(context, wrapper.Animation.Conditions);
        if (wrapper.Component != null) return StatusBarConditionResolver.Evaluate(context, wrapper.Component.Conditions);
        if (wrapper.Carousel != null) return StatusBarConditionResolver.Evaluate(context, wrapper.Carousel.Conditions);
        if (wrapper.List != null) return StatusBarConditionResolver.Evaluate(context, wrapper.List.Conditions);
        if (wrapper.String != null) return StatusBarConditionResolver.Evaluate(context, wrapper.String.Conditions);
        return true;
    }

    private static Vec2I ResolvePosition(StatusBarBaseDef def, Vec2I parentPos, float widescreenOffset)
    {
        Vec2I pos = parentPos;
        pos.X += def.X;
        pos.Y += def.Y;

        if (widescreenOffset > 0)
        {
            int offset = (int)Math.Ceiling(widescreenOffset);
            if ((def.Alignment & StatusBarAlignment.WidescreenLeft) != 0) pos.X -= offset;
            else if ((def.Alignment & StatusBarAlignment.WidescreenRight) != 0) pos.X += offset;
        }
        return pos;
    }

    private static bool ResolveGlyph(IHudRenderContext hud, string patch, out int width, out int height)
    {
        string p = ResolvePatchName(patch);
        if (hud.Textures.TryGet(p, out var handle) || hud.Textures.TryGet(p, out handle, ResourceNamespace.Sprites))
        {
            width = handle.Dimension.Width;
            height = handle.Dimension.Height;
            return true;
        }
        width = height = 0;
        return false;
    }

    private string GetHudFontPatch(StatusBarHudFontDef font, char c)
    {
        m_lookupKeySpan.Clear();
        m_lookupKeySpan.Append(font.Stem);
        m_lookupKeySpan.Append(c);

        var lookup = HudFontPatchCache.GetAlternateLookup<ReadOnlySpan<char>>();
        if (lookup.TryGetValue(m_lookupKeySpan.AsSpan(), out var cached)) return cached;

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
        if (lookup.TryGetValue(m_lookupKeySpan.AsSpan(), out var cached)) return cached;

        string result;
        if (c == '-')
        {
            string p = font.Stem + "MINUS";
            result = hud.Textures.HasImage(p) ? p : font.Stem + "-";
        }
        else if (c == '%')
        {
            string p = font.Stem + "PRCNT";
            if (!hud.Textures.HasImage(p)) p = font.Stem + "PRCN";
            if (!hud.Textures.HasImage(p)) p = font.Stem + "PERCENT";
            result = hud.Textures.HasImage(p) ? p : font.Stem + "%";
        }
        else if (char.IsDigit(c))
        {
            string p = font.Stem + "NUM" + c;
            result = hud.Textures.HasImage(p) ? p : font.Stem + c;
        }
        else result = font.Stem + c;

        FontPatchCache[font.Stem + c] = result;
        return result;
    }

    private int ResolveNumberValue(Player player, StatusBarNumberType type, int param)
    {
        var composer = m_archiveCollection.EntityDefinitionComposer;
        var stats = m_world.LevelStats;

        switch (type)
        {
            case StatusBarNumberType.Health: return Math.Max(0, player.Health);
            case StatusBarNumberType.Armor: return player.Armor;
            case StatusBarNumberType.Ammo:
                return StatusBarConditionResolver.TryGetId24AmmoType(composer, param, out var ammoDef)
                    ? player.Inventory.Amount(ammoDef.Name) : 0;
            case StatusBarNumberType.AmmoSelected:
                string? a = player.AnimationWeapon?.Definition.Properties.Weapons.AmmoType;
                return !string.IsNullOrEmpty(a) ? player.Inventory.Amount(a) : 0;
            case StatusBarNumberType.MaxAmmo:
                return StatusBarConditionResolver.TryGetId24AmmoType(composer, param, out var maxAmmoDef)
                    ? GetMaxAmount(player, maxAmmoDef.Name) : 0;
            case StatusBarNumberType.AmmoWeapon:
                var deh = m_archiveCollection.Definitions.DehackedDefinition;
                if (deh != null && deh.TryGetId24PickupType(composer, param, out var wDef))
                {
                    string weaponAmmo = wDef.Properties.Weapons.AmmoType;
                    return !string.IsNullOrEmpty(weaponAmmo) ? player.Inventory.Amount(weaponAmmo) : 0;
                }
                return 0;
            case StatusBarNumberType.MaxAmmoWeapon:
                var dehM = m_archiveCollection.Definitions.DehackedDefinition;
                if (dehM != null && dehM.TryGetId24PickupType(composer, param, out var mwDef))
                {
                    string maxWeaponAmmo = mwDef.Properties.Weapons.AmmoType;
                    return !string.IsNullOrEmpty(maxWeaponAmmo) ? GetMaxAmount(player, maxWeaponAmmo) : 0;
                }
                return 0;
            case StatusBarNumberType.Kills: return stats.KillCount;
            case StatusBarNumberType.Items: return stats.ItemCount;
            case StatusBarNumberType.Secrets: return stats.SecretCount;
            case StatusBarNumberType.KillsPercent:
                return stats.TotalMonsters > 0 ? (stats.KillCount * 100) / stats.TotalMonsters : 100;
            case StatusBarNumberType.ItemsPercent:
                return stats.TotalItems > 0 ? (stats.ItemCount * 100) / stats.TotalItems : 100;
            case StatusBarNumberType.SecretsPercent:
                return stats.TotalSecrets > 0 ? (stats.SecretCount * 100) / stats.TotalSecrets : 100;
            case StatusBarNumberType.MaxKills: return stats.TotalMonsters;
            case StatusBarNumberType.MaxItems: return stats.TotalItems;
            case StatusBarNumberType.MaxSecrets: return stats.TotalSecrets;
            case StatusBarNumberType.PowerupDuration:
                PowerupType pt = param switch
                {
                    0 => PowerupType.Invulnerable,
                    1 => PowerupType.Strength,
                    2 => PowerupType.Invisibility,
                    3 => PowerupType.IronFeet,
                    4 => PowerupType.ComputerAreaMap,
                    5 => PowerupType.LightAmp,
                    _ => PowerupType.None
                };

                if (pt == PowerupType.None) return 0;

                if (pt == PowerupType.Strength || pt == PowerupType.ComputerAreaMap)
                {
                    return player.Inventory.IsPowerupActive(pt) ? 1 : 0;
                }

                return (player.Inventory.GetPowerup(pt)?.Ticks ?? 0) / (int)Constants.TicksPerSecond;
            default: return 0;
        }
    }

    private static string GetSpeedometerText(Player player) { _ = player; return string.Empty; }

    private int GetMaxAmount(Player player, string name)
    {
        var def = m_archiveCollection.EntityDefinitionComposer.GetByName(name);
        if (def == null) return 0;
        var baseDef = Inventory.GetBaseInventoryDefinition(def) ?? def;
        int max = baseDef.Properties.Inventory.MaxAmount;
        if (player.Inventory.HasItemOfClass(Inventory.BackPackBaseClassName) && baseDef.IsType(Inventory.AmmoClassName))
            max = Math.Max(max, baseDef.Properties.Ammo.BackpackMaxAmount);
        return max;
    }

    private static Align ConvertAlignment(StatusBarAlignment sbarAlign)
    {
        bool hCenter = (sbarAlign & StatusBarAlignment.HCenter) != 0;
        bool right = (sbarAlign & StatusBarAlignment.Right) != 0;
        bool vCenter = (sbarAlign & StatusBarAlignment.VCenter) != 0;
        bool bottom = (sbarAlign & StatusBarAlignment.Bottom) != 0;

        if (bottom) return hCenter ? Align.BottomMiddle : (right ? Align.BottomRight : Align.BottomLeft);
        if (vCenter) return hCenter ? Align.Center : (right ? Align.MiddleRight : Align.MiddleLeft);
        return hCenter ? Align.TopMiddle : (right ? Align.TopRight : Align.TopLeft);
    }
    
    private static ImageBox2I? GetCropArea(IRenderableTextureHandle handle, StatusBarCropDef? cropDef)
    {
        if (cropDef == null) return null;

        int cx = cropDef.Left;
        int cy = cropDef.Top;
        
        if (cropDef.Center)
        {
            cx += handle.Dimension.Width / 2;
            cy += handle.Dimension.Height / 2;
        }

        int cw = cropDef.Width > 0 ? cropDef.Width : (handle.Dimension.Width - cx);
        int ch = cropDef.Height > 0 ? cropDef.Height : (handle.Dimension.Height - cy);

        return new ImageBox2I(cx, cy, cx + cw, cy + ch);
    }
}
