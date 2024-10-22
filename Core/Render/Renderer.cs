using GlmSharp;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Graphics.Palettes;
using Helion.Render.Common.Textures;
using Helion.Render.OpenGL;
using Helion.Render.OpenGL.Commands;
using Helion.Render.OpenGL.Commands.Types;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Framebuffer;
using Helion.Render.OpenGL.Renderers;
using Helion.Render.OpenGL.Renderers.Legacy.Hud;
using Helion.Render.OpenGL.Renderers.Legacy.World;
using Helion.Render.OpenGL.Renderers.Legacy.World.Automap;
using Helion.Render.OpenGL.Renderers.Legacy.World.Shader;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.OpenGL.Textures;
using Helion.Render.OpenGL.Util;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Configs.Components;
using Helion.Util.Timing;
using Helion.Window;
using Helion.World;
using Helion.World.Entities;
using Helion.World.Entities.Inventories.Powerups;
using Helion.World.Entities.Players;
using Helion.World.Geometry.Sectors;
using NLog;
using OpenTK.Graphics.OpenGL;
using System;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render;

public record struct FieldOfViewInfo(float Width, float Height, float FovY);

public partial class Renderer : IDisposable
{
    public const float ZNearMin = 0.2f;
    public const float ZNearMax = 7.9f;
    public const float ZFar = 65536;
    public const float ReversedZNear = 0.01f;
    public static readonly Color DefaultBackground = (16, 16, 16);
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static bool InfoPrinted;

    public readonly IWindow Window;
    public readonly GLSurface Default;
    /// <summary>
    /// Final framebuffer used to draw to screen.
    /// </summary>
    private GLFramebuffer m_mainFramebuffer;
    /// <summary>
    /// Framebuffer used to draw the world.
    /// </summary>
    private GLFramebuffer m_virtualFramebuffer;
    public readonly LegacyGLTextureManager Textures;
    internal readonly IConfig m_config;
    internal readonly FpsTracker m_fpsTracker;
    internal readonly ArchiveCollection m_archiveCollection;
    private readonly WorldRenderer m_worldRenderer;
    private readonly HudRenderer m_hudRenderer;
    private readonly RenderInfo m_renderInfo = new();
    private readonly BasicFramebufferRenderer m_framebufferRenderer;
    private readonly LegacyAutomapRenderer m_automapRenderer;
    private readonly TransitionRenderer m_transitionRenderer;

    private IWorld? m_world;
    private GLBufferTextureStorage? m_colorMapBuffer;
    private Rectangle m_viewport = new(0, 0, 800, 600);
    private bool m_disposed;

    public Dimension RenderDimension => UseVirtualResolution ? m_config.Window.Virtual.Dimension : Window.Dimension;
    public IImageDrawInfoProvider DrawInfo => Textures.ImageDrawInfoProvider;
    private bool UseVirtualResolution => (m_config.Window.Virtual.Enable && m_config.Window.Virtual.Dimension.Value.HasPositiveArea);

    public Renderer(IWindow window, IConfig config, ArchiveCollection archiveCollection, FpsTracker fpsTracker)
    {
        Window = window;
        m_config = config;
        m_archiveCollection = archiveCollection;
        m_fpsTracker = fpsTracker;

        SetGLDebugger();
        SetShaderVars();

        Textures = new LegacyGLTextureManager(config, archiveCollection);
        m_worldRenderer = new LegacyWorldRenderer(config, archiveCollection, Textures);
        m_hudRenderer = new LegacyHudRenderer(config, Textures, archiveCollection.DataCache);
        m_automapRenderer = new LegacyAutomapRenderer(archiveCollection);
        m_framebufferRenderer = new BasicFramebufferRenderer(window);
        m_transitionRenderer = new TransitionRenderer(window);
        Default = new(window, this);
        m_mainFramebuffer = GenerateMainFramebuffer();
        m_virtualFramebuffer = GenerateVirtualFramebuffer();

        PrintGLInfo();
        SetGLStates();
    }

    private GLFramebuffer GenerateMainFramebuffer() => new("Main", Window.Dimension, 1, RenderbufferStorage.Depth32fStencil8);
    private GLFramebuffer GenerateVirtualFramebuffer() => new("Virtual", RenderDimension, 1, RenderbufferStorage.Depth32fStencil8);

