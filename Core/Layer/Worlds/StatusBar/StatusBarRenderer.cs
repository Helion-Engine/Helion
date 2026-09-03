using Helion.Dehacked;
using Helion.Geometry;
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
using Helion.Util.Assertion;
using Helion.Util.Configs.Components;
using Helion.Util.Container;
using Helion.World.Entities.Definition;
using Helion.World.Entities.Inventories;
using Helion.World.Entities.Inventories.Powerups;
using Helion.World.Entities.Players;
using Helion.World.Stats;
using Helion.World.StatusBar;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Runtime.CompilerServices;

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
    private static readonly Dictionary<string, int> Type0WidthCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, int> HudType1WidthCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, int> HudType0WidthCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> FontPatchCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> HudFontPatchCache = new(StringComparer.OrdinalIgnoreCase);
    private static readonly Dictionary<string, string> PatchNameCache = new(StringComparer.OrdinalIgnoreCase);

    private static readonly Dictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> FontPatchCacheBySpan = FontPatchCache.GetAlternateLookup<ReadOnlySpan<char>>();
    private static readonly Dictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> HudFontPatchCacheBySpan = HudFontPatchCache.GetAlternateLookup<ReadOnlySpan<char>>();
    private static readonly Dictionary<string, string>.AlternateLookup<ReadOnlySpan<char>> PatchNameCacheBySpan = PatchNameCache.GetAlternateLookup<ReadOnlySpan<char>>();

    // Mapping SBARDEF stems to Helion Internal Fonts to enable grayscale tinting and better rendering
    private static readonly Dictionary<string, string> StemToHelionFontMap = new(StringComparer.OrdinalIgnoreCase)
    {
        { "STCFN", Constants.Fonts.Small },
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
    private readonly ConfigHud m_config;
    private readonly List<CoordData> m_coordPartsCache = new(16);
    private readonly SpanString m_fmtSpan = new();
    private readonly Dictionary<string, StatusBarNumberFontDef> m_fontNumberLookup = [];
    private readonly List<RenderGlyph> m_glyphCache = new(256);
    private readonly Dictionary<string, StatusBarHudFontDef> m_hudFontLookup = [];
    private readonly SpanString m_lookupKeySpan = new(128);
    private readonly LookupArray<EntityDefinition> m_id24PickupTypeLookup = new(Constants.Id24PickupLookup.Length);

    private readonly Func<IHudRenderContext, StatusBarHudFontDef, char, string> m_getHudFontPatch;
    private readonly Func<IHudRenderContext, StatusBarNumberFontDef, char, string> m_getFontNumberPatch;

    private StatusBarContext m_ctx;
    private float m_currentScale;
    private float m_hOffset;
    private Vec2F m_scale = Vec2F.One;

    private readonly HashSet<StatusBarLayoutDef> m_resolvedLayouts = new();
    private Dimension m_lastWindowDimension;
    private float m_lastUserScale = -1f;

    private bool m_invalidateBounds;
    private float m_userScale;
    private float m_vOffset;
    private float m_alpha;

    public StatusBarRenderer(ArchiveCollection archiveCollection)
    {
        m_archiveCollection = archiveCollection;
        m_config = archiveCollection.Config.Hud;
        var sbarDef = archiveCollection.Definitions.StatusBarDefinition;

        foreach (StatusBarNumberFontDef f in sbarDef.NumberFonts)
            m_fontNumberLookup[f.Name] = f;

        foreach (StatusBarHudFontDef f in sbarDef.HudFonts)
            m_hudFontLookup[f.Name] = f;

        var composer = m_archiveCollection.EntityDefinitionComposer;
        var enumCount = (int)Enum.GetValues<Id24PickupType>().Max();
        for (int i = 0; i < enumCount; i++)
        {
            Assert.Precondition(i < Constants.Id24PickupLookup.Length, "Id24PickupType index out of bounds of Id24PickupLookup");
            if (i >= Constants.Id24PickupLookup.Length)
                break;

            var def = composer.GetByName(Constants.Id24PickupLookup[i]);
            if (def != null)
                m_id24PickupTypeLookup.Set(i, def);
        }

        m_getHudFontPatch = GetHudFontPatch;
        m_getFontNumberPatch = GetFontPatch;
    }

    public static StatusBarCoverage GetCoverage(StatusBarLayoutDef layout)
    {
        if (layout.CoverageSet)
            return layout.Coverage;

        layout.CoverageSet = true;
        layout.Coverage = layout.Children.Length == 0 ? StatusBarCoverage.None : ScanChildren(layout.Children);
        return layout.Coverage;
    }

    private static StatusBarCoverage ScanChildren(StatusBarElementWrapper[] children)
    {
        StatusBarCoverage mask = StatusBarCoverage.None;
        foreach (StatusBarElementWrapper child in children)
        {
            child.HasConditions = child.CheckHasConditions();

            if (child.Component != null)
            {
                switch (child.Component.ComponentType)
                {
                    case StatusBarComponentType.StatTotals:
                        mask |= StatusBarCoverage.Stats;
                        break;
                    case StatusBarComponentType.Time:
                        mask |= StatusBarCoverage.Time;
                        break;
                    case StatusBarComponentType.Message:
                        mask |= StatusBarCoverage.Messages;
                        break;
                    case StatusBarComponentType.LevelTitle:
                        mask |= StatusBarCoverage.MapTitle;
                        break;
                    case StatusBarComponentType.FpsCounter:
                        mask |= StatusBarCoverage.FPS;
                        break;
                    case StatusBarComponentType.Unknown:
                    case StatusBarComponentType.Coordinates:
                    case StatusBarComponentType.Speedometer:
                    case StatusBarComponentType.AnnounceLevelTitle:
                    case StatusBarComponentType.RenderStats:
                    case StatusBarComponentType.CommandHistory:
                    case StatusBarComponentType.Chat:
                    default:
                        break;
                }

                if (child.Component.Children != null)
                    mask |= ScanChildren(child.Component.Children);
            }

            if (child.Canvas?.Children != null)
                mask |= ScanChildren(child.Canvas.Children);
            if (child.Native?.Children != null)
                mask |= ScanChildren(child.Native.Children);
            if (child.List?.Children != null)
                mask |= ScanChildren(child.List.Children);
            if (child.Graphic?.Children != null)
                mask |= ScanChildren(child.Graphic.Children);
            if (child.Face?.Children != null)
                mask |= ScanChildren(child.Face.Children);
            if (child.FaceBackground?.Children != null)
                mask |= ScanChildren(child.FaceBackground.Children);
            if (child.Animation?.Children != null)
                mask |= ScanChildren(child.Animation.Children);
            if (child.Carousel?.Children != null)
                mask |= ScanChildren(child.Carousel.Children);
            if (child.Number?.Children != null)
                mask |= ScanChildren(child.Number.Children);
            if (child.Percent?.Children != null)
                mask |= ScanChildren(child.Percent.Children);
            if (child.String?.Children != null)
                mask |= ScanChildren(child.String.Children);
        }

        return mask;
    }

    public void Draw(IHudRenderContext hud, StatusBarLayoutDef layout, StatusBarContext context, int hudNativePaddingX)
    {
        m_ctx = context;
        StatusBarConditionResolver.ShouldEvaluate = context.HasTicks;

        float currentUserScale = (float)m_config.Scale.Value;
        
        bool scaleChanged = Math.Abs(m_lastUserScale - currentUserScale) > 0.001f;

        if (m_lastWindowDimension != hud.WindowDimension || scaleChanged)
        {
            m_invalidateBounds = true;
            m_lastWindowDimension = hud.WindowDimension;
            m_lastUserScale = currentUserScale;
        }

        if (!m_resolvedLayouts.Contains(layout))
        {
            EnsureTexturesResolved(hud, layout);
            m_resolvedLayouts.Add(layout);
        }

        const int Width = 320;
        const int Height = 200;

        hud.PushVirtualDimension((Width, Height), ResolutionScale.Center, Constants.DoomVirtualAspectRatio);

        int yOffset = layout.FullscreenRender ? 0 : 200 - layout.Height;
        Vec2I rootPos = (0, yOffset);

        float windowWidth = hud.WindowDimension.Width;
        float windowHeight = hud.WindowDimension.Height;

        float scaleX = windowWidth / 320f;
        float scaleY = windowHeight / (200f * 1.2f);
        m_currentScale = Math.Min(scaleX, scaleY);
        m_userScale = (float)m_config.Scale.Value;
        m_alpha = HudView.GetStatusBarHeight(layout) > 0 ? 1 : 1 - (float)m_config.Transparency.Value;
        m_scale = Vec2F.One;

        m_hOffset = (windowWidth / m_currentScale - 320f) / 2f;
        m_vOffset = (windowHeight / m_currentScale - 200f * 1.2f) / 2f;
        float widescreenOffset = m_hOffset - hudNativePaddingX / m_currentScale;
        
        if (!layout.FullscreenRender)
        {
            string fillFlat = layout.FillFlat ?? m_ctx.World.GameInfo.BorderFlat;

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
            DrawElementWrapper(hud, child, rootPos, layout.Height, widescreenOffset, rootPos);

        hud.PopVirtualDimension();
        m_invalidateBounds = false;
        m_ctx = default;
    }

    private void EnsureTexturesResolved(IHudRenderContext hud, StatusBarLayoutDef layout)
    {
        foreach (StatusBarElementWrapper t in layout.Children)
            ResolveElementTextures(hud, t);
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
        {
            for (int i = 0; i < wrapper.Animation.Frames.Length; i++)
            {
                ref var frame = ref wrapper.Animation.Frames[i];
                frame.ResolvedPatchName = ResolvePatchName(frame.Lump);

                if (hud.Textures.TryGet(frame.ResolvedPatchName, out IRenderableTextureHandle? handle) ||
                    hud.Textures.TryGet(frame.ResolvedPatchName, out handle, ResourceNamespace.Sprites))
                    frame.Handle = handle;
            }
        }

        if (wrapper.String != null)
        {
            if (m_hudFontLookup.TryGetValue(wrapper.String.Font, out StatusBarHudFontDef? f))
            {
                string zeroPatch = GetHudFontPatch(hud, f, '0');
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

            face!.ResolvedHeight = hud.Textures.TryGet("STFST01", out IRenderableTextureHandle? h) ||
                                   hud.Textures.TryGet("STFST01", out h, ResourceNamespace.Sprites)
                ? h.Dimension.Height
                : 32;
        }

        if (wrapper.FaceBackground != null)
            if (hud.Textures.TryGet("STFB0", out IRenderableTextureHandle? handle) ||
                hud.Textures.TryGet("STFB0", out handle, ResourceNamespace.Sprites))
                wrapper.FaceBackground.Handle = handle;

        StatusBarBaseDef? baseDef = null;
        if (wrapper.Canvas != null)
            baseDef = wrapper.Canvas;
        else if (wrapper.List != null)
            baseDef = wrapper.List;
        else if (wrapper.Native != null)
            baseDef = wrapper.Native;
        else if (wrapper.Graphic != null)
            baseDef = wrapper.Graphic;
        else if (wrapper.Face != null)
            baseDef = wrapper.Face;
        else if (wrapper.Animation != null)
            baseDef = wrapper.Animation;
        else if (wrapper.Carousel != null)
            baseDef = wrapper.Carousel;
        else if (wrapper.Number != null)
            baseDef = wrapper.Number;
        else if (wrapper.Percent != null)
            baseDef = wrapper.Percent;
        else if (wrapper.String != null)
            baseDef = wrapper.String;
        else if (wrapper.Component != null)
            baseDef = wrapper.Component;
        else if (wrapper.FaceBackground != null)
            baseDef = wrapper.FaceBackground;

        if (baseDef?.Children == null)
            return;

        foreach (StatusBarElementWrapper t in baseDef.Children)
            ResolveElementTextures(hud, t);
    }

    private void DrawElementWrapper(IHudRenderContext hud,
        StatusBarElementWrapper wrapper,
        Vec2I parentPos,
        int containerHeight,
        float widescreenOffset,
        Vec2I rootPos)
    {
        Vec2I effectiveParentPos = parentPos;
        bool isHudWidget = wrapper.Component != null || wrapper.Carousel != null;
        bool isStandardMode = m_scale.IsApprox(Vec2F.One);

        if (isHudWidget && parentPos == rootPos && isStandardMode)
            effectiveParentPos = (0, 0);

        if (wrapper.Canvas != null)
            DrawBase(hud, wrapper.Canvas, parentPos, containerHeight, widescreenOffset, rootPos);
        else if (wrapper.Native != null)
            DrawNative(hud, wrapper.Native, parentPos, containerHeight, widescreenOffset);
        else if (wrapper.List != null)
            DrawList(hud, wrapper.List, parentPos, containerHeight, widescreenOffset, rootPos);
        else if (wrapper.Graphic != null)
            DrawGraphic(hud, wrapper.Graphic, parentPos, containerHeight, widescreenOffset);
        else if (wrapper.Number != null)
            DrawNumber(hud, wrapper.Number, parentPos, containerHeight, false, widescreenOffset);
        else if (wrapper.Percent != null)
            DrawNumber(hud, wrapper.Percent, parentPos, containerHeight, true, widescreenOffset);
        else if (wrapper.String != null)
            DrawString(hud, wrapper.String, parentPos, containerHeight, widescreenOffset);
        else if (wrapper.Face != null)
            DrawFace(hud, wrapper.Face, parentPos, containerHeight, widescreenOffset);
        else if (wrapper.FaceBackground != null)
            DrawFaceBackground(hud, wrapper.FaceBackground, parentPos, containerHeight, widescreenOffset);
        else if (wrapper.Animation != null)
            DrawAnimation(hud, wrapper.Animation, parentPos, containerHeight, widescreenOffset);
        else if (wrapper.Component != null)
            DrawComponent(hud, wrapper.Component, effectiveParentPos, containerHeight, widescreenOffset, rootPos);
        else if (wrapper.Carousel != null)
            DrawCarousel(hud, wrapper.Carousel, effectiveParentPos, containerHeight, widescreenOffset, rootPos);
    }

    private void DrawBase(IHudRenderContext hud,
        StatusBarCanvasDef def,
        Vec2I parentPos,
        int containerHeight,
        float widescreenOffset,
        Vec2I rootPos)
    {
        if (!StatusBarConditionResolver.Evaluate(m_ctx, def))
            return;

        Vec2I currentPos = ResolvePosition(def, parentPos, widescreenOffset);

        bool hAlign = (def.Alignment & (StatusBarAlignment.Right | StatusBarAlignment.HCenter)) != 0;
        bool vAlign = (def.Alignment & (StatusBarAlignment.Bottom | StatusBarAlignment.VCenter)) != 0;

        if (hAlign || vAlign)
        {
            var bounds = ElementBounds.Empty;
            if (def.Children != null)
            {
                foreach (StatusBarElementWrapper child in def.Children)
                {
                    ElementBounds.Union(ref bounds, MeasureElement(hud, child, m_scale.X, m_scale.Y));
                }
            }

            if (bounds.X1 != int.MaxValue)
            {
                if ((def.Alignment & StatusBarAlignment.HCenter) != 0)
                    currentPos.X -= (bounds.X1 + bounds.X2) / 2;
                else if ((def.Alignment & StatusBarAlignment.Right) != 0)
                    currentPos.X -= bounds.X2;

                if ((def.Alignment & StatusBarAlignment.VCenter) != 0)
                    currentPos.Y -= (bounds.Y1 + bounds.Y2) / 2;
                else if ((def.Alignment & StatusBarAlignment.Bottom) != 0)
                    currentPos.Y -= bounds.Y2;
            }
        }
        
        DrawChildren(hud, def, currentPos, containerHeight, widescreenOffset, rootPos);
    }

    private void DrawList(IHudRenderContext hud,
        StatusBarListDef def,
        Vec2I parentPos,
        int containerHeight,
        float widescreenOffset,
        Vec2I rootPos)
    {
        if (!StatusBarConditionResolver.Evaluate(m_ctx, def) || def.Children == null)
            return;

        int totalWidth = 0;
        int totalHeight = 0;
        int spacing = (int)(def.Spacing * m_scale.X);

        for (int i = 0; i < def.Children.Length; i++)
        {
            StatusBarElementWrapper child = def.Children[i];
            var bounds = MeasureElement(hud, child, m_scale.X, m_scale.Y);
            child.Size = new Vec2I(bounds.Width, bounds.Height);

            if (def.Horizontal)
            {
                totalWidth += child.Size.X;
                if (i > 0) totalWidth += spacing;
                totalHeight = Math.Max(totalHeight, child.Size.Y);
            }
            else
            {
                totalHeight += child.Size.Y;
                if (i > 0) totalHeight += spacing;
                totalWidth = Math.Max(totalWidth, child.Size.X);
            }
        }

        var listPos = ResolvePosition(def, parentPos, widescreenOffset);

        if ((def.Alignment & StatusBarAlignment.HCenter) != 0)
            listPos.X -= totalWidth / 2;
        else if ((def.Alignment & StatusBarAlignment.Right) != 0)
            listPos.X -= totalWidth;

        if ((def.Alignment & StatusBarAlignment.Bottom) != 0)
            listPos.Y -= totalHeight;
        else if ((def.Alignment & StatusBarAlignment.VCenter) != 0)
            listPos.Y -= totalHeight / 2;

        var cursor = listPos;
        foreach (StatusBarElementWrapper child in def.Children)
        {
            if (!EvaluateWrapperConditions(child))
                continue;

            var childPos = cursor;

            if (def.Horizontal)
            {
                if ((def.Alignment & StatusBarAlignment.Bottom) != 0)
                    childPos.Y += totalHeight - child.Size.Y;
                else if ((def.Alignment & StatusBarAlignment.VCenter) != 0)
                    childPos.Y += (totalHeight - child.Size.Y) / 2;

                DrawElementWrapper(hud, child, childPos, containerHeight, 0, rootPos);
                cursor.X += child.Size.X + spacing;
            }
            else
            {
                if ((def.Alignment & StatusBarAlignment.Right) != 0)
                    childPos.X += totalWidth - child.Size.X;
                else if ((def.Alignment & StatusBarAlignment.HCenter) != 0)
                    childPos.X += (totalWidth - child.Size.X) / 2;

                DrawElementWrapper(hud, child, childPos, containerHeight, 0, rootPos);
                cursor.Y += child.Size.Y + spacing;
            }
        }
    }

    private void DrawNative(IHudRenderContext hud,
        StatusBarNativeDef def,
        Vec2I parentPos,
        int containerHeight,
        float widescreenOffset)
    {
        if (!StatusBarConditionResolver.Evaluate(m_ctx, def))
            return;

        Vec2I vPos = ResolvePosition(def, parentPos, widescreenOffset);

        float nScaleX = m_userScale;
        float nScaleY = m_userScale * 1.2f;

        ElementBounds bounds = ElementBounds.Empty;
        if (def.Children != null)
        {
            foreach (StatusBarElementWrapper child in def.Children)
            {
                if (!EvaluateWrapperConditions(child))
                    continue;

                ElementBounds.Union(ref bounds, MeasureElement(hud, child, nScaleX, nScaleY));
            }
        }

        if (bounds.X1 == int.MaxValue) 
            bounds = new ElementBounds(0, 0, 0, 0);

        int pivotX = (bounds.X1 + bounds.X2) / 2;
        int pivotY = (bounds.Y1 + bounds.Y2) / 2;

        int nativeX = (int)Math.Floor((vPos.X + m_hOffset) * m_currentScale);
        int nativeY = (int)Math.Floor((vPos.Y * 1.2f + m_vOffset) * m_currentScale);

        if ((def.Alignment & StatusBarAlignment.HCenter) != 0)
            nativeX -= pivotX;
        else if ((def.Alignment & StatusBarAlignment.Right) != 0)
            nativeX -= bounds.X2;
        else
            nativeX -= bounds.X1;

        if ((def.Alignment & StatusBarAlignment.VCenter) != 0)
            nativeY -= pivotY;
        else if ((def.Alignment & StatusBarAlignment.Bottom) != 0)
            nativeY -= bounds.Y2;
        else
            nativeY -= bounds.Y1;

        Vec2I nativeRoot = new(nativeX, nativeY);

        hud.PopVirtualDimension();

        Vec2F prevScale = m_scale;
        m_scale = (nScaleX, nScaleY);

        DrawChildren(hud, def, nativeRoot, containerHeight, 0, nativeRoot);

        m_scale = prevScale;

        hud.PushVirtualDimension((320, 200), ResolutionScale.Center, Constants.DoomVirtualAspectRatio);
    }

    private void DrawGraphic(IHudRenderContext hud,
        StatusBarGraphicDef graphic,
        Vec2I parentPos,
        int containerHeight,
        float widescreenOffset)
    {
        if (!StatusBarConditionResolver.Evaluate(m_ctx, graphic))
            return;

        Vec2I currentPos = ResolvePosition(graphic, parentPos, widescreenOffset);

        if (graphic.Handle != null || !string.IsNullOrEmpty(graphic.Patch))
        {
            Align align = ConvertAlignment(graphic.Alignment);
            if (graphic.MidOffset != 0) currentPos.X += (int)(graphic.MidOffset * m_scale.X);

            float alpha = graphic.Translucency ? 0.5f : 1.0f;

            DrawSBarTexture(hud,
                graphic.ResolvedPatchName ?? graphic.Patch,
                graphic.Handle,
                currentPos,
                align,
                graphic.Alignment,
                graphic.Translation,
                alpha,
                graphic.Crop);
        }

        DrawChildren(hud, graphic, currentPos, containerHeight, 0, Vec2I.Zero);
    }

    private void DrawFace(IHudRenderContext hud,
        StatusBarFaceDef face,
        Vec2I parentPos,
        int containerHeight,
        float widescreenOffset)
    {
        if (!StatusBarConditionResolver.Evaluate(m_ctx, face))
            return;

        Vec2I currentPos = ResolvePosition(face, parentPos, widescreenOffset);
        string patch = m_ctx.Player.StatusBar.GetFacePatch();

        if (!string.IsNullOrEmpty(patch))
        {
            Align align = ConvertAlignment(face.Alignment);
            float alpha = face.Translucency ? 0.5f : 1.0f;

            DrawSBarTexture(hud, patch, null, currentPos, align, face.Alignment, face.Translation, alpha, face.Crop);
        }

        DrawChildren(hud, face, currentPos, containerHeight, 0, Vec2I.Zero);
    }

    private void DrawFaceBackground(IHudRenderContext hud,
        StatusBarFaceDef faceBg,
        Vec2I parentPos,
        int containerHeight,
        float widescreenOffset)
    {
        if (!StatusBarConditionResolver.Evaluate(m_ctx, faceBg))
            return;

        Vec2I currentPos = ResolvePosition(faceBg, parentPos, widescreenOffset);

        if (faceBg.Handle != null)
        {
            Align align = ConvertAlignment(faceBg.Alignment);
            float alpha = faceBg.Translucency ? 0.5f : 1.0f;

            DrawSBarTexture(hud, "STFB0", faceBg.Handle, currentPos, align, faceBg.Alignment, faceBg.Translation, alpha);
        }

        DrawChildren(hud, faceBg, currentPos, containerHeight, 0, Vec2I.Zero);
    }

    private void DrawAnimation(IHudRenderContext hud,
        StatusBarAnimationDef anim,
        Vec2I parentPos,
        int containerHeight,
        float widescreenOffset)
    {
        if (!StatusBarConditionResolver.Evaluate(m_ctx, anim))
            return;

        Vec2I currentPos = ResolvePosition(anim, parentPos, widescreenOffset);

        if (anim.Frames.Length > 0)
        {
            double totalDuration = 0;
            foreach (StatusBarFrameDef frameDef in anim.Frames)
                totalDuration += frameDef.Duration;

            if (totalDuration > 0)
            {
                double timePerLoop = totalDuration * Constants.TicksPerSecond;
                long currentTick = m_ctx.World.LevelTime;
                double animTime = currentTick % timePerLoop;

                ref var frame = ref anim.Frames[0];
                double timeAccumulator = 0;

                for (int i = 0; i < anim.Frames.Length; i++)
                {
                    ref var f = ref anim.Frames[i];
                    timeAccumulator += f.Duration * Constants.TicksPerSecond;
                    if (!(animTime < timeAccumulator))
                        continue;
                    frame = ref f;
                    break;
                }

                Align align = ConvertAlignment(anim.Alignment);
                DrawSBarTexture(hud,
                    frame.ResolvedPatchName ?? frame.Lump,
                    frame.Handle,
                    currentPos,
                    align,
                    anim.Alignment,
                    anim.Translation);
            }
        }

        DrawChildren(hud, anim, currentPos, containerHeight, 0, (0, 0));
    }

    private void DrawString(IHudRenderContext hud,
        StatusBarStringDef def,
        Vec2I parentPos,
        int containerHeight,
        float widescreenOffset)
    {
        if (!StatusBarConditionResolver.Evaluate(m_ctx, def))
            return;

        Vec2I pos = ResolvePosition(def, parentPos, widescreenOffset);
        ReadOnlySpan<char> text = GetStringValue(def);
        if (text.IsEmpty) return;

        int fontHeight = m_hudFontLookup.TryGetValue(def.Font, out StatusBarHudFontDef? fontDef) ? hud.GetFontMaxHeight(fontDef.Stem) : 8;

        if (fontHeight <= 0) fontHeight = 8;
        float alpha = def.Translucency ? 0.5f : 1.0f;

        RenderLines(hud, text, pos, fontDef, fontHeight, def.Alignment, def.Translation, alpha);
        DrawChildren(hud, def, pos, containerHeight, 0, (0, 0));
    }

    private void DrawComponent(IHudRenderContext hud,
        StatusBarComponentDef comp,
        Vec2I parentPos,
        int containerHeight,
        float widescreenOffset,
        Vec2I rootPos)
    {
        if (!StatusBarConditionResolver.Evaluate(m_ctx, comp))
            return;

        Vec2I pos = ResolvePosition(comp, parentPos, widescreenOffset);
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
                if (!m_config.ShowStats.Value)
                    return;
                DrawStatTotals(hud, comp, pos, fontDef, fontHeight, alpha);
                break;

            case StatusBarComponentType.Time:
                TimeSpan t = TimeSpan.FromSeconds(m_ctx.World.LevelTime / 35.0);
                m_fmtSpan.Clear();
                m_fmtSpan.Append((int)t.TotalHours, 2);
                m_fmtSpan.Append(':');
                m_fmtSpan.Append(t.Minutes, 2);
                m_fmtSpan.Append(':');
                m_fmtSpan.Append(t.Seconds, 2);
                RenderLines(hud, m_fmtSpan.AsSpan(), pos, fontDef, fontHeight, alignment, comp.Translation, alpha);
                break;
            case StatusBarComponentType.Coordinates:
                DrawCoordinates(hud, comp, pos, fontDef, fontHeight, alpha);
                break;
            case StatusBarComponentType.Speedometer:
                string speedText = GetSpeedometerText(m_ctx.Player);
                RenderLines(hud, speedText.AsSpan(), pos, fontDef, fontHeight, alignment, comp.Translation, alpha);
                break;
            case StatusBarComponentType.LevelTitle:
                string levelTitle = m_ctx.MapInfo.GetDisplayNameWithPrefix(m_archiveCollection.Language);
                RenderLines(hud, levelTitle.AsSpan(), pos, fontDef, fontHeight, alignment, comp.Translation, alpha);
                break;
            case StatusBarComponentType.FpsCounter:
                if (!m_config.ShowFPS.Value)
                    return;
                m_fmtSpan.Clear();
                m_fmtSpan.Append(m_ctx.Fps);
                RenderLines(hud, m_fmtSpan.AsSpan(), pos, fontDef, fontHeight, alignment, comp.Translation, alpha);
                break;
            case StatusBarComponentType.Message:
                string msg = m_ctx.ConsoleMessage ?? string.Empty;
                if (m_ctx.IsMessageCentered)
                {
                    pos = new Vec2I(160, 66);
                    alignment = StatusBarAlignment.HCenter;
                }

                RenderLines(hud, msg.AsSpan(), pos, fontDef, fontHeight, alignment, comp.Translation, alpha);
                break;
            case StatusBarComponentType.AnnounceLevelTitle:
                double duration = comp.Duration > 0 ? comp.Duration : 2.5;
                const double FadeInTime = 0.25;
                const double FadeOutTime = 1.0;
                double timeSinceStart = m_ctx.World.LevelTime / Constants.TicksPerSecond;

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

                string annTitle = m_ctx.MapInfo.GetDisplayNameWithPrefix(m_archiveCollection.Language);
                RenderLines(hud, annTitle.AsSpan(), pos, fontDef, fontHeight, alignment, comp.Translation, alpha);
                break;

            case StatusBarComponentType.Unknown:
            case StatusBarComponentType.RenderStats:
            case StatusBarComponentType.CommandHistory:
            case StatusBarComponentType.Chat:
                break;

            default:
                return;
        }

        DrawChildren(hud, comp, pos, containerHeight, 0, rootPos);
    }

    private void RenderLines(IHudRenderContext hud,
        ReadOnlySpan<char> text,
        Vec2I pos,
        StatusBarHudFontDef? fontDef,
        int fontHeight,
        StatusBarAlignment alignment,
        string? translation,
        float alpha)
    {
        if (text.IsEmpty) return;

        Vec2I drawPos = pos;
        int lineStart = 0;

        for (int i = 0; i < text.Length; i++)
        {
            if (text[i] != '\n') continue;
            ReadOnlySpan<char> line = text[lineStart..i];
            DrawSingleLine(hud, line, drawPos, fontDef, alignment, translation, alpha);
            drawPos.Y += (int)(fontHeight * m_scale.Y);
            lineStart = i + 1;
        }

        if (lineStart >= text.Length) return;
        {
            ReadOnlySpan<char> line = text[lineStart..];
            DrawSingleLine(hud, line, drawPos, fontDef, alignment, translation, alpha);
        }
    }

    private void DrawSingleLine(IHudRenderContext hud,
        ReadOnlySpan<char> line,
        Vec2I drawPos,
        StatusBarHudFontDef? fontDef,
        StatusBarAlignment alignment,
        string? translation,
        float alpha)
    {
        if (line.IsEmpty)
            return;

        int drawnWidth = 0;
        if (fontDef != null)
            drawnWidth = DrawHudText(hud, line, fontDef, drawPos, alignment, translation, alpha);

        if (drawnWidth != 0)
            return;
        Align align = ConvertAlignment(alignment);
        hud.Text(line, Constants.Fonts.Small, 8, drawPos, TextAlign.Left, Align.TopLeft, align, alpha: alpha, scale: m_scale.X);
    }

    private int DrawHudText(IHudRenderContext hud,
        ReadOnlySpan<char> text,
        StatusBarHudFontDef fontDef,
        Vec2I pos,
        StatusBarAlignment alignment,
        string? translation,
        float alpha,
        bool draw = true)
    {
        Color? drawColor = null;
        if (!string.IsNullOrEmpty(translation) && StandardTextColors.TryGetValue(translation, out Color colorValue))
            drawColor = colorValue;

        if (StemToHelionFontMap.TryGetValue(fontDef.Stem, out string? helionFont))
        {
            if (!draw)
                return (int)(hud.MeasureText(text, helionFont, 8).Width * m_scale.X);

            Align anchor = ConvertAlignment(alignment);
            hud.Text(text, helionFont, 8, pos, TextAlign.Left, Align.TopLeft, anchor, color: drawColor, alpha: alpha, scale: m_scale.X);

            return (int)(hud.MeasureText(text, helionFont, 8).Width * m_scale.X);
        }

        int totalWidth = 0;
        int maxHeight = 0;

        m_glyphCache.Clear();

        var monoWidth = GetFontMonoWidth(hud, fontDef.Type, fontDef.Stem, fontDef, HudType0WidthCache, HudType1WidthCache, m_getHudFontPatch);

        foreach (char originalChar in text)
        {
            int width;
            string patch = string.Empty;
            char c = originalChar;

            if (c == ' ')
            {
                string bang = GetHudFontPatch(hud, fontDef, '!');
                width = hud.Textures.TryGet(bang, out IRenderableTextureHandle? h) ? h.Dimension.Width : 4;
            }
            else
            {
                if (char.IsLower(c)) c = char.ToUpper(c, CultureInfo.InvariantCulture);

                patch = GetHudFontPatch(hud, fontDef, c);
                bool found = ResolveGlyph(hud, patch, out width, out int height);

                if (!found && c != originalChar)
                {
                    string rawPatch = GetHudFontPatch(hud, fontDef, originalChar);
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

            if (monoWidth > 0)
                width = monoWidth;

            int scaledWidth = (int)(width * m_scale.X);
            m_glyphCache.Add(new RenderGlyph(patch, scaledWidth, 0));
            totalWidth += scaledWidth;
        }

        if (m_glyphCache.Count == 0 && text.Length > 0)
            return 0;
        if (!draw)
            return totalWidth;

        int drawX = pos.X;
        int drawY = pos.Y;

        if ((alignment & StatusBarAlignment.HCenter) != 0)
            drawX -= totalWidth / 2;
        else if ((alignment & StatusBarAlignment.Right) != 0)
            drawX -= totalWidth;

        int scaledMaxHeight = (int)(maxHeight * m_scale.Y);
        if ((alignment & StatusBarAlignment.Bottom) != 0)
            drawY -= scaledMaxHeight;
        else if ((alignment & StatusBarAlignment.VCenter) != 0)
            drawY -= scaledMaxHeight / 2;

        foreach (RenderGlyph g in m_glyphCache)
        {
            if (!string.IsNullOrEmpty(g.Patch))
                DrawSBarTexture(hud, g.Patch, null, (drawX, drawY), Align.TopLeft, alignment, translation, alpha);
            drawX += g.Width;
        }

        return totalWidth;
    }

    private static int GetFontMonoWidth<T>(IHudRenderContext hud, FontType type, string name, T fontDef,
        Dictionary<string, int> lookup0, Dictionary<string, int> lookup1,
        Func<IHudRenderContext, T, char, string> getFontPatch)
    {
        int monoWidth = 0;
        switch (type)
        {
            case FontType.MonoSpacedWidest:
                {
                    if (!lookup1.TryGetValue(name, out monoWidth))
                    {
                        monoWidth = 0;
                        for (char c1 = '!'; c1 <= '_'; c1++)
                        {
                            string patch1 = getFontPatch(hud, fontDef, c1);
                            if (ResolveGlyph(hud, patch1, out int w, out _))
                                monoWidth = Math.Max(monoWidth, w);
                        }

                        lookup1[name] = monoWidth;
                    }

                    break;
                }
            case FontType.MonoSpacedZero:
                {
                    if (!lookup0.TryGetValue(name, out monoWidth))
                    {
                        string zero = getFontPatch(hud, fontDef, '0');
                        if (hud.Textures.TryGet(zero, out var zh))
                            monoWidth = zh.Dimension.Width;

                        lookup0[name] = monoWidth;
                    }
                    break;
                }
        }

        return monoWidth;
    }

    private void DrawCarousel(IHudRenderContext hud,
        StatusBarCarouselDef carousel,
        Vec2I parentPos,
        int containerHeight,
        float widescreenOffset,
        Vec2I rootPos)
    {
        if (!StatusBarConditionResolver.Evaluate(m_ctx, carousel))
            return;

        Vec2I pos = ResolvePosition(carousel, parentPos, widescreenOffset);
        pos.X = 160;

        if (m_ctx.Player.Weapon != null)
        {
            string icon = m_ctx.Player.Weapon.Definition.Properties.Inventory.Icon;
            if (!string.IsNullOrEmpty(icon))
            {
                Align align = ConvertAlignment(carousel.Alignment);
                float alpha = carousel.Translucency ? 0.5f : 1.0f;
                DrawSBarTexture(hud, icon, null, pos, align, carousel.Alignment, carousel.Translation, alpha);
            }
        }

        DrawChildren(hud, carousel, pos, containerHeight, 0, rootPos);
    }

    private void DrawSBarTexture(IHudRenderContext hud,
        string patch,
        IRenderableTextureHandle? handle,
        Vec2I pos,
        Align align,
        StatusBarAlignment sbarAlign,
        string? translation = null,
        float alpha = 1.0f,
        StatusBarCropDef? cropDef = null)
    {
        string pName = handle == null ? ResolvePatchName(patch) : patch;
        if (handle == null && !hud.Textures.TryGet(pName, out handle) &&
            !hud.Textures.TryGet(pName, out handle, ResourceNamespace.Sprites)) return;

        ImageBox2I? cropArea = GetCropArea(handle, cropDef);

        int baseWidth = cropArea?.Width ?? handle.Dimension.Width;
        int baseHeight = cropArea?.Height ?? handle.Dimension.Height;

        int w = (int)(baseWidth * m_scale.X);
        int h = (int)(baseHeight * m_scale.Y);

        Vec2I translatedOffset = RenderDimensions.TranslateDoomOffset(handle.Offset);
        int offsetX = (sbarAlign & StatusBarAlignment.IgnoreLeftOffset) == 0 ? (int)(translatedOffset.X * m_scale.X) : 0;
        int offsetY = (sbarAlign & StatusBarAlignment.IgnoreTopOffset) == 0 ? (int)(translatedOffset.Y * m_scale.Y) : 0;

        Vec2I pivotOffset = align.AnchorDelta((w, h));

        int finalX = pos.X + offsetX + pivotOffset.X;
        int finalY = pos.Y + offsetY + pivotOffset.Y;
        HudBox destBox = new(new Vec2I(finalX, finalY), new Vec2I(finalX + w, finalY + h));

        Color? drawColor = null;
        if (!string.IsNullOrEmpty(translation) && StandardTextColors.TryGetValue(translation, out Color colorValue))
            drawColor = colorValue;

        hud.Image(pName,
            destBox,
            out _,
            Align.TopLeft,
            Align.TopLeft,
            null,
            ResourceNamespace.Undefined,
            drawColor,
            1.0f,
            alpha * m_alpha,
            0,
            1,
            null,
            cropArea);
    }

    private void DrawNumber(IHudRenderContext hud,
        StatusBarNumberDef number,
        Vec2I parentPos,
        int containerHeight,
        bool isPercent,
        float widescreenOffset)
    {
        if (!StatusBarConditionResolver.Evaluate(m_ctx, number))
            return;

        int value = ResolveNumberValue(m_ctx.Player, number.Type, number.Param);

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
        if (isPercent)
            m_fmtSpan.Append('%');

        ReadOnlySpan<char> text = m_fmtSpan.AsSpan();

        Vec2I pos = ResolvePosition(number, parentPos, widescreenOffset);

        float alpha = number.Translucency ? 0.5f : 1.0f;
        int totalWidth = 0;
        var monoWidth = GetFontMonoWidth(hud, fontDef.Type, fontDef.Name, fontDef, Type0WidthCache, Type1WidthCache, m_getFontNumberPatch);

        m_glyphCache.Clear();

        foreach (char c in text)
        {
            string patch = GetFontPatch(hud, fontDef, c);
            int width;
            int xOffset = 0;

            if (hud.Textures.TryGet(patch, out IRenderableTextureHandle? handle) || hud.Textures.TryGet(patch, out handle, ResourceNamespace.Sprites))
                width = handle.Dimension.Width;
            else
                continue;

            if ((fontDef.Type is FontType.MonoSpacedZero or FontType.MonoSpacedWidest) && monoWidth > 0)
                width = monoWidth;

            int scaledWidth = (int)(width * m_scale.X);
            m_glyphCache.Add(new RenderGlyph(patch, scaledWidth, (int)(xOffset * m_scale.X), handle));
            totalWidth += scaledWidth;
        }

        int drawX = pos.X;
        int drawY = pos.Y;

        if ((number.Alignment & StatusBarAlignment.HCenter) != 0)
            drawX -= totalWidth / 2;
        else if ((number.Alignment & StatusBarAlignment.Right) != 0)
            drawX -= totalWidth;

        Align yAnchor = Align.TopLeft;
        if ((number.Alignment & StatusBarAlignment.Bottom) != 0) 
            yAnchor = Align.BottomLeft;
        else if ((number.Alignment & StatusBarAlignment.VCenter) != 0) 
            yAnchor = Align.MiddleLeft; 

        foreach (RenderGlyph g in m_glyphCache)
        {
            Vec2I drawPos = (drawX + g.Offset, drawY);
            DrawSBarTexture(hud, g.Patch, g.Handle, drawPos, yAnchor, number.Alignment, number.Translation, alpha);
            drawX += g.Width;
        }

        DrawChildren(hud, number, pos, containerHeight, 0, (0, 0));
    }

    private void DrawStatTotals(IHudRenderContext hud,
        StatusBarComponentDef comp,
        Vec2I pos,
        StatusBarHudFontDef? fontDef,
        int fontHeight,
        float alpha)
    {
        LevelStats stats = m_ctx.World.LevelStats;
        Vec2I cur = pos;
        DrawStatPart(hud, "K: ", stats.KillCount, stats.TotalMonsters, ref cur, comp, fontDef, fontHeight, alpha);
        DrawStatPart(hud, "I: ", stats.ItemCount, stats.TotalItems, ref cur, comp, fontDef, fontHeight, alpha);
        DrawStatPart(hud, "S: ", stats.SecretCount, stats.TotalSecrets, ref cur, comp, fontDef, fontHeight, alpha);
    }

    private void DrawStatPart(IHudRenderContext hud,
        string label,
        int count,
        int total,
        ref Vec2I cursor,
        StatusBarComponentDef comp,
        StatusBarHudFontDef? fontDef,
        int fontHeight,
        float alpha)
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

        if (comp.Vertical) cursor.Y += (int)(fontHeight * m_scale.Y);
        else cursor.X += labelWidth + valueWidth + (int)(8 * m_scale.X);
    }

    private void DrawCoordinates(IHudRenderContext hud,
        StatusBarComponentDef comp,
        Vec2I pos,
        StatusBarHudFontDef? fontDef,
        int fontHeight,
        float alpha)
    {
        Vec3D playerPos = m_ctx.Player.Position;
        m_coordPartsCache.Clear();
        m_coordPartsCache.Add(new CoordData("X: ", (int)playerPos.X, 0, 0));
        m_coordPartsCache.Add(new CoordData("Y: ", (int)playerPos.Y, 0, 0));
        m_coordPartsCache.Add(new CoordData("Z: ", (int)playerPos.Z, 0, 0));

        int totalHorizontalWidth = 0;
        for (int i = 0; i < m_coordPartsCache.Count; i++)
        {
            CoordData data = m_coordPartsCache[i];
            int lw = MeasureSpan(hud, data.Label.AsSpan(), "CRGREEN", fontDef, alpha);
            m_fmtSpan.Clear();
            m_fmtSpan.Append(data.Value);
            int vw = MeasureSpan(hud, m_fmtSpan.AsSpan(), comp.Translation, fontDef, alpha);
            m_coordPartsCache[i] = data with { LabelWidth = lw, ValWidth = vw };
            totalHorizontalWidth += lw + vw;
            if (i < m_coordPartsCache.Count - 1) totalHorizontalWidth += (int)(8 * m_scale.X);
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

                _ = DrawTextPart(hud, data.Label.AsSpan(), (lineX, cursor.Y), "CRGREEN", StatusBarAlignment.Left, fontDef, alpha);

                m_fmtSpan.Clear();
                m_fmtSpan.Append(data.Value);

                _ = DrawTextPart(hud,
                    m_fmtSpan.AsSpan(),
                    (lineX + data.LabelWidth, cursor.Y),
                    comp.Translation,
                    StatusBarAlignment.Left,
                    fontDef,
                    alpha);

                cursor.Y += (int)(fontHeight * m_scale.Y);
            }
            else
            {
                cursor.X += DrawTextPart(hud, data.Label.AsSpan(), cursor, "CRGREEN", StatusBarAlignment.Left, fontDef, alpha);

                m_fmtSpan.Clear();
                m_fmtSpan.Append(data.Value);

                cursor.X += DrawTextPart(hud, m_fmtSpan.AsSpan(), cursor, comp.Translation, StatusBarAlignment.Left, fontDef, alpha);

                cursor.X += (int)(8 * m_scale.X);
            }
    }

    private int MeasureSpan(IHudRenderContext hud, ReadOnlySpan<char> t, string? trans, StatusBarHudFontDef? fontDef, float alpha)
    {
        return fontDef != null
            ? DrawHudText(hud, t, fontDef, (0, 0), StatusBarAlignment.Left, trans, alpha, false)
            : (int)(hud.MeasureText(t, Constants.Fonts.Small, 8).Width * m_scale.X);
    }

    private int DrawTextPart(IHudRenderContext hud,
        ReadOnlySpan<char> text,
        Vec2I position,
        string? translation,
        StatusBarAlignment alignment,
        StatusBarHudFontDef? fontDef,
        float alpha)
    {
        if (fontDef != null)
            return DrawHudText(hud, text, fontDef, position, alignment, translation, alpha);

        Align align = ConvertAlignment(alignment);
        Color? color = !string.IsNullOrEmpty(translation) && StandardTextColors.TryGetValue(translation, out Color c) ? c : null;

        hud.Text(text, Constants.Fonts.Small, 8, position, both: align, alpha: alpha, color: color, scale: m_scale.X);
        return (int)(hud.MeasureText(text, Constants.Fonts.Small, 8).Width * m_scale.X);
    }

    private void DrawChildren(IHudRenderContext hud,
        StatusBarBaseDef def,
        Vec2I pos,
        int containerHeight,
        float widescreenOffset,
        Vec2I rootPos)
    {
        if (def.Children == null) return;

        foreach (StatusBarElementWrapper child in def.Children)
        {
            if (!EvaluateWrapperConditions(child))
                continue;

            DrawElementWrapper(hud, child, pos, containerHeight, widescreenOffset, rootPos);
        }
    }

    private ElementBounds MeasureElement(IHudRenderContext hud,
        StatusBarElementWrapper wrapper,
        float sX = 1.0f,
        float sY = 1.0f)
    {
        if (wrapper.Graphic != null)
            return MeasureGraphic(wrapper.Graphic, sX, sY);
        if (wrapper.Number != null)
            return MeasureNumber(hud, wrapper.Number, false, sX, sY);
        if (wrapper.Percent != null)
            return MeasureNumber(hud, wrapper.Percent, true, sX, sY);
        if (wrapper.Face != null)
            return MeasureFace(hud, wrapper.Face, m_ctx.Player, sX, sY);
        if (wrapper.String != null)
            return MeasureString(hud, wrapper.String, sX, sY);
        if (wrapper.Canvas != null)
            return MeasureBase(hud, wrapper.Canvas, sX, sY);
        if (wrapper.List != null)
            return MeasureList(hud, wrapper.List, sX, sY);
        if (wrapper.Component != null)
            return MeasureComponent(hud, wrapper.Component, sX, sY);
        if (wrapper.Carousel != null)
            return MeasureCarousel(hud, wrapper.Carousel, m_ctx.Player, sX, sY);

        if (wrapper.Native == null)
            return ElementBounds.Empty;

        float nX = m_userScale;
        float nY = m_userScale * 1.2f;

        ElementBounds bounds = MeasureBase(hud, wrapper.Native, nX, nY);

        return m_currentScale > 0
            ? new ElementBounds((int)(bounds.X1 / m_currentScale),
                (int)(bounds.Y1 / m_currentScale),
                (int)(bounds.X2 / m_currentScale),
                (int)(bounds.Y2 / m_currentScale))
            : bounds;
    }

    private ElementBounds MeasureBase(IHudRenderContext hud, StatusBarBaseDef def, float sX, float sY)
    {
        if (def.Children == null || def.Children.Length == 0)
            return ElementBounds.Empty;

        if (!m_ctx.HasTicks)
            return def.LastBounds;

        var contentBounds = ElementBounds.Empty;
        foreach (StatusBarElementWrapper t in def.Children)
        {
            if (!m_invalidateBounds)
            {
                if (t.BoundsSet)
                {
                    ElementBounds.Union(ref contentBounds, t.Bounds);
                    continue;
                }
            }

            t.BoundsSet = true;
            t.Bounds = MeasureElement(hud, t, sX, sY);
            ElementBounds.Union(ref contentBounds, t.Bounds);
        }

        if (contentBounds.X1 == int.MaxValue)
        {
            def.LastBounds = ElementBounds.Empty;
        }
        else
        {
            int posX = (int)(def.X * sX);
            int posY = (int)(def.Y * sY);
            var containerPos = ApplyAlignment(new Vec2I(contentBounds.Width, contentBounds.Height), posX, posY, def.Alignment);

            def.LastBounds = new ElementBounds(containerPos.X1 + contentBounds.X1,
                containerPos.Y1 + contentBounds.Y1,
                containerPos.X1 + contentBounds.X2,
                containerPos.Y1 + contentBounds.Y2);
        }

        return def.LastBounds;
    }

    private static ElementBounds MeasureGraphic(StatusBarGraphicDef def, float scaleX, float scaleY)
    {
        IRenderableTextureHandle? handle = def.Handle;
        if (handle == null) return ElementBounds.Empty;

        ImageBox2I? cropArea = GetCropArea(handle, def.Crop);
        int baseWidth = cropArea?.Width ?? handle.Dimension.Width;
        int baseHeight = cropArea?.Height ?? handle.Dimension.Height;

        Vec2I size = new((int)(baseWidth * scaleX), (int)(baseHeight * scaleY));
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
        bool isPercent,
        float sX,
        float sY)
    {
        m_fmtSpan.Clear();
        m_fmtSpan.Append(ResolveNumberValue(m_ctx.Player, def.Type, def.Param));
        if (isPercent) m_fmtSpan.Append('%');

        Vec2F oldScale = m_scale;
        m_scale = (sX, sY);
        int width = MeasureSpan(hud, m_fmtSpan.AsSpan(), null, null, 1.0f);
        m_scale = oldScale;

        int height = (int)((def.ResolvedHeight > 0 ? def.ResolvedHeight : 8) * sY);

        int posX = (int)(def.X * sX);
        int posY = (int)(def.Y * sY);
        return ApplyAlignment(new Vec2I(width, height), posX, posY, def.Alignment);
    }

    private static ElementBounds MeasureFace(IHudRenderContext hud,
        StatusBarFaceDef def,
        Player player,
        float scaleX,
        float scaleY)
    {
        const string StabilizerPatch = "STFST01";
        if (!hud.Textures.TryGet(StabilizerPatch, out IRenderableTextureHandle? h) &&
            !hud.Textures.TryGet(StabilizerPatch, out h, ResourceNamespace.Sprites))
        {
            string p = player.StatusBar.GetFacePatch();
            if (!hud.Textures.TryGet(p, out h) && !hud.Textures.TryGet(p, out h, ResourceNamespace.Sprites)) 
                return ElementBounds.Empty;
        }

        ImageBox2I? cropArea = GetCropArea(h, def.Crop);
        int baseWidth = cropArea?.Width ?? h.Dimension.Width;
        int baseHeight = cropArea?.Height ?? h.Dimension.Height;

        Vec2I size = new((int)(baseWidth * scaleX), (int)(baseHeight * scaleY));
        int posX = (int)(def.X * scaleX);
        int posY = (int)(def.Y * scaleY);

        Vec2I translatedOffset = RenderDimensions.TranslateDoomOffset(h.Offset);

        if ((def.Alignment & StatusBarAlignment.IgnoreLeftOffset) == 0)
            posX += (int)(translatedOffset.X * scaleX);

        if ((def.Alignment & StatusBarAlignment.IgnoreTopOffset) == 0)
            posY += (int)(translatedOffset.Y * scaleY);

        return ApplyAlignment(size, posX, posY, def.Alignment);
    }

    private ElementBounds MeasureString(IHudRenderContext hud, StatusBarStringDef def, float sX, float sY)
    {
        ReadOnlySpan<char> text = GetStringValue(def);
        _ = m_hudFontLookup.TryGetValue(def.Font, out StatusBarHudFontDef? f);

        Vec2F oldScale = m_scale;
        m_scale = (sX, sY);
        int width = MeasureSpan(hud, text, null, f, 1.0f);
        m_scale = oldScale;

        int height = (int)((def.ResolvedHeight > 0 ? def.ResolvedHeight : 8) * sY);

        int posX = (int)(def.X * sX);
        int posY = (int)(def.Y * sY);
        return ApplyAlignment(new Vec2I(width, height), posX, posY, def.Alignment);
    }

    private ElementBounds MeasureList(IHudRenderContext hud, StatusBarListDef def, float sX, float sY)
    {
        if (def.Children == null)
            return ElementBounds.Empty;

        int totalW = 0;
        int totalH = 0;
        int count = 0;

        int spacing = (int)(def.Spacing * sX);

        foreach (StatusBarElementWrapper child in def.Children)
        {
            ElementBounds size = MeasureElement(hud, child, sX, sY);
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

        int posX = (int)(def.X * sX);
        int posY = (int)(def.Y * sY);

        return ApplyAlignment(new Vec2I(totalW, totalH), posX, posY, def.Alignment);
    }

    private ElementBounds MeasureComponent(IHudRenderContext hud, StatusBarComponentDef comp, float sX, float sY)
    {
        int fontHeight = 8;
        if (m_hudFontLookup.TryGetValue(comp.Font, out StatusBarHudFontDef? fontDef) &&
            StemToHelionFontMap.TryGetValue(fontDef.Stem, out string? helionFontName))
        {
            int h = hud.GetFontMaxHeight(helionFontName);
            if (h > 0) fontHeight = h;
        }

        Vec2I size = Vec2I.Zero;
        Vec2F oldScale = m_scale;
        m_scale = (sX, sY);

        switch (comp.ComponentType)
        {
            case StatusBarComponentType.StatTotals:
                if (m_config.ShowStats.Value)
                {
                    const string Dummy = "K: 000/000 I: 000/000 S: 000/000";
                    int w = MeasureSpan(hud, Dummy.AsSpan(), null, fontDef, 1.0f);
                    size = comp.Vertical ? (w / 3, (int)(fontHeight * 3 * sY)) : (w, (int)(fontHeight * sY));
                }

                break;

            case StatusBarComponentType.Time:
                size = (MeasureSpan(hud, "00:00:00".AsSpan(), null, fontDef, 1.0f), (int)(fontHeight * sY));
                break;

            case StatusBarComponentType.Coordinates:
                int cw = MeasureSpan(hud, "X: -00000 Y: -00000 Z: -00000".AsSpan(), null, fontDef, 1.0f);
                size = comp.Vertical ? (cw / 3, (int)(fontHeight * 3 * sY)) : (cw, (int)(fontHeight * sY));
                break;

            case StatusBarComponentType.Speedometer:
                size = (MeasureSpan(hud, "000.00".AsSpan(), null, fontDef, 1.0f), (int)(fontHeight * sY));
                break;

            case StatusBarComponentType.LevelTitle:
            case StatusBarComponentType.AnnounceLevelTitle:
                string title = m_ctx.MapInfo.GetDisplayNameWithPrefix(m_archiveCollection.Language);
                size = (MeasureSpan(hud, title.AsSpan(), null, fontDef, 1.0f), (int)(fontHeight * sY));
                break;

            case StatusBarComponentType.FpsCounter:
                if (m_config.ShowFPS.Value)
                    size = (MeasureSpan(hud, "000".AsSpan(), null, fontDef, 1.0f), (int)(fontHeight * sY));
                break;

            case StatusBarComponentType.Unknown:
            case StatusBarComponentType.Message:
            case StatusBarComponentType.RenderStats:
            case StatusBarComponentType.CommandHistory:
            case StatusBarComponentType.Chat:
                break;

            default:
                size = Vec2I.Zero;
                break;
        }

        m_scale = oldScale;
        int posX = (int)(comp.X * sX);
        int posY = (int)(comp.Y * sY);

        return ApplyAlignment(size, posX, posY, comp.Alignment);
    }

    private static ElementBounds MeasureCarousel(IHudRenderContext hud,
        StatusBarCarouselDef carousel,
        Player player,
        float scaleX,
        float scaleY)
    {
        Vec2I size = Vec2I.Zero;
        if (player.Weapon != null)
        {
            var icon = player.Weapon.Definition.Properties.Inventory.Icon;
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
        if (string.IsNullOrEmpty(patch))
            return string.Empty;

        ReadOnlySpan<char> patchSpan = patch.AsSpan();

        if (PatchNameCacheBySpan.TryGetValue(patchSpan, out string? cached))
            return cached;

        int lastSlash = patch.LastIndexOf('/') + 1;
        int lastDot = patch.LastIndexOf('.');

        if (lastDot < lastSlash)
            lastDot = patch.Length;

        if (lastSlash == 0 && lastDot == patch.Length)
        {
            PatchNameCache[patch] = patch;
            return patch;
        }

        string result = patch[lastSlash..lastDot];
        PatchNameCache[patch] = result;
        return result;
    }

    private ReadOnlySpan<char> GetStringValue(StatusBarStringDef def)
    {
        return def.Type switch
        {
            0 => def.Data.AsSpan(),
            1 => m_ctx.MapInfo.GetDisplayNameWithPrefix(m_archiveCollection.Language).AsSpan(),
            2 => m_ctx.MapInfo.Label.AsSpan(),
            3 => m_ctx.MapInfo.Author.AsSpan(),
            _ => []
        };
    }

    private bool EvaluateWrapperConditions(StatusBarElementWrapper wrapper)
    {
        return wrapper switch
        {
            { Canvas: not null } => StatusBarConditionResolver.Evaluate(m_ctx, wrapper.Canvas),
            { Graphic: not null } => StatusBarConditionResolver.Evaluate(m_ctx, wrapper.Graphic),
            { Number: not null } => StatusBarConditionResolver.Evaluate(m_ctx, wrapper.Number),
            { Percent: not null } => StatusBarConditionResolver.Evaluate(m_ctx, wrapper.Percent),
            { Face: not null } => StatusBarConditionResolver.Evaluate(m_ctx, wrapper.Face),
            { Animation: not null } => StatusBarConditionResolver.Evaluate(m_ctx, wrapper.Animation),
            { Component: not null } => StatusBarConditionResolver.Evaluate(m_ctx, wrapper.Component),
            { Carousel: not null } => StatusBarConditionResolver.Evaluate(m_ctx, wrapper.Carousel),
            { List: not null } => StatusBarConditionResolver.Evaluate(m_ctx, wrapper.List),
            { String: not null } => StatusBarConditionResolver.Evaluate(m_ctx, wrapper.String),
            _ => true
        };
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private Vec2I ResolvePosition(StatusBarBaseDef def, Vec2I parentPos, float widescreenOffset)
    {
        Vec2I pos = parentPos;
        pos.X += (int)(def.X * m_scale.X);
        pos.Y += (int)(def.Y * m_scale.Y);

        if ((def.Alignment & StatusBarAlignment.WidescreenLeft) != 0)
            pos.X -= (int)widescreenOffset;
        else if ((def.Alignment & StatusBarAlignment.WidescreenRight) != 0)
            pos.X += (int)widescreenOffset;

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

    private string GetHudFontPatch(IHudRenderContext hud, StatusBarHudFontDef font, char c)
    {
        m_lookupKeySpan.Clear();
        m_lookupKeySpan.Append(font.Stem);
        m_lookupKeySpan.Append(c);

        if (HudFontPatchCacheBySpan.TryGetValue(m_lookupKeySpan.AsSpan(), out var cached))
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
        if (FontPatchCacheBySpan.TryGetValue(m_lookupKeySpan.AsSpan(), out string? cached))
            return cached;

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
        var composer = m_archiveCollection.EntityDefinitionComposer;
        var stats = m_ctx.World.LevelStats;

        switch (type)
        {
            case StatusBarNumberType.Health:
                return Math.Max(0, player.Health);
            case StatusBarNumberType.Armor:
                return player.Armor;
            case StatusBarNumberType.Frags:
                return 0;
            case StatusBarNumberType.Ammo:
                return StatusBarConditionResolver.TryGetId24AmmoType(composer, param, out var ammoDef)
                    ? GetAmount(player, ammoDef)
                    : 0;

            case StatusBarNumberType.AmmoSelected:
                return GetAmount(player, player.Weapon?.AmmoDefinition);

            case StatusBarNumberType.MaxAmmo:
                return StatusBarConditionResolver.TryGetId24AmmoType(composer, param, out var maxAmmoDef)
                    ? GetMaxAmmoAmount(maxAmmoDef)
                    : 0;

            case StatusBarNumberType.AmmoWeapon:
                return m_id24PickupTypeLookup.TryGetValue(param, out var wDef)
                    ? GetAmount(player, wDef.Properties.Weapons.AmmoTypeDef)
                    : 0;

            case StatusBarNumberType.MaxAmmoWeapon:
                return m_id24PickupTypeLookup.TryGetValue(param, out var mwDef)
                    ? GetMaxAmmoAmount(mwDef.Properties.Weapons.AmmoTypeDef)
                    : 0;

            case StatusBarNumberType.Kills:
                return stats.KillCount;
            case StatusBarNumberType.Items:
                return stats.ItemCount;
            case StatusBarNumberType.Secrets:
                return stats.SecretCount;

            case StatusBarNumberType.KillsPercent:
                return stats.TotalMonsters > 0 ? stats.KillCount * 100 / stats.TotalMonsters : 100;
            case StatusBarNumberType.ItemsPercent:
                return stats.TotalItems > 0 ? stats.ItemCount * 100 / stats.TotalItems : 100;
            case StatusBarNumberType.SecretsPercent:
                return stats.TotalSecrets > 0 ? stats.SecretCount * 100 / stats.TotalSecrets : 100;

            case StatusBarNumberType.MaxKills:
                return stats.TotalMonsters;
            case StatusBarNumberType.MaxItems:
                return stats.TotalItems;
            case StatusBarNumberType.MaxSecrets:
                return stats.TotalSecrets;

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

            default:
                return 0;
        }
    }

    private static string GetSpeedometerText(Player player)
    {
        _ = player;
        return string.Empty;
    }

    private static int GetAmount(Player player, EntityDefinition? def)
    {
        if (def == null)
            return 0;

        return player.Inventory.Amount(def);
    }

    private int GetMaxAmmoAmount(EntityDefinition? ammoDef)
    {
        if (ammoDef == null)
            return 0;

        var baseDef = Inventory.GetBaseInventoryDefinition(ammoDef) ?? ammoDef;
        var max = baseDef.Properties.Inventory.MaxAmount;
        if (m_ctx.HasBackPack && baseDef.IsAmmo)
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
}