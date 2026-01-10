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
using Helion.Resources.Archives.Collection;
using Helion.Resources.Definitions.MapInfo;
using Helion.Resources.Definitions.StatusBar;
using Helion.Resources.Definitions.StatusBar.Enums;
using Helion.Strings;
using Helion.Util;
using Helion.Util.Configs.Components;
using Helion.World;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Definition.Composer;
using Helion.World.Entities.Inventories;
using Helion.World.Entities.Inventories.Powerups;
using Helion.World.Entities.Players;
using Helion.World.Stats;
using Helion.World.StatusBar;

namespace Helion.Layer.Worlds.StatusBar;

[Flags]
public enum StatusBarCoverage
{
    None = 0,
    Stats = 1 << 0, // for stat_totals
    Time = 1 << 1, // for time
    Messages = 1 << 2, // for message
    MapTitle = 1 << 3, // for level_title
    FPS = 1 << 4 // for fps_counter
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

    private readonly ArchiveCollection m_archiveCollection;
    private readonly List<CoordData> m_coordPartsCache = [];
    private readonly SpanString m_fmtSpan = new();
    private readonly Dictionary<string, StatusBarNumberFontDef> m_fontNumberLookup = [];
    private readonly List<RenderGlyph> m_glyphCache = [];
    private readonly Dictionary<string, StatusBarHudFontDef> m_hudFontLookup = [];
    private readonly SpanString m_lookupKeySpan = new(128);

    private readonly IWorld m_world;
    private float m_currentScale;
    private float m_hOffset;

    private bool m_texturesResolved;
    private float m_userScale;
    private float m_vOffset;

    public StatusBarRenderer(IWorld world)
    {
        m_world = world;
        m_archiveCollection = world.ArchiveCollection;
        StatusBarDefinition sbarDef = world.ArchiveCollection.Definitions.StatusBarDefinition;

        m_glyphCache.Capacity = 256;
        m_coordPartsCache.Capacity = 16;

        foreach (StatusBarNumberFontDef f in sbarDef.NumberFonts)
            m_fontNumberLookup[f.Name] = f;

        foreach (StatusBarHudFontDef f in sbarDef.HudFonts)
            m_hudFontLookup[f.Name] = f;
    }

    public static StatusBarCoverage GetCoverage(StatusBarLayoutDef layout)
    {
        return layout.Children.Count == 0 ? StatusBarCoverage.None : ScanChildren(layout.Children);
    }

    private static StatusBarCoverage ScanChildren(List<StatusBarElementWrapper> children)
    {
        StatusBarCoverage mask = StatusBarCoverage.None;
        foreach (StatusBarElementWrapper child in children)
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
                    case StatusBarComponentType.Unknown:
                    case StatusBarComponentType.Coordinates:
                    case StatusBarComponentType.Speedometer:
                    case StatusBarComponentType.AnnounceLevelTitle:
                    case StatusBarComponentType.RenderStats:
                    case StatusBarComponentType.CommandHistory:
                    case StatusBarComponentType.Chat:
                    default: break;
                }