    public unsafe void UploadColorMap()
    {
        if (!ShaderVars.PaletteColorMode)
            return;

        var colorMapData = ColorMapBuffer.Create(m_archiveCollection.Palette, m_archiveCollection.Colormap, m_archiveCollection.Definitions.Colormaps);
        m_colorMapBuffer = new("Colormap buffer", colorMapData, SizedInternalFormat.Rgb32f, GLInfo.MapPersistentBitSupported);

        m_colorMapBuffer.Map(data =>
        {
            float* destBuffer = (float*)data.ToPointer();
            fixed (float* colorMapBuffer = &colorMapData[0])
            {
                int length = sizeof(float) * colorMapData.Length;
                System.Buffer.MemoryCopy(colorMapBuffer, destBuffer, length, length);
            };
        });
    }

    private void SetShaderVars()
    {
        SetReverseZ();
        ShaderVars.Depth = ShaderVars.ReversedZ ? "w" : "z";
        ShaderVars.PaletteColorMode = m_config.Window.ColorMode.Value == RenderColorMode.Palette;
    }

    private void SetReverseZ()
    {
        if (!m_config.Developer.UseReversedZ)
        {
            ShaderVars.ReversedZ = GLInfo.ClipControlSupported;
            return;
        }

        ShaderVars.ReversedZ = m_config.Developer.ReversedZ;
    }

    ~Renderer()
    {
        Dispose(false);
    }

    public static float GetTimeFrac()
    {
        if (WorldStatic.World == null)
            return 0;

        const int TicksPerFrame = 4;
        const int DifferentFrames = 8;

        return ((WorldStatic.World.GameTicker / TicksPerFrame) % DifferentFrames) + 1;
    }

    public static float GetFuzzDiv(ConfigRender config, in Rectangle viewport)
    {
        return viewport.Height / 480f * (float)config.FuzzAmount;
    }

    public static ShaderUniforms GetShaderUniforms(IConfig config, IWorld world, RenderInfo renderInfo)
    {
        bool drawInvulnerability = false;
        int extraLight = 0;
        float mix = 0.0f;
        var colorMix = GetColorMix(renderInfo.ViewerEntity, renderInfo.Camera);
        PaletteIndex paletteIndex = PaletteIndex.Normal;
        ColorMapUniforms colorMapUniforms = default;

        if (renderInfo.ViewerEntity.PlayerObj != null)
        {
            var player = renderInfo.ViewerEntity.PlayerObj;
            if (player.DrawFullBright())
                mix = 1.0f;
            if (player.DrawInvulnerableColorMap())
                drawInvulnerability = true;

            extraLight = player.GetExtraLightRender();

            if (ShaderVars.PaletteColorMode)
            {
                mix = 0.0f;
                colorMapUniforms = GetColorMapUniforms(renderInfo.ViewerEntity, renderInfo.Camera);
                paletteIndex = GetPalette(config, player);
                if (!player.DrawInvulnerableColorMap() && player.DrawFullBright())
                    mix = 1.0f;
            }
        }

        return new ShaderUniforms(CalculateMvpMatrix(renderInfo),
            CalculateMvpMatrix(renderInfo, true),
            GetTimeFrac(), drawInvulnerability, mix, extraLight, GetDistanceOffset(renderInfo),
            colorMix, GetFuzzDiv(renderInfo.Config, renderInfo.Viewport), colorMapUniforms, paletteIndex, config.Render.LightMode, 
            (float)config.Render.GammaCorrection);
    }

    private static PaletteIndex GetPalette(IConfig config, Player player)
    {
        var palette = PaletteIndex.Normal;
        var powerup = player.Inventory.PowerupEffectColor;
        int damageCount = player.DamageCount;

        if (powerup != null && powerup.PowerupType == PowerupType.Strength)
            damageCount = Math.Max(damageCount, 12 - (powerup.Ticks >> 6));

        if (damageCount > 0)
        {
            if (damageCount == player.DamageCount)
                damageCount = (int)(player.DamageCount * config.Game.PainIntensity);

            palette = GetDamagePalette(damageCount);
        }
        else if (player.BonusCount > 0)
        {
            palette = GetBonusPalette(player.BonusCount);
        }

        if (palette == PaletteIndex.Normal && powerup != null &&
            powerup.PowerupType == PowerupType.IronFeet && powerup.DrawPowerupEffect)
        {
            palette = PaletteIndex.Green;
        }

        return palette;
    }

    private static PaletteIndex GetBonusPalette(int bonusCount)
    {
        const int BonusPals = 4;
        const int StartBonusPals = 9;
        int palette = (bonusCount + 7) >> 3;
        if (palette >= BonusPals)
            palette = BonusPals - 1;
        palette += StartBonusPals;
        return (PaletteIndex)palette;
    }

    private static PaletteIndex GetDamagePalette(int damageCount)
    {
        const int RedPals = 8;
        const int StartRedPals = 1;
        int palette = (damageCount + 7) >> 3;
        if (palette >= RedPals)
            palette = RedPals - 1;
        palette += StartRedPals;
        return (PaletteIndex)palette;
    }

    private static ColorMapUniforms GetColorMapUniforms(Entity viewer, OldCamera camera)
    {
        ColorMapUniforms uniforms = default;
        if (ShaderVars.PaletteColorMode)
        {
            GetViewerColorMap(viewer, camera, out var globalColormap, out var sectorColormap, out var skyColormap);
            if (globalColormap != null)
                uniforms.GlobalIndex = globalColormap.Index;
            if (sectorColormap != null)
                uniforms.SectorIndex = sectorColormap.Index;
            if (skyColormap != null)
                uniforms.SkyIndex = skyColormap.Index;
        }
        return uniforms;
    }

    public static ColorMixUniforms GetColorMix(Entity viewer, OldCamera camera)
    {
        ColorMixUniforms uniforms = new(Vec3F.One, Vec3F.One, Vec3F.One);
        if (!ShaderVars.PaletteColorMode)
        {
            GetViewerColorMap(viewer, camera, out var globalColormap, out var sectorColormap, out var skyColormap);
            if (globalColormap != null)
                uniforms.Global = globalColormap.ColorMix;
            if (sectorColormap != null)
                uniforms.Sector = sectorColormap.ColorMix;
            if (skyColormap != null)
                uniforms.Sky = skyColormap.ColorMix;
        }
        return uniforms;
    }

    private static void GetViewerColorMap(Entity viewer, OldCamera camera,
        out Colormap? globalColormap, out Colormap? sectorColormap, out Colormap? skyColormap)
    {
        globalColormap = null;
        sectorColormap = null;
        skyColormap = null;

        if (viewer.Sector.TransferHeights != null)
        {
            viewer.Sector.TransferHeights.TryGetColormap(viewer.Sector, camera.PositionInterpolated.Z, out globalColormap);
            skyColormap = globalColormap;
        }

        if (viewer.Sector.TransferFloorLightSector.Colormap != null)
            sectorColormap = viewer.Sector.TransferFloorLightSector.Colormap;
    }

    public static mat4 CalculateMvpMatrix(RenderInfo renderInfo, bool onlyXY = false)
    {
        mat4 model = mat4.Identity;
        mat4 view = renderInfo.Camera.CalculateViewMatrix(onlyXY);
        return GetProjection(renderInfo) * view * model;
    }

    private static mat4 GetProjection(RenderInfo renderInfo)
    {
        var fovInfo = GetFieldOfViewInfo(renderInfo);
        if (!ShaderVars.ReversedZ)
            return mat4.PerspectiveFov(fovInfo.FovY, fovInfo.Width, fovInfo.Height, GetZNear(renderInfo), ZFar);

        // Adapted from https://nlguillemot.wordpress.com/2016/12/07/reversed-z-in-opengl/
        var viewFov = Math.Cos((double)fovInfo.FovY / 2.0) / Math.Sin((double)fovInfo.FovY / 2.0);
        var viewAspect = viewFov * (double)(fovInfo.Height / fovInfo.Width);
        mat4 projection = mat4.Zero;
        projection.m00 = (float)viewAspect;
        projection.m11 = (float)viewFov;
        projection.m23 = -1;
        projection.m32 = ReversedZNear;
        return projection;
    }