                if (child.Component.Children != null)
                    mask |= ScanChildren(child.Component.Children);
            }

            if (child.Canvas?.Children != null) mask |= ScanChildren(child.Canvas.Children);
            if (child.Native?.Children != null) mask |= ScanChildren(child.Native.Children);
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

        const int Width = 320;
        const int Height = 200;

        hud.PushVirtualDimension((Width, Height), ResolutionScale.Center, Constants.DoomVirtualAspectRatio);

        int yOffset = layout.FullscreenRender ? 0 : 200 - layout.Height;
        Vec2I rootPos = (0, yOffset);

        float windowWidth = hud.WindowDimension.Width;
        float windowHeight = hud.WindowDimension.Height;

        float scaleX = windowWidth / 320f;
        float scaleY = windowHeight / 200f;
        m_currentScale = Math.Min(scaleX, scaleY);
        m_userScale = (float)m_world.Config.Hud.Scale.Value;

        m_hOffset = (windowWidth / m_currentScale - 320f) / 2f;
        m_vOffset = (windowHeight / m_currentScale - 200f) / 2f;

        if (!layout.FullscreenRender)
        {
            string fillFlat = layout.FillFlat ?? m_world.GameInfo.BorderFlat;

            if (!string.IsNullOrEmpty(fillFlat) && hud.Textures.TryGet(fillFlat, out IRenderableTextureHandle? bgHandle))
            {
                int bgWidth = bgHandle.Dimension.Width;
                int bgHeight = bgHandle.Dimension.Height;
                if (bgHeight <= 0) bgHeight = 64;

                int startX = (int)-Math.Ceiling(m_hOffset);
                int endX = 320 + (int)Math.Ceiling(m_hOffset);
                int endY = 200 + (int)Math.Ceiling(m_vOffset);

                for (int x = (startX / bgWidth - 1) * bgWidth; x < endX; x += bgWidth)
                for (int y = yOffset; y < endY; y += bgHeight)
                    hud.Image(fillFlat, (x, y), anchor: Align.TopLeft);
            }
        }

        foreach (StatusBarElementWrapper child in layout.Children)
            DrawElementWrapper(hud, child, rootPos, layout.Height, context, m_hOffset, rootPos, false);

        hud.PopVirtualDimension();
    }

    private void EnsureTexturesResolved(IHudRenderContext hud, StatusBarLayoutDef layout)
    {
        foreach (StatusBarElementWrapper t in layout.Children) ResolveElementTextures(hud, t);
    }

    private void ResolveElementTextures(IHudRenderContext hud, StatusBarElementWrapper wrapper)
    {
        if (wrapper.Graphic != null)
        {
            wrapper.Graphic.ResolvedPatchName = ResolvePatchName(wrapper.Graphic.Patch);

            if (hud.Textures.TryGet(wrapper.Graphic.ResolvedPatchName, out IRenderableTextureHandle? handle) ||
                hud.Textures.TryGet(wrapper.Graphic.ResolvedPatchName, out handle, ResourceNamespace.Sprites))
            {
                wrapper.Graphic.Handle = handle;
                wrapper.Graphic.ResolvedHeight = handle.Dimension.Height;
            }
        }

        if (wrapper.Animation != null)
            for (int i = 0; i < wrapper.Animation.Frames.Count; i++)
            {
                StatusBarFrameDef frame = wrapper.Animation.Frames[i];
                frame.ResolvedPatchName = ResolvePatchName(frame.Lump);

                if (hud.Textures.TryGet(frame.ResolvedPatchName, out IRenderableTextureHandle? handle) ||
                    hud.Textures.TryGet(frame.ResolvedPatchName, out handle, ResourceNamespace.Sprites))
                    frame.Handle = handle;

                wrapper.Animation.Frames[i] = frame;
            }

        if (wrapper.String != null)
        {
            if (m_hudFontLookup.TryGetValue(wrapper.String.Font, out StatusBarHudFontDef? f))
            {
                string zeroPatch = GetHudFontPatch(f, '0');
                wrapper.String.ResolvedHeight = hud.Textures.TryGet(zeroPatch, out IRenderableTextureHandle? h)
                    ? h.Dimension.Height
                    : hud.GetFontMaxHeight(f.Stem);
            }
            else
            {
                wrapper.String.ResolvedHeight = 8;
            }
        }
        else if (wrapper.Number != null || wrapper.Percent != null)
        {
            StatusBarBaseDef? num = (StatusBarBaseDef?)wrapper.Number ?? wrapper.Percent;
            if (m_fontNumberLookup.TryGetValue(wrapper.Number?.Font ?? wrapper.Percent?.Font ?? string.Empty,
                    out StatusBarNumberFontDef? nf))
            {
                string zeroPatch = GetFontPatch(hud, nf, '0');
                num!.ResolvedHeight = hud.Textures.TryGet(zeroPatch, out IRenderableTextureHandle? h) ? h.Dimension.Height : 8;
            }
            else
            {
                num!.ResolvedHeight = 8;
            }
        }
        else if (wrapper.Face != null || wrapper.FaceBackground != null)
        {
            StatusBarBaseDef? face = (StatusBarBaseDef?)wrapper.Face ?? wrapper.FaceBackground;

            face!.ResolvedHeight = hud.Textures.TryGet("STFST00", out IRenderableTextureHandle? h) ||
                                   hud.Textures.TryGet("STFST00", out h, ResourceNamespace.Sprites)
                ? h.Dimension.Height
                : 32;
        }

        if (wrapper.FaceBackground != null)
            if (hud.Textures.TryGet("STFB0", out IRenderableTextureHandle? handle) ||
                hud.Textures.TryGet("STFB0", out handle, ResourceNamespace.Sprites))
                wrapper.FaceBackground.Handle = handle;

        StatusBarBaseDef? baseDef = null;
        if (wrapper.Canvas != null) baseDef = wrapper.Canvas;
        else if (wrapper.List != null) baseDef = wrapper.List;
        else if (wrapper.Native != null) baseDef = wrapper.Native;
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
        foreach (StatusBarElementWrapper t in baseDef.Children)
            ResolveElementTextures(hud, t);
    }

    private void DrawElementWrapper(IHudRenderContext hud,
        StatusBarElementWrapper wrapper,
        Vec2I parentPos,
        int containerHeight,
        StatusBarContext context,
        float widescreenOffset,
        Vec2I rootPos,
        bool isNative)
    {
        Vec2I effectiveParentPos = parentPos;
        bool isHudWidget = wrapper.Component != null || wrapper.Carousel != null;

        if (isHudWidget && parentPos == rootPos && !isNative) effectiveParentPos = (0, 0);

        if (wrapper.Canvas != null)
            DrawBase(hud, wrapper.Canvas, parentPos, containerHeight, context, widescreenOffset, rootPos, isNative);
        else if (wrapper.Native != null)
            DrawNative(hud, wrapper.Native, parentPos, containerHeight, context);
        else if (wrapper.List != null)
            DrawList(hud, wrapper.List, parentPos, containerHeight, context, widescreenOffset, rootPos, isNative);
        else if (wrapper.Graphic != null)
            DrawGraphic(hud, wrapper.Graphic, parentPos, containerHeight, context, widescreenOffset, isNative);
        else if (wrapper.Number != null)
            DrawNumber(hud, wrapper.Number, parentPos, containerHeight, context, false, widescreenOffset, isNative);
        else if (wrapper.Percent != null)
            DrawNumber(hud, wrapper.Percent, parentPos, containerHeight, context, true, widescreenOffset, isNative);
        else if (wrapper.String != null)
            DrawString(hud, wrapper.String, parentPos, containerHeight, context, widescreenOffset, isNative);
        else if (wrapper.Face != null)
            DrawFace(hud, wrapper.Face, parentPos, containerHeight, context, widescreenOffset, isNative);
        else if (wrapper.FaceBackground != null)
            DrawFaceBackground(hud, wrapper.FaceBackground, parentPos, containerHeight, context, widescreenOffset, isNative);
        else if (wrapper.Animation != null)
            DrawAnimation(hud, wrapper.Animation, parentPos, containerHeight, context, widescreenOffset, isNative);
        else if (wrapper.Component != null)
            DrawComponent(hud, wrapper.Component, effectiveParentPos, containerHeight, context, widescreenOffset, rootPos, isNative);
        else if (wrapper.Carousel != null)
            DrawCarousel(hud, wrapper.Carousel, effectiveParentPos, containerHeight, context, widescreenOffset, rootPos, isNative);
    }

    private void DrawBase(IHudRenderContext hud,
        StatusBarCanvasDef def,
        Vec2I parentPos,
        int containerHeight,
        StatusBarContext context,
        float widescreenOffset,
        Vec2I rootPos,
        bool isNative)
    {
        if (!StatusBarConditionResolver.Evaluate(context, def.Conditions))
            return;

        Vec2I currentPos = ResolvePosition(def, parentPos, isNative, isNative ? m_userScale : 1.0f);
        DrawChildren(hud, def, currentPos, containerHeight, context, widescreenOffset, rootPos, isNative);
    }

    private void DrawList(IHudRenderContext hud,
        StatusBarListDef def,
        Vec2I parentPos,
        int containerHeight,
        StatusBarContext context,
        float widescreenOffset,
        Vec2I rootPos,
        bool isNative)
    {
        if (!StatusBarConditionResolver.Evaluate(context, def.Conditions) || def.Children == null)
            return;

        float scaleX = isNative ? m_userScale : 1.0f;
        float scaleY = isNative ? m_userScale * 1.2f : 1.0f;

        int childCount = def.Children.Count;
        Span<Vec2I> sizes = stackalloc Vec2I[childCount];
        int activeCount = 0;
        int totalWidth = 0;
        int totalHeight = 0;

        int spacing = isNative ? (int)(def.Spacing * m_userScale) : def.Spacing;

        foreach (StatusBarElementWrapper child in def.Children)
        {
            if (!EvaluateWrapperConditions(child, context)) continue;

            ElementBounds bounds = MeasureElement(hud, child, context, isNative, scaleX, scaleY);
            Vec2I size = new(bounds.Width, bounds.Height);
            sizes[activeCount] = size;

            if (def.Horizontal)
            {
                totalWidth += size.X;
                if (activeCount > 0) totalWidth += spacing;
                totalHeight = Math.Max(totalHeight, size.Y);
            }
            else
            {
                totalHeight += size.Y;
                if (activeCount > 0) totalHeight += spacing;
                totalWidth = Math.Max(totalWidth, size.X);
            }

            activeCount++;
        }

        if (activeCount == 0) return;

        Vec2I listPos = ResolvePosition(def, parentPos, isNative, scaleX);

        if ((def.Alignment & StatusBarAlignment.HCenter) != 0) listPos.X -= totalWidth / 2;
        else if ((def.Alignment & StatusBarAlignment.Right) != 0) listPos.X -= totalWidth;

        if ((def.Alignment & StatusBarAlignment.Bottom) != 0) listPos.Y -= totalHeight;
        else if ((def.Alignment & StatusBarAlignment.VCenter) != 0) listPos.Y -= totalHeight / 2;

        int currentIdx = 0;
        Vec2I cursor = listPos;
        foreach (StatusBarElementWrapper child in def.Children)
        {
            if (!EvaluateWrapperConditions(child, context)) continue;

            Vec2I size = sizes[currentIdx];
            Vec2I childPos = cursor;

            if (def.Horizontal)
            {
                if ((def.Alignment & StatusBarAlignment.Bottom) != 0)
                    childPos.Y += totalHeight - size.Y;
                else if ((def.Alignment & StatusBarAlignment.VCenter) != 0)
                    childPos.Y += (totalHeight - size.Y) / 2;

                DrawElementWrapper(hud, child, childPos, containerHeight, context, widescreenOffset, rootPos, isNative);
                cursor.X += size.X + spacing;
            }
            else
            {
                if ((def.Alignment & StatusBarAlignment.Right) != 0)
                    childPos.X += totalWidth - size.X;
                else if ((def.Alignment & StatusBarAlignment.HCenter) != 0)
                    childPos.X += (totalWidth - size.X) / 2;

                DrawElementWrapper(hud, child, childPos, containerHeight, context, widescreenOffset, rootPos, isNative);
                cursor.Y += size.Y + spacing;
            }

            currentIdx++;
        }
    }

    private void DrawNative(IHudRenderContext hud, StatusBarNativeDef def, Vec2I parentPos, int containerHeight, StatusBarContext context)
    {
        if (!StatusBarConditionResolver.Evaluate(context, def.Conditions))
            return;

        Vec2I vPos = ResolvePosition(def, parentPos, false, 1.0f);

        float scaleX = m_userScale;
        float scaleY = m_userScale * 1.2f;

        ElementBounds bounds = ElementBounds.Empty;
        if (def.Children != null)
            foreach (StatusBarElementWrapper child in def.Children)
            {
                if (!EvaluateWrapperConditions(child, context)) continue;
                bounds = ElementBounds.Union(bounds, MeasureElement(hud, child, context, true, scaleX, scaleY));
            }

        if (bounds.X1 == int.MaxValue) return;

        int pivotX = (bounds.X1 + bounds.X2) / 2;
        int pivotY = (bounds.Y1 + bounds.Y2) / 2;

        int nativeX = (int)Math.Floor((vPos.X + m_hOffset) * m_currentScale);
        int nativeY = (int)Math.Floor((vPos.Y + m_vOffset) * m_currentScale);

        bool isHCenter = (def.Alignment & StatusBarAlignment.HCenter) != 0;
        bool isVCenter = (def.Alignment & StatusBarAlignment.VCenter) != 0;

        int hShift = (int)Math.Ceiling(m_hOffset * m_currentScale);
        int vShift = (int)Math.Ceiling(m_vOffset * m_currentScale);

        if (!isHCenter)
        {
            if ((def.Alignment & StatusBarAlignment.Right) != 0)
                nativeX += hShift;
            else
                nativeX -= hShift;
        }

        if (!isVCenter)
        {
            if ((def.Alignment & StatusBarAlignment.Bottom) != 0)
                nativeY += vShift;
            else
                nativeY -= vShift;
        }

        if (isHCenter) nativeX -= pivotX;
        else if ((def.Alignment & StatusBarAlignment.Right) != 0) nativeX -= bounds.X2;
        else nativeX -= bounds.X1;

        if (isVCenter) nativeY -= pivotY;
        else if ((def.Alignment & StatusBarAlignment.Bottom) != 0) nativeY -= bounds.Y2;
        else nativeY -= bounds.Y1;

        Vec2I nativeRoot = (nativeX, nativeY);

        hud.PopVirtualDimension();
        DrawChildren(hud, def, nativeRoot, containerHeight, context, 0, nativeRoot, true);
        hud.PushVirtualDimension((320, 200), ResolutionScale.Center, Constants.DoomVirtualAspectRatio);
    }

    private void DrawGraphic(IHudRenderContext hud,
        StatusBarGraphicDef graphic,
        Vec2I parentPos,
        int containerHeight,
        StatusBarContext context,
        float widescreenOffset,
        bool isNative)
    {
        if (!StatusBarConditionResolver.Evaluate(context, graphic.Conditions))
            return;

        Vec2I currentPos = ResolvePosition(graphic, parentPos, isNative, isNative ? m_userScale : 1.0f);

        if (graphic.Handle != null || !string.IsNullOrEmpty(graphic.Patch))
        {
            Align align = ConvertAlignment(graphic.Alignment);
            if (graphic.MidOffset != 0) currentPos.X += (int)(graphic.MidOffset * (isNative ? m_userScale : 1.0f));

            float alpha = graphic.Translucency ? 0.5f : 1.0f;

            DrawSBarTexture(hud,
                graphic.ResolvedPatchName ?? graphic.Patch,
                graphic.Handle,
                currentPos,
                align,
                graphic.Alignment,
                graphic.Translation,
                alpha,
                graphic.Crop,
                isNative);
        }

        DrawChildren(hud, graphic, currentPos, containerHeight, context, widescreenOffset, (0, 0), isNative);
    }

    private void DrawFace(IHudRenderContext hud,
        StatusBarFaceDef face,
        Vec2I parentPos,
        int containerHeight,
        StatusBarContext context,
        float widescreenOffset,
        bool isNative)
    {
        if (!StatusBarConditionResolver.Evaluate(context, face.Conditions))
            return;

        Vec2I currentPos = ResolvePosition(face, parentPos, isNative, isNative ? m_userScale : 1.0f);
        string patch = context.Player.StatusBar.GetFacePatch();

        if (!string.IsNullOrEmpty(patch))
        {
            Align align = ConvertAlignment(face.Alignment);
            float alpha = face.Translucency ? 0.5f : 1.0f;

            DrawSBarTexture(hud, patch, null, currentPos, align, face.Alignment, face.Translation, alpha, face.Crop, isNative);
        }

        DrawChildren(hud, face, currentPos, containerHeight, context, widescreenOffset, (0, 0), isNative);
    }

    private void DrawFaceBackground(IHudRenderContext hud,
        StatusBarFaceDef faceBg,
        Vec2I parentPos,
        int containerHeight,
        StatusBarContext context,
        float widescreenOffset,
        bool isNative)
    {
        if (!StatusBarConditionResolver.Evaluate(context, faceBg.Conditions))
            return;

        Vec2I currentPos = ResolvePosition(faceBg, parentPos, isNative, isNative ? m_userScale : 1.0f);

        if (faceBg.Handle != null)
        {
            Align align = ConvertAlignment(faceBg.Alignment);
            float alpha = faceBg.Translucency ? 0.5f : 1.0f;

            DrawSBarTexture(hud,
                "STFB0",
                faceBg.Handle,
                currentPos,
                align,
                faceBg.Alignment,
                faceBg.Translation,
                alpha,
                isNative: isNative);
        }

        DrawChildren(hud, faceBg, currentPos, containerHeight, context, widescreenOffset, (0, 0), isNative);
    }

    private void DrawAnimation(IHudRenderContext hud,
        StatusBarAnimationDef anim,
        Vec2I parentPos,
        int containerHeight,
        StatusBarContext context,
        float widescreenOffset,
        bool isNative)
    {
        if (!StatusBarConditionResolver.Evaluate(context, anim.Conditions))
            return;

        Vec2I currentPos = ResolvePosition(anim, parentPos, isNative, isNative ? m_userScale : 1.0f);

        if (anim.Frames.Count > 0)
        {
            double totalDuration = 0;
            foreach (StatusBarFrameDef frame1 in anim.Frames)
                totalDuration += frame1.Duration;

            if (totalDuration > 0)
            {
                double timePerLoop = totalDuration * Constants.TicksPerSecond;
                long currentTick = m_world.LevelTime;
                double animTime = currentTick % timePerLoop;

                StatusBarFrameDef frame = anim.Frames[0];
                double timeAccumulator = 0;

                foreach (StatusBarFrameDef f in anim.Frames)
                {
                    timeAccumulator += f.Duration * Constants.TicksPerSecond;
                    if (!(animTime < timeAccumulator)) continue;
                    frame = f;
                    break;
                }

                Align align = ConvertAlignment(anim.Alignment);
                DrawSBarTexture(hud,
                    frame.ResolvedPatchName ?? frame.Lump,
                    frame.Handle,
                    currentPos,
                    align,
                    anim.Alignment,
                    anim.Translation,
                    isNative: isNative);
            }
        }

        DrawChildren(hud, anim, currentPos, containerHeight, context, widescreenOffset, (0, 0), isNative);
    }

    private void DrawString(IHudRenderContext hud,
        StatusBarStringDef def,
        Vec2I parentPos,
        int containerHeight,
        StatusBarContext context,
        float widescreenOffset,
        bool isNative)
    {
        if (!StatusBarConditionResolver.Evaluate(context, def.Conditions))
            return;

        Vec2I pos = ResolvePosition(def, parentPos, isNative, isNative ? m_userScale : 1.0f);
        ReadOnlySpan<char> text = GetStringValue(def);
        if (text.IsEmpty) return;

        int fontHeight = m_hudFontLookup.TryGetValue(def.Font, out StatusBarHudFontDef? fontDef) ? hud.GetFontMaxHeight(fontDef.Stem) : 8;

        if (fontHeight <= 0) fontHeight = 8;
        float alpha = def.Translucency ? 0.5f : 1.0f;

        RenderLines(hud, text, pos, fontDef, fontHeight, def.Alignment, def.Translation, alpha, isNative);
        DrawChildren(hud, def, pos, containerHeight, context, widescreenOffset, (0, 0), isNative);
    }

    private void DrawComponent(IHudRenderContext hud,
        StatusBarComponentDef comp,
        Vec2I parentPos,
        int containerHeight,
        StatusBarContext context,
        float widescreenOffset,
        Vec2I rootPos,
        bool isNative)
    {
        if (!StatusBarConditionResolver.Evaluate(context, comp.Conditions))
            return;

        Vec2I pos = ResolvePosition(comp, parentPos, isNative, isNative ? m_userScale : 1.0f);
        ConfigHud config = m_world.Config.Hud;
        float alpha = comp.Translucency ? 0.5f : 1.0f;
        StatusBarAlignment alignment = comp.Alignment;

        int fontHeight = 8;
        if (m_hudFontLookup.TryGetValue(comp.Font, out StatusBarHudFontDef? fontDef) &&
            StemToHelionFontMap.TryGetValue(fontDef.Stem, out string? helionFontName))
        {
            int h = hud.GetFontMaxHeight(helionFontName);
            if (h > 0) fontHeight = h;
        }

        switch (comp.ComponentType)
        {
            case StatusBarComponentType.StatTotals:
                if (!config.ShowStats.Value) return;
                DrawStatTotals(hud, comp, pos, fontDef, fontHeight, alpha, isNative);
                break;

            case StatusBarComponentType.Time:
                TimeSpan t = TimeSpan.FromSeconds(m_world.LevelTime / 35.0);
                m_fmtSpan.Clear();
                m_fmtSpan.Append((int)t.TotalHours, 2);
                m_fmtSpan.Append(':');
                m_fmtSpan.Append(t.Minutes, 2);
                m_fmtSpan.Append(':');
                m_fmtSpan.Append(t.Seconds, 2);
                RenderLines(hud, m_fmtSpan.AsSpan(), pos, fontDef, fontHeight, alignment, comp.Translation, alpha, isNative);
                break;
            case StatusBarComponentType.Coordinates:
                DrawCoordinates(hud, comp, pos, fontDef, fontHeight, alpha, context, isNative);
                break;
            case StatusBarComponentType.Speedometer:
                string speedText = GetSpeedometerText(context.Player);
                RenderLines(hud, speedText.AsSpan(), pos, fontDef, fontHeight, alignment, comp.Translation, alpha, isNative);
                break;
            case StatusBarComponentType.LevelTitle:
                string levelTitle = m_world.MapInfo.GetDisplayNameWithPrefix(m_archiveCollection.Language);
                RenderLines(hud, levelTitle.AsSpan(), pos, fontDef, fontHeight, alignment, comp.Translation, alpha, isNative);
                break;
            case StatusBarComponentType.FpsCounter:
                if (!config.ShowFPS.Value) return;
                m_fmtSpan.Clear();
                m_fmtSpan.Append(context.Fps);
                RenderLines(hud, m_fmtSpan.AsSpan(), pos, fontDef, fontHeight, alignment, comp.Translation, alpha, isNative);
                break;
            case StatusBarComponentType.Message:
                string msg = context.ConsoleMessage ?? string.Empty;
                if (context.IsMessageCentered)
                {
                    pos = (160, 66);
                    alignment = StatusBarAlignment.HCenter;
                }

                RenderLines(hud, msg.AsSpan(), pos, fontDef, fontHeight, alignment, comp.Translation, alpha, isNative);
                break;
            case StatusBarComponentType.AnnounceLevelTitle:
                double duration = comp.Duration > 0 ? comp.Duration : 2.5;
                const double FadeInTime = 0.25;
                const double FadeOutTime = 1.0;
                double timeSinceStart = m_world.LevelTime / Constants.TicksPerSecond;

                if (timeSinceStart > duration + FadeOutTime)
                    return;

                if (timeSinceStart < FadeInTime)
                {
                    alpha *= (float)(timeSinceStart / FadeInTime);
                }
                else if (timeSinceStart > duration)
                {
                    double progress = (timeSinceStart - duration) / FadeOutTime;
                    alpha *= (float)(1.0 - progress);
                }

                string annTitle = m_world.MapInfo.GetDisplayNameWithPrefix(m_archiveCollection.Language);
                RenderLines(hud, annTitle.AsSpan(), pos, fontDef, fontHeight, alignment, comp.Translation, alpha, isNative);
                break;
            case StatusBarComponentType.Unknown:
            case StatusBarComponentType.RenderStats:
            case StatusBarComponentType.CommandHistory:
            case StatusBarComponentType.Chat:
            default:
                break;
        }

        DrawChildren(hud, comp, pos, containerHeight, context, widescreenOffset, rootPos, isNative);
    }

    private void RenderLines(IHudRenderContext hud,
        ReadOnlySpan<char> text,
        Vec2I pos,
        StatusBarHudFontDef? fontDef,
        int fontHeight,
        StatusBarAlignment alignment,
        string? translation,
        float alpha,
        bool isNative)
    {
        if (text.IsEmpty) return;

        Vec2I drawPos = pos;
        int lineStart = 0;
        float scaleY = isNative ? m_userScale * 1.2f : 1.0f;

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] != '\n') continue;
            ReadOnlySpan<char> line = text[lineStart..i];
            DrawSingleLine(hud, line, drawPos, fontDef, alignment, translation, alpha, isNative);
            drawPos.Y += (int)(fontHeight * scaleY);
            lineStart = i + 1;
        }

        if (lineStart >= text.Length) return;
        {
            ReadOnlySpan<char> line = text[lineStart..];
            DrawSingleLine(hud, line, drawPos, fontDef, alignment, translation, alpha, isNative);
        }
    }

    private void DrawSingleLine(IHudRenderContext hud,
        ReadOnlySpan<char> line,
        Vec2I drawPos,
        StatusBarHudFontDef? fontDef,
        StatusBarAlignment alignment,
        string? translation,
        float alpha,
        bool isNative)
    {
        if (line.IsEmpty) return;

        int drawnWidth = 0;
        if (fontDef != null) drawnWidth = DrawHudText(hud, line, fontDef, drawPos, alignment, translation, alpha, isNative);

        if (drawnWidth != 0) return;
        Align align = ConvertAlignment(alignment);
        float textScale = isNative ? m_userScale : 1.0f;
        hud.Text(line, Constants.Fonts.Small, 8, drawPos, both: align, alpha: alpha, scale: textScale);
    }

    private int DrawHudText(IHudRenderContext hud,
        ReadOnlySpan<char> text,
        StatusBarHudFontDef fontDef,
        Vec2I pos,
        StatusBarAlignment alignment,
        string? translation,
        float alpha,
        bool isNative,
        bool draw = true)
    {
        Color? drawColor = null;
        if (!string.IsNullOrEmpty(translation) && StandardTextColors.TryGetValue(translation, out Color colorValue))
            drawColor = colorValue;

        float scaleX = isNative ? m_userScale : 1.0f;
        float scaleY = isNative ? m_userScale * 1.2f : 1.0f;

        if (StemToHelionFontMap.TryGetValue(fontDef.Stem, out string? helionFont))
        {
            if (!draw) return (int)(hud.MeasureText(text, helionFont, 8).Width * scaleX);
            Align anchor = ConvertAlignment(alignment);
            hud.Text(text, helionFont, 8, pos, TextAlign.Left, Align.TopLeft, anchor, color: drawColor, alpha: alpha, scale: scaleX);

            return (int)(hud.MeasureText(text, helionFont, 8).Width * scaleX);
        }

        int totalWidth = 0;
        int maxHeight = 0;

        m_glyphCache.Clear();

        int monoWidth = 0;
        switch (fontDef.Type)
        {
            case 1:
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

                break;
            }
            case 0:
            {
                string zero = GetHudFontPatch(fontDef, '0');
                if (hud.Textures.TryGet(zero, out IRenderableTextureHandle? zh)) monoWidth = zh.Dimension.Width;
                break;
            }
        }

        foreach (char originalChar in text)
        {
            int width;
            string patch = string.Empty;
            char c = originalChar;

            if (c == ' ')
            {
                string bang = GetHudFontPatch(fontDef, '!');
                width = hud.Textures.TryGet(bang, out IRenderableTextureHandle? h) ? h.Dimension.Width : 4;
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

            int scaledWidth = (int)(width * scaleX);
            m_glyphCache.Add(new RenderGlyph(patch, scaledWidth, 0));
            totalWidth += scaledWidth;
        }

        if (m_glyphCache.Count == 0 && text.Length > 0) return 0;
        if (!draw) return totalWidth;

        int drawX = pos.X;
        int drawY = pos.Y;

        if ((alignment & StatusBarAlignment.HCenter) != 0) drawX -= totalWidth / 2;
        else if ((alignment & StatusBarAlignment.Right) != 0) drawX -= totalWidth;

        int scaledMaxHeight = (int)(maxHeight * scaleY);
        if ((alignment & StatusBarAlignment.Bottom) != 0) drawY -= scaledMaxHeight;
        else if ((alignment & StatusBarAlignment.VCenter) != 0) drawY -= scaledMaxHeight / 2;

        foreach (RenderGlyph g in m_glyphCache)
        {
            if (!string.IsNullOrEmpty(g.Patch))
                DrawSBarTexture(hud, g.Patch, null, (drawX, drawY), Align.TopLeft, alignment, translation, alpha, isNative: isNative);
            drawX += g.Width;
        }

        return totalWidth;
    }

    private void DrawCarousel(IHudRenderContext hud,
        StatusBarCarouselDef carousel,
        Vec2I parentPos,
        int containerHeight,
        StatusBarContext context,
        float widescreenOffset,
        Vec2I rootPos,
        bool isNative)
    {
        if (!StatusBarConditionResolver.Evaluate(context, carousel.Conditions))
            return;

        Vec2I pos = ResolvePosition(carousel, parentPos, isNative, isNative ? m_userScale : 1.0f);
        pos.X = 160;

        if (context.Player.Weapon != null)
        {
            string icon = context.Player.Weapon.Definition.Properties.Inventory.Icon;
            if (!string.IsNullOrEmpty(icon))
            {
                Align align = ConvertAlignment(carousel.Alignment);
                float alpha = carousel.Translucency ? 0.5f : 1.0f;
                DrawSBarTexture(hud, icon, null, pos, align, carousel.Alignment, carousel.Translation, alpha, isNative: isNative);
            }
        }

        DrawChildren(hud, carousel, pos, containerHeight, context, widescreenOffset, rootPos, isNative);
    }


    private void DrawSBarTexture(IHudRenderContext hud,
        string patch,
        IRenderableTextureHandle? handle,
        Vec2I pos,
        Align align,
        StatusBarAlignment sbarAlign,
        string? translation = null,
        float alpha = 1.0f,
        StatusBarCropDef? cropDef = null,
        bool isNative = false)
    {
        string pName = handle == null ? ResolvePatchName(patch) : patch;
        if (handle == null && !hud.Textures.TryGet(pName, out handle) &&
            !hud.Textures.TryGet(pName, out handle, ResourceNamespace.Sprites)) return;

        ImageBox2I? cropArea = GetCropArea(handle, cropDef);
        float scaleX = isNative ? m_userScale : 1.0f;
        float scaleY = isNative ? m_userScale * 1.2f : 1.0f;

        Vec2I drawPos = pos;
        Vec2I translatedOffset = RenderDimensions.TranslateDoomOffset(handle.Offset);

        if ((sbarAlign & StatusBarAlignment.IgnoreLeftOffset) == 0)
            drawPos.X += (int)(translatedOffset.X * scaleX);

        if ((sbarAlign & StatusBarAlignment.IgnoreTopOffset) == 0)
            drawPos.Y += (int)(translatedOffset.Y * scaleY);

        Color? drawColor = null;
        if (!string.IsNullOrEmpty(translation) && StandardTextColors.TryGetValue(translation, out Color colorValue))
            drawColor = colorValue;

        int w = (int)(handle.Dimension.Width * scaleX);
        int h = (int)(handle.Dimension.Height * scaleY);
        int x = drawPos.X;
        int y = drawPos.Y;

        switch (align)
        {
            case Align.TopMiddle:
            case Align.Center:
            case Align.BottomMiddle: x -= w / 2; break;
            case Align.TopRight:
            case Align.MiddleRight:
            case Align.BottomRight: x -= w; break;
            case Align.TopLeft:
            case Align.MiddleLeft:
            case Align.BottomLeft:
            default:
                break;
        }

        switch (align)
        {
            case Align.MiddleLeft:
            case Align.Center:
            case Align.MiddleRight: y -= h / 2; break;
            case Align.BottomLeft:
            case Align.BottomMiddle:
            case Align.BottomRight: y -= h; break;
            case Align.TopLeft:
            case Align.TopMiddle:
            case Align.TopRight:
            default:
                break;
        }

        hud.Image(pName,
            new HudBox((x, y), (x + w, y + h)),
            out _,
            Align.TopLeft,
            Align.TopLeft,
            null,
            ResourceNamespace.Undefined,
            drawColor,
            1.0f,
            alpha,
            0,
            1,
            null,
            cropArea);
    }

    private void DrawNumber(IHudRenderContext hud,
        StatusBarNumberDef number,
        Vec2I parentPos,
        int containerHeight,
        StatusBarContext context,
        bool isPercent,
        float widescreenOffset,
        bool isNative)
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

        if (!m_fontNumberLookup.TryGetValue(number.Font, out StatusBarNumberFontDef? fontDef))
            return;

        m_fmtSpan.Clear();
        m_fmtSpan.Append(value);
        if (isPercent) m_fmtSpan.Append('%');

        ReadOnlySpan<char> text = m_fmtSpan.AsSpan();
        float scaleX = isNative ? m_userScale : 1.0f;

        Vec2I pos = ResolvePosition(number, parentPos, isNative, scaleX);

        float alpha = number.Translucency ? 0.5f : 1.0f;
        int totalWidth = 0;
        int monoWidth = 0;

        switch (fontDef.Type)
        {
            case 0:
            {
                string zeroPatch = GetFontPatch(hud, fontDef, '0');
                if (hud.Textures.TryGet(zeroPatch, out IRenderableTextureHandle? zeroHandle))
                    monoWidth = zeroHandle.Dimension.Width;
                break;
            }
            case 1:
            {
                if (!Type1WidthCache.TryGetValue(fontDef.Stem, out monoWidth))
                {
                    monoWidth = 0;
                    for (char d = '0'; d <= '9'; d++)
                    {
                        string dPatch = GetFontPatch(hud, fontDef, d);
                        if (hud.Textures.TryGet(dPatch, out IRenderableTextureHandle? dHandle))
                            monoWidth = Math.Max(monoWidth, dHandle.Dimension.Width);
                    }

                    Type1WidthCache[fontDef.Stem] = monoWidth;
                }

                break;
            }
        }

        m_glyphCache.Clear();

        foreach (char c in text)
        {
            string patch = GetFontPatch(hud, fontDef, c);
            int width;
            int xOffset = 0;

            if (hud.Textures.TryGet(patch, out IRenderableTextureHandle? handle) ||
                hud.Textures.TryGet(patch, out handle, ResourceNamespace.Sprites)) width = handle.Dimension.Width;
            else
                continue;

            if (fontDef.Type is 0 or 1)
                if (monoWidth > 0)
                {
                    if (width < monoWidth) xOffset = (monoWidth - width) / 2;
                    width = monoWidth;
                }

            int scaledWidth = (int)(width * scaleX);
            m_glyphCache.Add(new RenderGlyph(patch, scaledWidth, (int)(xOffset * scaleX), handle));
            totalWidth += scaledWidth;
        }

        int drawX = pos.X;
        int drawY = pos.Y;

        if ((number.Alignment & StatusBarAlignment.HCenter) != 0) drawX -= totalWidth / 2;
        else if ((number.Alignment & StatusBarAlignment.Right) != 0) drawX -= totalWidth;

        Align yAnchor = (number.Alignment & StatusBarAlignment.Bottom) != 0 ? Align.BottomLeft : Align.TopLeft;

        foreach (RenderGlyph g in m_glyphCache)
        {
            Vec2I drawPos = (drawX + g.Offset, drawY);
            DrawSBarTexture(hud, g.Patch, g.Handle, drawPos, yAnchor, number.Alignment, number.Translation, alpha, isNative: isNative);
            drawX += g.Width;
        }

        DrawChildren(hud, number, pos, containerHeight, context, widescreenOffset, (0, 0), isNative);
    }

    private void DrawStatTotals(IHudRenderContext hud,
        StatusBarComponentDef comp,
        Vec2I pos,
        StatusBarHudFontDef? fontDef,
        int fontHeight,
        float alpha,
        bool isNative)
    {
        LevelStats stats = m_world.LevelStats;
        Vec2I cur = pos;
        DrawStatPart(hud, "K: ", stats.KillCount, stats.TotalMonsters, ref cur, comp, fontDef, fontHeight, alpha, isNative);
        DrawStatPart(hud, "I: ", stats.ItemCount, stats.TotalItems, ref cur, comp, fontDef, fontHeight, alpha, isNative);
        DrawStatPart(hud, "S: ", stats.SecretCount, stats.TotalSecrets, ref cur, comp, fontDef, fontHeight, alpha, isNative);
    }

    private void DrawStatPart(IHudRenderContext hud,
        string label,
        int count,
        int total,
        ref Vec2I cursor,
        StatusBarComponentDef comp,
        StatusBarHudFontDef? fontDef,
        int fontHeight,
        float alpha,
        bool isNative)
    {
        string? defaultColor = comp.Translation;
        string? valueColor = defaultColor;

        if (total != int.MinValue && total > 0 && count >= total) valueColor = "CRGREEN";

        int labelWidth = DrawTextPart(hud, label.AsSpan(), cursor, defaultColor, comp.Alignment, fontDef, alpha, isNative);
        Vec2I valuePos = cursor;
        valuePos.X += labelWidth;

        m_fmtSpan.Clear();
        m_fmtSpan.Append(count);
        m_fmtSpan.Append('/');
        m_fmtSpan.Append(total);

        int valueWidth = DrawTextPart(hud, m_fmtSpan.AsSpan(), valuePos, valueColor, comp.Alignment, fontDef, alpha, isNative);

        float scale = isNative ? m_userScale : 1.0f;
        if (comp.Vertical) cursor.Y += (int)(fontHeight * (isNative ? m_userScale * 1.2f : 1.0f));
        else cursor.X += labelWidth + valueWidth + (int)(8 * scale);
    }

    private void DrawCoordinates(IHudRenderContext hud,
        StatusBarComponentDef comp,
        Vec2I pos,
        StatusBarHudFontDef? fontDef,
        int fontHeight,
        float alpha,
        StatusBarContext context,
        bool isNative)
    {
        Vec3D playerPos = context.Player.Position;
        m_coordPartsCache.Clear();
        m_coordPartsCache.Add(new CoordData("X: ", (int)playerPos.X, 0, 0));
        m_coordPartsCache.Add(new CoordData("Y: ", (int)playerPos.Y, 0, 0));
        m_coordPartsCache.Add(new CoordData("Z: ", (int)playerPos.Z, 0, 0));

        float scale = isNative ? m_userScale : 1.0f;
        int totalHorizontalWidth = 0;
        for (int i = 0; i < m_coordPartsCache.Count; i++)
        {
            CoordData data = m_coordPartsCache[i];
            int lw = MeasureSpan(hud, data.Label.AsSpan(), "CRGREEN", fontDef, alpha, isNative);
            m_fmtSpan.Clear();
            m_fmtSpan.Append(data.Value);
            int vw = MeasureSpan(hud, m_fmtSpan.AsSpan(), comp.Translation, fontDef, alpha, isNative);
            m_coordPartsCache[i] = data with { LabelWidth = lw, ValWidth = vw };
            totalHorizontalWidth += lw + vw;
            if (i < m_coordPartsCache.Count - 1) totalHorizontalWidth += (int)(8 * scale);
        }

        Vec2I cursor = pos;
        if (!comp.Vertical)
        {
            if ((comp.Alignment & StatusBarAlignment.Right) != 0) cursor.X -= totalHorizontalWidth;
            else if ((comp.Alignment & StatusBarAlignment.HCenter) != 0) cursor.X -= totalHorizontalWidth / 2;
        }

        foreach (CoordData data in m_coordPartsCache)
            if (comp.Vertical)
            {
                int lineWidth = data.LabelWidth + data.ValWidth;
                int lineX = pos.X;
                if ((comp.Alignment & StatusBarAlignment.Right) != 0) lineX -= lineWidth;
                else if ((comp.Alignment & StatusBarAlignment.HCenter) != 0) lineX -= lineWidth / 2;

                _ = DrawTextPart(hud, data.Label.AsSpan(), (lineX, cursor.Y), "CRGREEN", StatusBarAlignment.Left, fontDef, alpha, isNative);

                m_fmtSpan.Clear();
                m_fmtSpan.Append(data.Value);

                _ = DrawTextPart(hud,
                    m_fmtSpan.AsSpan(),
                    (lineX + data.LabelWidth, cursor.Y),
                    comp.Translation,
                    StatusBarAlignment.Left,
                    fontDef,
                    alpha,
                    isNative);

                cursor.Y += (int)(fontHeight * (isNative ? m_userScale * 1.2f : 1.0f));
            }
            else
            {
                cursor.X += DrawTextPart(hud, data.Label.AsSpan(), cursor, "CRGREEN", StatusBarAlignment.Left, fontDef, alpha, isNative);

                m_fmtSpan.Clear();
                m_fmtSpan.Append(data.Value);

                cursor.X += DrawTextPart(hud,
                    m_fmtSpan.AsSpan(),
                    cursor,
                    comp.Translation,
                    StatusBarAlignment.Left,
                    fontDef,
                    alpha,
                    isNative);

                cursor.X += (int)(8 * scale);
            }
    }

    private int MeasureSpan(IHudRenderContext hud,
        ReadOnlySpan<char> t,
        string? trans,
        StatusBarHudFontDef? fontDef,
        float alpha,
        bool isNative)
    {
        if (fontDef != null)
            return DrawHudText(hud, t, fontDef, (0, 0), StatusBarAlignment.Left, trans, alpha, isNative, false);

        float textScale = isNative ? m_userScale : 1.0f;
        return (int)(hud.MeasureText(t, Constants.Fonts.Small, 8).Width * textScale);
    }

    private int DrawTextPart(IHudRenderContext hud,
        ReadOnlySpan<char> text,
        Vec2I position,
        string? translation,
        StatusBarAlignment alignment,
        StatusBarHudFontDef? fontDef,
        float alpha,
        bool isNative)
    {
        if (fontDef != null)
            return DrawHudText(hud, text, fontDef, position, alignment, translation, alpha, isNative);

        Align align = ConvertAlignment(alignment);
        Color? color = !string.IsNullOrEmpty(translation) && StandardTextColors.TryGetValue(translation, out Color c) ? c : null;
        float textScale = isNative ? m_userScale : 1.0f;

        hud.Text(text, Constants.Fonts.Small, 8, position, both: align, alpha: alpha, color: color, scale: textScale);
        return (int)(hud.MeasureText(text, Constants.Fonts.Small, 8).Width * textScale);
    }

    private void DrawChildren(IHudRenderContext hud,
        StatusBarBaseDef def,
        Vec2I pos,
        int containerHeight,
        StatusBarContext context,
        float widescreenOffset,
        Vec2I rootPos,
        bool isNative)
    {
        if (def.Children == null) return;

        foreach (StatusBarElementWrapper child in def.Children)
        {
            if (!EvaluateWrapperConditions(child, context))
                continue;

            DrawElementWrapper(hud, child, pos, containerHeight, context, widescreenOffset, rootPos, isNative);
        }
    }

    private ElementBounds MeasureElement(IHudRenderContext hud,
        StatusBarElementWrapper wrapper,
        StatusBarContext context,
        bool isNative,
        float sX = 1.0f,
        float sY = 1.0f)
    {
        float scaleX = isNative ? sX : 1.0f;
        float scaleY = isNative ? sY : 1.0f;

        if (wrapper.Graphic != null) return MeasureGraphic(wrapper.Graphic, scaleX, scaleY);
        if (wrapper.Number != null) return MeasureNumber(hud, wrapper.Number, context, false, isNative, scaleX, scaleY);
        if (wrapper.Percent != null) return MeasureNumber(hud, wrapper.Percent, context, true, isNative, scaleX, scaleY);
        if (wrapper.Face != null) return MeasureFace(hud, wrapper.Face, context, scaleX, scaleY);
        if (wrapper.String != null) return MeasureString(hud, wrapper.String, isNative, scaleX, scaleY);
        if (wrapper.Canvas != null) return MeasureBase(hud, wrapper.Canvas, context, isNative, scaleX, scaleY);
        if (wrapper.List != null) return MeasureList(hud, wrapper.List, context, isNative, scaleX, scaleY);
        if (wrapper.Component != null) return MeasureComponent(hud, wrapper.Component, isNative, scaleX, scaleY);
        if (wrapper.Carousel != null) return MeasureCarousel(hud, wrapper.Carousel, context, scaleX, scaleY);

        if (wrapper.Native == null) return ElementBounds.Empty;
        ElementBounds bounds = MeasureBase(hud, wrapper.Native, context, true, m_userScale, m_userScale * 1.2f);
        return !isNative && m_currentScale > 0
            ? new ElementBounds((int)(bounds.X1 / m_currentScale),
                (int)(bounds.Y1 / m_currentScale),
                (int)(bounds.X2 / m_currentScale),
                (int)(bounds.Y2 / m_currentScale))
            : bounds;
    }

    private ElementBounds MeasureBase(IHudRenderContext hud,
        StatusBarBaseDef def,
        StatusBarContext context,
        bool isNative,
        float sX,
        float sY)
    {
        if (def.Children == null || def.Children.Count == 0) return ElementBounds.Empty;

        ElementBounds contentBounds = ElementBounds.Empty;
        foreach (StatusBarElementWrapper t in def.Children)
        {
            if (!EvaluateWrapperConditions(t, context)) continue;
            contentBounds = ElementBounds.Union(contentBounds, MeasureElement(hud, t, context, isNative, sX, sY));
        }

        if (contentBounds.X1 == int.MaxValue) return ElementBounds.Empty;

        int posX = (int)(def.X * sX);
        int posY = (int)(def.Y * sY);
        ElementBounds containerPos = ApplyAlignment(new Vec2I(contentBounds.Width, contentBounds.Height), posX, posY, def.Alignment);

        return new ElementBounds(containerPos.X1 + contentBounds.X1,
            containerPos.Y1 + contentBounds.Y1,
            containerPos.X1 + contentBounds.X2,
            containerPos.Y1 + contentBounds.Y2);
    }

    private static ElementBounds MeasureGraphic(StatusBarGraphicDef def, float scaleX, float scaleY)
    {
        IRenderableTextureHandle? handle = def.Handle;
        if (handle == null) return ElementBounds.Empty;

        Vec2I size = new((int)(handle.Dimension.Width * scaleX), (int)(handle.Dimension.Height * scaleY));
        int posX = (int)(def.X * scaleX);
        int posY = (int)(def.Y * scaleY);

        Vec2I translatedOffset = RenderDimensions.TranslateDoomOffset(handle.Offset);

        if ((def.Alignment & StatusBarAlignment.IgnoreLeftOffset) == 0)
            posX += (int)(translatedOffset.X * scaleX);

        if ((def.Alignment & StatusBarAlignment.IgnoreTopOffset) == 0)
            posY += (int)(translatedOffset.Y * scaleY);

        return ApplyAlignment(size, posX, posY, def.Alignment);
    }

    private ElementBounds MeasureNumber(IHudRenderContext hud,
        StatusBarNumberDef def,
        StatusBarContext context,
        bool isPercent,
        bool isNative,
        float scaleX,
        float scaleY)
    {
        m_fmtSpan.Clear();
        m_fmtSpan.Append(ResolveNumberValue(context.Player, def.Type, def.Param));
        if (isPercent) m_fmtSpan.Append('%');

        int width = MeasureSpan(hud, m_fmtSpan.AsSpan(), null, null, 1.0f, isNative);
        int height = (int)((def.ResolvedHeight > 0 ? def.ResolvedHeight : 8) * scaleY);

        int posX = (int)(def.X * scaleX);
        int posY = (int)(def.Y * scaleY);
        return ApplyAlignment(new Vec2I(width, height), posX, posY, def.Alignment);
    }

    private static ElementBounds MeasureFace(IHudRenderContext hud,
        StatusBarFaceDef def,
        StatusBarContext context,
        float scaleX,
        float scaleY)
    {
        string p = context.Player.StatusBar.GetFacePatch();
        if (!hud.Textures.TryGet(p, out IRenderableTextureHandle? h)) return ElementBounds.Empty;

        Vec2I size = new((int)(h.Dimension.Width * scaleX), (int)(h.Dimension.Height * scaleY));
        int posX = (int)(def.X * scaleX);
        int posY = (int)(def.Y * scaleY);

        Vec2I translatedOffset = RenderDimensions.TranslateDoomOffset(h.Offset);

        if ((def.Alignment & StatusBarAlignment.IgnoreLeftOffset) == 0)
            posX += (int)(translatedOffset.X * scaleX);

        if ((def.Alignment & StatusBarAlignment.IgnoreTopOffset) == 0)
            posY += (int)(translatedOffset.Y * scaleY);

        return ApplyAlignment(size, posX, posY, def.Alignment);
    }

    private ElementBounds MeasureString(IHudRenderContext hud, StatusBarStringDef def, bool isNative, float scaleX, float scaleY)
    {
        ReadOnlySpan<char> text = GetStringValue(def);

        _ = m_hudFontLookup.TryGetValue(def.Font, out StatusBarHudFontDef? f);

        int width = MeasureSpan(hud, text, null, f, 1.0f, isNative);
        int height = (int)((def.ResolvedHeight > 0 ? def.ResolvedHeight : 8) * scaleY);

        int posX = (int)(def.X * scaleX);
        int posY = (int)(def.Y * scaleY);
        return ApplyAlignment(new Vec2I(width, height), posX, posY, def.Alignment);
    }

    private ElementBounds MeasureList(IHudRenderContext hud,
        StatusBarListDef def,
        StatusBarContext context,
        bool isNative,
        float scaleX,
        float scaleY)
    {
        if (def.Children == null) return ElementBounds.Empty;

        int totalW = 0;
        int totalH = 0;
        int count = 0;

        int spacing = (int)(def.Spacing * (isNative ? m_userScale : 1.0f));

        foreach (StatusBarElementWrapper child in def.Children)
        {
            if (!EvaluateWrapperConditions(child, context)) continue;

            ElementBounds size = MeasureElement(hud, child, context, isNative, scaleX, scaleY);
            if (def.Horizontal)
            {
                totalW += size.Width;
                if (count > 0) totalW += spacing;
                totalH = Math.Max(totalH, size.Height);
            }
            else
            {
                totalH += size.Height;
                if (count > 0) totalH += spacing;
                totalW = Math.Max(totalW, size.Width);
            }

            count++;
        }

        int posX = (int)(def.X * scaleX);
        int posY = (int)(def.Y * scaleY);

        return ApplyAlignment(new Vec2I(totalW, totalH), posX, posY, def.Alignment);
    }

    private ElementBounds MeasureComponent(IHudRenderContext hud, StatusBarComponentDef comp, bool isNative, float scaleX, float scaleY)
    {
        int fontHeight = 8;
        if (m_hudFontLookup.TryGetValue(comp.Font, out StatusBarHudFontDef? fontDef) &&
            StemToHelionFontMap.TryGetValue(fontDef.Stem, out string? helionFontName))
        {
            int h = hud.GetFontMaxHeight(helionFontName);
            if (h > 0) fontHeight = h;
        }

        Vec2I size = Vec2I.Zero;

        switch (comp.ComponentType)
        {
            case StatusBarComponentType.StatTotals:
                if (m_world.Config.Hud.ShowStats.Value)
                {
                    const string Dummy = "K: 000/000 I: 000/000 S: 000/000";
                    int w = MeasureSpan(hud, Dummy.AsSpan(), null, fontDef, 1.0f, isNative);
                    size = comp.Vertical ? (w / 3, (int)(fontHeight * 3 * scaleY)) : (w, (int)(fontHeight * scaleY));
                }

                break;

            case StatusBarComponentType.Time:
                size = (MeasureSpan(hud, "00:00:00".AsSpan(), null, fontDef, 1.0f, isNative), (int)(fontHeight * scaleY));
                break;

            case StatusBarComponentType.LevelTitle:
            case StatusBarComponentType.AnnounceLevelTitle:
                string title = m_world.MapInfo.GetDisplayNameWithPrefix(m_archiveCollection.Language);
                size = (MeasureSpan(hud, title.AsSpan(), null, fontDef, 1.0f, isNative), (int)(fontHeight * scaleY));
                break;

            case StatusBarComponentType.FpsCounter:
                if (m_world.Config.Hud.ShowFPS.Value)
                    size = (MeasureSpan(hud, "000".AsSpan(), null, fontDef, 1.0f, isNative), (int)(fontHeight * scaleY));
                break;

            case StatusBarComponentType.Message:
                size = Vec2I.Zero;
                break;

            case StatusBarComponentType.Unknown:
            case StatusBarComponentType.Coordinates:
            case StatusBarComponentType.Speedometer:
            case StatusBarComponentType.RenderStats:
            case StatusBarComponentType.CommandHistory:
            case StatusBarComponentType.Chat:
            default:
                break;
        }

        int posX = (int)(comp.X * scaleX);
        int posY = (int)(comp.Y * scaleY);

        return ApplyAlignment(size, posX, posY, comp.Alignment);
    }

    private static ElementBounds MeasureCarousel(IHudRenderContext hud,
        StatusBarCarouselDef carousel,
        StatusBarContext context,
        float scaleX,
        float scaleY)
    {
        Vec2I size = Vec2I.Zero;
        if (context.Player.Weapon != null)
        {
            string icon = context.Player.Weapon.Definition.Properties.Inventory.Icon;
            if (!string.IsNullOrEmpty(icon) && hud.Textures.TryGet(icon, out IRenderableTextureHandle? handle))
                size = new Vec2I((int)(handle.Dimension.Width * scaleX), (int)(handle.Dimension.Height * scaleY));
        }

        int posX = (int)(carousel.X * scaleX);
        int posY = (int)(carousel.Y * scaleY);

        return ApplyAlignment(size, posX, posY, carousel.Alignment);
    }

    private static ElementBounds ApplyAlignment(Vec2I size, int posX, int posY, StatusBarAlignment alignment)
    {
        int x1 = posX;
        int y1 = posY;

        if ((alignment & StatusBarAlignment.HCenter) != 0) x1 -= size.X / 2;
        else if ((alignment & StatusBarAlignment.Right) != 0) x1 -= size.X;

        if ((alignment & StatusBarAlignment.VCenter) != 0) y1 -= size.Y / 2;
        else if ((alignment & StatusBarAlignment.Bottom) != 0) y1 -= size.Y;

        return new ElementBounds(x1, y1, x1 + size.X, y1 + size.Y);
    }

    private static string ResolvePatchName(string patch)
    {
        if (string.IsNullOrEmpty(patch) || (!patch.Contains('/') && !patch.Contains('.')))
            return patch;

        int lastSlash = patch.LastIndexOf('/') + 1;
        int lastDot = patch.LastIndexOf('.');

        if (lastDot < lastSlash)
            lastDot = patch.Length;

        return patch[lastSlash..lastDot];
    }

    private ReadOnlySpan<char> GetStringValue(StatusBarStringDef def)
    {
        return def.Type switch
        {
            0 => def.Data.AsSpan(),
            1 => m_world.MapInfo.GetDisplayNameWithPrefix(m_archiveCollection.Language).AsSpan(),
            2 => m_world.MapInfo.Label.AsSpan(),
            3 => m_world.MapInfo.Author.AsSpan(),
            _ => []
        };
    }

    private static bool EvaluateWrapperConditions(StatusBarElementWrapper wrapper, StatusBarContext context)
    {
        return wrapper switch
        {
            { Canvas: not null } => StatusBarConditionResolver.Evaluate(context, wrapper.Canvas.Conditions),
            { Graphic: not null } => StatusBarConditionResolver.Evaluate(context, wrapper.Graphic.Conditions),
            { Number: not null } => StatusBarConditionResolver.Evaluate(context, wrapper.Number.Conditions),
            { Percent: not null } => StatusBarConditionResolver.Evaluate(context, wrapper.Percent.Conditions),
            { Face: not null } => StatusBarConditionResolver.Evaluate(context, wrapper.Face.Conditions),
            { Animation: not null } => StatusBarConditionResolver.Evaluate(context, wrapper.Animation.Conditions),
            { Component: not null } => StatusBarConditionResolver.Evaluate(context, wrapper.Component.Conditions),
            { Carousel: not null } => StatusBarConditionResolver.Evaluate(context, wrapper.Carousel.Conditions),
            { List: not null } => StatusBarConditionResolver.Evaluate(context, wrapper.List.Conditions),
            { String: not null } => StatusBarConditionResolver.Evaluate(context, wrapper.String.Conditions),
            _ => true
        };
    }

    private static Vec2I ResolvePosition(StatusBarBaseDef def, Vec2I parentPos, bool isNative, float scale)
    {
        Vec2I pos = parentPos;

        int x = (int)(def.X * scale);
        int y = (int)(def.Y * (isNative ? scale * 1.2f : scale));

        pos.X += x;
        pos.Y += y;

        return pos;
    }

    private static bool ResolveGlyph(IHudRenderContext hud, string patch, out int width, out int height)
    {
        string p = ResolvePatchName(patch);
        if (hud.Textures.TryGet(p, out IRenderableTextureHandle? handle) || hud.Textures.TryGet(p, out handle, ResourceNamespace.Sprites))
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

        Dictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> lookup = HudFontPatchCache.GetAlternateLookup<ReadOnlySpan<char>>();
        if (lookup.TryGetValue(m_lookupKeySpan.AsSpan(), out string? cached)) return cached;

        string result = font.Stem + ((int)c).ToString("D3", CultureInfo.InvariantCulture);
        HudFontPatchCache[font.Stem + c] = result;
        return result;
    }

    private string GetFontPatch(IHudRenderContext hud, StatusBarNumberFontDef font, char c)
    {
        m_lookupKeySpan.Clear();
        m_lookupKeySpan.Append(font.Stem);
        m_lookupKeySpan.Append(c);
        Dictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> lookup = FontPatchCache.GetAlternateLookup<ReadOnlySpan<char>>();
        if (lookup.TryGetValue(m_lookupKeySpan.AsSpan(), out string? cached)) return cached;

        string result = c switch
        {
            '-' => hud.Textures.HasImage(font.Stem + "MINUS") ? font.Stem + "MINUS" : font.Stem + "-",

            '%' => hud.Textures.HasImage(font.Stem + "PRCNT") ? font.Stem + "PRCNT" :
                hud.Textures.HasImage(font.Stem + "PRCN") ? font.Stem + "PRCN" :
                hud.Textures.HasImage(font.Stem + "PERCENT") ? font.Stem + "PERCENT" : font.Stem + "%",

            _ when char.IsDigit(c) => hud.Textures.HasImage(font.Stem + "NUM" + c) ? font.Stem + "NUM" + c : font.Stem + c,

            _ => font.Stem + c
        };

        FontPatchCache[font.Stem + c] = result;
        return result;
    }

    private int ResolveNumberValue(Player player, StatusBarNumberType type, int param)
    {
        EntityDefinitionComposer composer = m_archiveCollection.EntityDefinitionComposer;
        LevelStats stats = m_world.LevelStats;

        switch (type)
        {
            case StatusBarNumberType.Health: return Math.Max(0, player.Health);
            case StatusBarNumberType.Armor: return player.Armor;
            case StatusBarNumberType.Frags: return 0;
            case StatusBarNumberType.Ammo:
                return StatusBarConditionResolver.TryGetId24AmmoType(composer, param, out EntityDefinition? ammoDef)
                    ? player.Inventory.Amount(ammoDef.Name)
                    : 0;

            case StatusBarNumberType.AmmoSelected:
                return player.AnimationWeapon?.Definition.Properties.Weapons.AmmoType is { } a && !string.IsNullOrEmpty(a)
                    ? player.Inventory.Amount(a)
                    : 0;

            case StatusBarNumberType.MaxAmmo:
                return StatusBarConditionResolver.TryGetId24AmmoType(composer, param, out EntityDefinition? maxAmmoDef)
                    ? GetMaxAmount(player, maxAmmoDef.Name)
                    : 0;

            case StatusBarNumberType.AmmoWeapon:
                return m_archiveCollection.Definitions.DehackedDefinition is { } deh &&
                       deh.TryGetId24PickupType(composer, param, out EntityDefinition? wDef)
                    ? player.Inventory.Amount(wDef.Properties.Weapons.AmmoType)
                    : 0;

            case StatusBarNumberType.MaxAmmoWeapon:
                return m_archiveCollection.Definitions.DehackedDefinition is { } dehM &&
                       dehM.TryGetId24PickupType(composer, param, out EntityDefinition? mwDef)
                    ? GetMaxAmount(player, mwDef.Properties.Weapons.AmmoType)
                    : 0;

            case StatusBarNumberType.Kills: return stats.KillCount;
            case StatusBarNumberType.Items: return stats.ItemCount;
            case StatusBarNumberType.Secrets: return stats.SecretCount;

            case StatusBarNumberType.KillsPercent:
                return stats.TotalMonsters > 0 ? stats.KillCount * 100 / stats.TotalMonsters : 100;
            case StatusBarNumberType.ItemsPercent:
                return stats.TotalItems > 0 ? stats.ItemCount * 100 / stats.TotalItems : 100;
            case StatusBarNumberType.SecretsPercent:
                return stats.TotalSecrets > 0 ? stats.SecretCount * 100 / stats.TotalSecrets : 100;

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

                return pt switch
                {
                    PowerupType.None => 0,
                    PowerupType.Strength or PowerupType.ComputerAreaMap => player.Inventory.IsPowerupActive(pt) ? 1 : 0,
                    _ => (player.Inventory.GetPowerup(pt)?.Ticks ?? 0) / (int)Constants.TicksPerSecond
                };

            default: return 0;
        }
    }

    private static string GetSpeedometerText(Player player)
    {
        _ = player;
        return string.Empty;
    }

    private int GetMaxAmount(Player player, string name)
    {
        EntityDefinition? def = m_archiveCollection.EntityDefinitionComposer.GetByName(name);
        if (def == null) return 0;
        EntityDefinition baseDef = Inventory.GetBaseInventoryDefinition(def) ?? def;
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

        return bottom ? hCenter ? Align.BottomMiddle : right ? Align.BottomRight : Align.BottomLeft :
            vCenter ? hCenter ? Align.Center : right ? Align.MiddleRight : Align.MiddleLeft :
            hCenter ? Align.TopMiddle :
            right ? Align.TopRight : Align.TopLeft;
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

        int cw = cropDef.Width > 0 ? cropDef.Width : handle.Dimension.Width - cx;
        int ch = cropDef.Height > 0 ? cropDef.Height : handle.Dimension.Height - cy;

        return new ImageBox2I(cx, cy, cx + cw, cy + ch);
    }

    private readonly record struct RenderGlyph(string Patch, int Width, int Offset, IRenderableTextureHandle? Handle = null);

    private readonly record struct CoordData(string Label, int Value, int LabelWidth, int ValWidth);

    private readonly record struct ElementBounds(int X1, int Y1, int X2, int Y2)
    {
        public int Width => X2 - X1;
        public int Height => Y2 - Y1;
        public static ElementBounds Empty => new(int.MaxValue, int.MaxValue, int.MinValue, int.MinValue);

        public static ElementBounds Union(ElementBounds a, ElementBounds b)
        {
            return new ElementBounds(Math.Min(a.X1, b.X1), Math.Min(a.Y1, b.Y1), Math.Max(a.X2, b.X2), Math.Max(a.Y2, b.Y2));
        }
    }
}