    public static FieldOfViewInfo GetFieldOfViewInfo(RenderInfo renderInfo)
    {
        float w = renderInfo.Viewport.Width;
        float h = renderInfo.Viewport.Height * 0.825f;
        // Default FOV is 63.2. Config default is 90 so we need to convert. (90 - 63.2 = 26.8).
        float fovY = (float)MathHelper.ToRadians(renderInfo.Config.FieldOfView - 26.8);
        return new(w, h, fovY);
    }

    public static float GetZNear(RenderInfo renderInfo)
    {
        if (ShaderVars.ReversedZ)
            return ReversedZNear;

        // Optimally this should be handled in the shader. Setting this variable and using it for a low zNear is good enough for now.
        // If we are being crushed or clipped into a line with a middle texture then use a lower zNear.
        float zNear = (float)((renderInfo.ViewerEntity.LowestCeilingZ - renderInfo.ViewerEntity.HighestFloorZ - renderInfo.ViewerEntity.ViewZ) * 0.68);
        var player = renderInfo.ViewerEntity.PlayerObj;
        if (player != null && (player.ViewLineClip || player.ViewPlaneClip))
            zNear = ZNearMin;
        if (renderInfo.Config.FieldOfView > 100)
            zNear = Math.Min(zNear, 6);

        float aspectRatio = renderInfo.Viewport.Width / (float)renderInfo.Viewport.Height;
        if (aspectRatio > 1.78f)
            zNear = Math.Min(zNear, 2.2f + 2.2f * (3.5555f - aspectRatio));

        return MathHelper.Clamp(zNear, ZNearMin, ZNearMax);
    }

    public static float GetDistanceOffset(RenderInfo renderInfo) =>
        (ZNearMax - GetZNear(renderInfo)) * 2;

    private void UpdateFramebufferDimensionsIfNeeded()
    {
        if (m_mainFramebuffer.Dimension != Window.Dimension && Window.Dimension.HasPositiveArea)
        {
            m_mainFramebuffer.Dispose();
            m_mainFramebuffer = GenerateMainFramebuffer();
        }
        if (m_virtualFramebuffer.Dimension != RenderDimension && RenderDimension.HasPositiveArea)
        {
            m_virtualFramebuffer.Dispose();
            m_virtualFramebuffer = GenerateVirtualFramebuffer();
        }
        m_transitionRenderer.UpdateFramebufferDimensionsIfNeeded();
    }

    public void Render(RenderCommands renderCommands)
    {
        m_hudRenderer.Clear();
        UpdateFramebufferDimensionsIfNeeded();
        m_virtualFramebuffer.Bind();
        BindColorMapBuffer();
        BindSectorColorMapBuffer();
        BindLightBuffer();

        // This has to be tracked beyond just the rendering command, and it
        // also prevents something from going terribly wrong if there is no
        // call to setting the viewport.
        bool virtualFrameBufferDraw = false;
        for (int i = 0; i < renderCommands.Commands.Count; i++)
        {
            RenderCommand cmd = renderCommands.Commands[i];
            switch (cmd.Type)
            {
                case RenderCommandType.Image:
                    HandleDrawImage(renderCommands.ImageCommands[cmd.Index]);
                    break;
                case RenderCommandType.Shape:
                    HandleDrawShape(renderCommands.ShapeCommands[cmd.Index]);
                    break;
                case RenderCommandType.Text:
                    HandleDrawText(renderCommands.TextCommands[cmd.Index]);
                    break;
                case RenderCommandType.Clear:
                    HandleClearCommand(renderCommands.ClearCommands[cmd.Index]);
                    break;
                case RenderCommandType.World:
                    HandleRenderWorldCommand(renderCommands.WorldCommands[cmd.Index], m_viewport);
                    break;
                case RenderCommandType.Automap:
                    HandleRenderAutomapCommand(renderCommands.AutomapCommands[cmd.Index], m_viewport);
                    break;
                case RenderCommandType.Hud:
                    DrawHudImagesIfAnyQueued(m_viewport, m_renderInfo.Uniforms);
                    break;
                case RenderCommandType.Viewport:
                    HandleViewportCommand(renderCommands.ViewportCommands[cmd.Index], out m_viewport);
                    break;
                case RenderCommandType.DrawVirtualFrameBuffer:
                    virtualFrameBufferDraw = true;
                    BlitVirtualFramebufferToMain();
                    break;
                case RenderCommandType.Transition:
                    var tranCmd = renderCommands.TransitionCommands[cmd.Index];
                    if (tranCmd.Init == true)
                        m_transitionRenderer.PrepareNewTransition(m_mainFramebuffer, tranCmd.Type);
                    if (tranCmd.Progress.HasValue)
                        m_transitionRenderer.Render(m_mainFramebuffer, tranCmd.Progress.Value);
                    break;
                default:
                    Fail($"Unsupported render command type: {cmd.Type}");
                    break;
            }
        }

        DrawHudImagesIfAnyQueued(m_viewport, m_renderInfo.Uniforms);

        if (!virtualFrameBufferDraw)
            BlitVirtualFramebufferToMain();

        // draw main framebuffer to default
        // BlitMainFramebufferToDefault();
        GL.BindFramebuffer(FramebufferTarget.DrawFramebuffer, 0);
        m_framebufferRenderer.Render(m_mainFramebuffer);
    }

    private void BindColorMapBuffer()
    {
        m_colorMapBuffer?.BindTexture(TextureUnit.Texture2);
    }

    private void BindSectorColorMapBuffer()
    {
        m_sectorColorMapsBuffer?.BindTexture(TextureUnit.Texture3);
    }

    private void BindLightBuffer()
    {
        m_lightBufferStorage?.BindTexture(TextureUnit.Texture1);
    }

    public void PerformThrowableErrorChecks()
    {
        if (m_config.Developer.Render.Debug)
            GLHelper.AssertNoGLError();
    }

    public void FlushPipeline()
    {
        GL.Finish();
    }

    private static void PrintGLInfo()
    {
        if (InfoPrinted)
            return;

        Log.Info("OpenGL v{0}", GLVersion.Version);
        Log.Info("OpenGL Shading Language: {0}", GLInfo.ShadingVersion);
        Log.Info("OpenGL Vendor: {0}", GLInfo.Vendor);
        Log.Info("OpenGL Hardware: {0}", GLInfo.Renderer);
        Log.Info("OpenGL Extensions: {0}", GLExtensions.Count);
        Log.Info("GL_ARB_clip_control {0}", GLInfo.ClipControlSupported);
        Log.Info("MapPersistentBit {0}", GLInfo.MapPersistentBitSupported);

        InfoPrinted = true;
    }

    private void SetGLStates()
    {
        GL.Enable(EnableCap.DepthTest);

        if (m_config.Render.Multisample > 1)
            GL.Enable(EnableCap.Multisample);

        GL.Enable(EnableCap.TextureCubeMapSeamless);

        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        GL.Enable(EnableCap.CullFace);
        GL.FrontFace(FrontFaceDirection.Ccw);
        GL.CullFace(CullFaceMode.Back);
        GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
    }

    private void SetGLDebugger()
    {
        // Note: This means it's not set if `RenderDebug` changes. As far
        // as I can tell, we can't unhook actions, but maybe we could do
        // some glDebugControl... setting that changes them all to don't
        // cares if we have already registered a function? See:
        // https://www.khronos.org/opengl/wiki/GLAPI/glDebugMessageControl
        if (!GLExtensions.DebugOutput || !m_config.Developer.Render.Debug)
            return;

        GL.Enable(EnableCap.DebugOutput);
        GL.Enable(EnableCap.DebugOutputSynchronous);

        // TODO: We should filter messages we want to get since this could
        //       pollute us with lots of messages and we wouldn't know it.
        //       https://www.khronos.org/opengl/wiki/GLAPI/glDebugMessageControl
        GLHelper.DebugMessageCallback((level, message) =>
        {
            switch (level.Ordinal)
            {
                case 2:
                    Log.Warn("OpenGL minor issue: {0}", message);
                    return;
                case 3:
                    Log.Error("OpenGL warning: {0}", message);
                    return;
                case 4:
                    Log.Error("OpenGL major error: {0}", message);
                    return;
                default:
                    throw new ArgumentOutOfRangeException($"Unsupported enumeration debug callback: {level}");
            }
        });
    }

    public Image GetMainFramebufferData()
    {
        var (w, h, rgba) = GetMainFramebufferDataRaw();
        int pixelCount = w * h;
        uint[] argb = new uint[pixelCount];
        int offset = 0;
        for (int i = 0; i < pixelCount; i++)
        {
            uint r = rgba[offset];
            uint g = rgba[offset + 1];
            uint b = rgba[offset + 2];
            // ignore the original alpha channel
            argb[i] = 0xFF000000 | (r << 16) | (g << 8) | b;
            offset += 4;
        }

        var image = new Image(argb, (w, h), ImageType.Argb, (0, 0), Resources.ResourceNamespace.Global).FlipY();
        return image;
    }

    private unsafe (int width, int height, byte[] rgba) GetMainFramebufferDataRaw()
    {
        GL.Finish();
        (int w, int h) = m_mainFramebuffer.Dimension;
        byte[] rgba = new byte[w * h * 4];

        m_mainFramebuffer.BindRead();
        fixed (byte* rgbPtr = rgba)
        {
            IntPtr ptr = new(rgbPtr);
            GL.ReadPixels(0, 0, w, h, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
        }

        return (w, h, rgba);
    }

    private void HandleClearCommand(ClearRenderCommand clearRenderCommand)
    {
        Color color = clearRenderCommand.ClearColor;
        GL.ClearColor(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);

        ClearBufferMask clearMask = 0;
        if (clearRenderCommand.Color)
            clearMask |= ClearBufferMask.ColorBufferBit;
        if (clearRenderCommand.Depth)
            clearMask |= ClearBufferMask.DepthBufferBit;
        if (clearRenderCommand.Stencil)
            clearMask |= ClearBufferMask.StencilBufferBit;

        GL.Clear(clearMask);
    }

    private void HandleDrawImage(DrawImageCommand cmd)
    {
        if (cmd.AreaIsTextureDimension)
        {
            Vec2I topLeft = (cmd.DrawArea.Top, cmd.DrawArea.Left);
            m_hudRenderer.DrawImage(cmd.TextureName, cmd.ResourceNamespace, topLeft, cmd.MultiplyColor, cmd.Alpha, cmd.DrawColorMap, cmd.DrawFuzz, cmd.DrawPalette, cmd.ColorMapIndex);
        }
        else
            m_hudRenderer.DrawImage(cmd.TextureName, cmd.ResourceNamespace, cmd.DrawArea, cmd.MultiplyColor, cmd.Alpha, cmd.DrawColorMap, cmd.DrawFuzz, cmd.DrawPalette, cmd.ColorMapIndex);
    }

    private void HandleDrawShape(DrawShapeCommand cmd)
    {
        m_hudRenderer.DrawShape(cmd.Rectangle, cmd.Color, cmd.Alpha);
    }

    private void HandleDrawText(DrawTextCommand cmd)
    {
        m_hudRenderer.DrawText(cmd.Text, cmd.DrawArea, cmd.Alpha, cmd.DrawColorMap);
        var dataCache = m_archiveCollection.DataCache;
        dataCache.FreeRenderableString(cmd.Text);
    }

    private void HandleRenderAutomapCommand(DrawWorldCommand cmd, Rectangle viewport)
    {
        if (viewport.Width == 0 || viewport.Height == 0 || cmd.World.IsDisposed)
            return;

        var viewSector = cmd.World.BspTree.ToSector(cmd.Camera.PositionInterpolated.Double);
        var transferHeightsView = TransferHeights.GetView(viewSector, cmd.Camera.PositionInterpolated.Z);

        m_renderInfo.Set(cmd.Camera, cmd.GametickFraction, viewport, cmd.ViewerEntity, cmd.DrawAutomap,
            cmd.AutomapOffset, cmd.AutomapScale, m_config.Render, viewSector, transferHeightsView);

        m_automapRenderer.Render(cmd.World, m_renderInfo);
    }

    private void HandleRenderWorldCommand(DrawWorldCommand cmd, Rectangle viewport)
    {
        if (viewport.Width == 0 || viewport.Height == 0 || cmd.World.IsDisposed)
            return;

        var viewSector = cmd.World.BspTree.ToSector(cmd.Camera.PositionInterpolated.Double);
        var transferHeightsView = TransferHeights.GetView(viewSector, cmd.Camera.PositionInterpolated.Z);

        m_renderInfo.Set(cmd.Camera, cmd.GametickFraction, viewport, cmd.ViewerEntity, cmd.DrawAutomap,
            cmd.AutomapOffset, cmd.AutomapScale, m_config.Render, viewSector, transferHeightsView);
        m_renderInfo.Uniforms = GetShaderUniforms(m_config, cmd.World, m_renderInfo);

        DrawHudImagesIfAnyQueued(viewport, m_renderInfo.Uniforms);

        if (ShaderVars.ReversedZ)
        {
            GL.ClipControl(ClipOrigin.LowerLeft, ClipDepthMode.ZeroToOne);
            GL.DepthFunc(DepthFunction.Greater);
            GL.Enable(EnableCap.DepthTest);

            GL.ClearDepth(0.0);
            GL.Clear(ClearBufferMask.DepthBufferBit);
        }

        UpdateBuffers();
        m_worldRenderer.Render(cmd.World, m_renderInfo);

        if (ShaderVars.ReversedZ)
        {
            GL.ClipControl(ClipOrigin.LowerLeft, ClipDepthMode.NegativeOneToOne);
            GL.DepthFunc(DepthFunction.Less);
            GL.Disable(EnableCap.DepthTest);
        }
    }

    private static void HandleViewportCommand(ViewportCommand viewportCommand, out Rectangle viewport)
    {
        Vec2I offset = viewportCommand.Offset;
        Dimension dimension = viewportCommand.Dimension;
        viewport = new Rectangle(offset.X, offset.Y, dimension.Width, dimension.Height);

        GL.Viewport(offset.X, offset.Y, dimension.Width, dimension.Height);
    }

    private void DrawHudImagesIfAnyQueued(Rectangle viewport, ShaderUniforms uniforms)
    {
        m_hudRenderer.Render(viewport, uniforms);
        m_hudRenderer.Clear();
    }

    private void BlitVirtualFramebufferToMain()
    {
        var mainDimension = m_mainFramebuffer.Dimension;
        var virtualDimension = m_virtualFramebuffer.Textures[0].Dimension;
        float scaleX = (m_config.Window.Virtual.Stretch)
            ? 1f
            : Math.Min(virtualDimension.AspectRatio / mainDimension.AspectRatio, 1.0f);
        int destWidth = (int)(mainDimension.Width * scaleX);
        int offsetX = (mainDimension.Width - destWidth) / 2;
        var filterType = GetFilterType();

        m_mainFramebuffer.BindDraw();
        m_virtualFramebuffer.BindRead();
        GL.ClearColor(0, 0, 0, 1);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        GL.BlitFramebuffer(
            0, 0, virtualDimension.Width, virtualDimension.Height,
            offsetX, 0, offsetX + destWidth, mainDimension.Height,
            ClearBufferMask.ColorBufferBit, filterType);
    }

    private BlitFramebufferFilter GetFilterType()
    {
        if (m_config.Window.Virtual.Filter == BlitFilter.Auto)
        {
            return (m_config.Render.Filter.Texture == FilterType.Nearest)
                            ? BlitFramebufferFilter.Nearest
                            : BlitFramebufferFilter.Linear;
        }

        return (m_config.Window.Virtual.Filter == BlitFilter.Nearest)
                    ? BlitFramebufferFilter.Nearest
                    : BlitFramebufferFilter.Linear;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        m_mainFramebuffer.Dispose();
        m_virtualFramebuffer.Dispose();
        Textures.Dispose();
        m_hudRenderer.Dispose();
        m_worldRenderer.Dispose();
        m_framebufferRenderer.Dispose();
        m_automapRenderer.Dispose();
        m_lightBufferStorage?.Dispose();
        m_sectorColorMapsBuffer?.Dispose();
        m_transitionRenderer?.Dispose();

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
