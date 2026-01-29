using GlmSharp;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Graphics.Geometry;
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
using Helion.Render.OpenGL.Util;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Configs.Components;
using Helion.Util.Timing;
using Helion.Window;
using Helion.World;
using Helion.World.Entities;
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
    public static readonly Color OffBlackBackground = (16, 16, 16);
    public static readonly Color BlackBackground = (0, 0, 0);
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static bool InfoPrinted;

    public readonly IWindow Window;
    public readonly GLSurface Default;
    private GLFramebuffer m_mainFramebuffer;
    private GLFramebuffer? m_virtualFramebuffer;
    private GLFramebuffer m_worldFramebuffer;

    private readonly GLFramebuffer m_screenshotFramebuffer;
    public readonly LegacyGLTextureManager Textures;
    internal readonly IConfig m_config;
    internal readonly FpsTracker m_fpsTracker;
    internal readonly ArchiveCollection m_archiveCollection;
    private readonly WorldRenderer m_worldRenderer;
    private readonly HudRenderer m_hudRenderer;
    private readonly RenderInfo m_renderInfo = new();
    private readonly FramebufferRenderer m_framebufferRenderer = new();
    private readonly LegacyAutomapRenderer m_automapRenderer;
    private readonly TransitionRenderer m_transitionRenderer;
    private readonly Image m_framebufferImage = new([], (0, 0), ImageType.Rgba, (0, 0), Resources.ResourceNamespace.Global);
    private uint[] m_imageRowFlip = [];

    private IWorld? m_world;
    private Rectangle m_viewport = new(0, 0, 800, 600);
    private uint[] m_frameBufferPixelData = [];
    private bool m_disposed;
    private bool m_useVirtualBuffer;
    private bool m_vanillaRender;
    private DrawWorldCommand m_lastDrawWorldCmd;
    private TextureMinFilter m_virtualMinFilter;
    private TextureMagFilter m_virtualMagFilter;

    public Dimension RenderDimension => UseVirtualResolution ? m_config.Window.Virtual.Dimension : Window.ClientDimension;
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
        m_transitionRenderer = new TransitionRenderer(window);
        Default = new(window, this);
        m_useVirtualBuffer = ShouldUseVirtualBuffer();
        m_mainFramebuffer = GenerateMainFramebuffer();
        m_virtualFramebuffer = GenerateVirtualFramebuffer();
        m_worldFramebuffer = m_mainFramebuffer;
        // Temporary frame buffer for smaller save game screenshots. Significantly faster than pulling the full sized pixel buffer and downsizing using image sharp.
        m_screenshotFramebuffer = new("Screenshot", (Constants.ScreenshotSaveWidth, Constants.ScreenshotSaveHeight), 1);

        m_config.Render.PixelGapCorrection.OnChanged += PixelGapCorrection_OnChanged;

        m_vanillaRender = m_config.Render.VanillaRender;

        SetPixelGapCorrection(m_config.Render.PixelGapCorrection.Value);

        PrintGLInfo();
        SetGLStates();
    }

    private mat4 CalculateVirtualMvp(GLFramebuffer buffer, Dimension bufferDimension)
    {
        // If stretching or dimensions match then it's always Identity.
        if (bufferDimension == Window.ClientDimension || m_config.Window.Virtual.Stretch)
            return mat4.Identity;

        // We already draw to the unit plane, which means instead of doing a bunch
        // of orthographic stuff, we can instead scale the X axis to add black bars
        // depending on whether we want stretched or widescreen.

        // How much we stretch depends on the window resolution, and the virtual
        // dimension's resolution. Also don't let it be larger than the NDC box.
        // Since our vertices are in NDC coordinates, 1.0 is the max we can go.
        var windowDim = Window.ClientDimension;
        var textureDim = buffer.ColorAttachment0.Dimension;
        var scaleX = Math.Min(textureDim.AspectRatio / windowDim.AspectRatio, 1.0f);

        return mat4.Scale(scaleX, 1.0f, 1.0f);
    }

    private void UpdateVirtualTextureFilter(GLFramebuffer buffer)
    {
        if (m_virtualFramebuffer == null)
            return;

        CalculateBufferTextureFilter(m_virtualFramebuffer, m_config.Window.Virtual.Filter, out var magFilter, out var minFilter);
        if (magFilter == m_virtualMagFilter && minFilter == m_virtualMinFilter)
            return;

        m_virtualMagFilter = magFilter;
        m_virtualMinFilter = minFilter;

        buffer.ColorAttachment0.Bind();
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMagFilter, (int)magFilter);
        GL.TexParameter(TextureTarget.Texture2D, TextureParameterName.TextureMinFilter, (int)minFilter);
    }

    private void CalculateBufferTextureFilter(GLFramebuffer buffer, VirtualDrawFilter filter, out TextureMagFilter magFilter, out TextureMinFilter minFilter)
    {
        buffer.ColorAttachment0.Bind();
        var isDimensionMatch = buffer.Dimension == Window.ClientDimension;
        var filterNearest = (
            isDimensionMatch
            || filter == VirtualDrawFilter.Nearest
            || (filter == VirtualDrawFilter.Auto && m_config.Render.Filter.Texture == FilterType.Nearest)
        );
        magFilter = (filterNearest ? TextureMagFilter.Nearest : TextureMagFilter.Linear);
        minFilter = (filterNearest ? TextureMinFilter.Nearest : TextureMinFilter.Linear);
    }

    private void PixelGapCorrection_OnChanged(object? sender, bool e) => SetPixelGapCorrection(e);
    private static void SetPixelGapCorrection(bool set)
    {
        if (set)
        {
            WorldStatic.LineVertexGap = Constants.VertexGapPush;
            WorldStatic.LineVertexOffset = -(float)Constants.VertexGapPush;
            WorldStatic.CoverWallOffset = -(float)Constants.VertexGapPush * 2;
        }
        else
        {
            WorldStatic.LineVertexGap = 0;
            WorldStatic.LineVertexOffset = 0;
            WorldStatic.CoverWallOffset = 0;
        }
    }

    private GLFramebuffer GenerateMainFramebuffer()
    {
        // Depth attachment only required if not using the virtual buffer. This only happens with render.postprocessingeffects = 0.
        return new("Main", Window.ClientDimension, 1, 
            ShouldUseVirtualBuffer() ? GLFrameBufferOptions.None : GLFrameBufferOptions.DepthStencilAttachment, 
            mainBackBuffer: true);
    }

    private GLFramebuffer? GenerateVirtualFramebuffer()
    {
        if (!ShouldUseVirtualBuffer())
            return null;

        return new("Virtual", RenderDimension, 1, GLFrameBufferOptions.DepthStencilAttachment);
    }

    private bool ShouldUseVirtualBuffer()
    {
        var useVirtual = m_config.Window.Virtual.Enable && m_config.Window.Virtual.Dimension != Window.ClientDimension;
        if (useVirtual)
            return true;

        // Requires FBO to sample pixels from the color attachment. Direct write to backbuffer can't be used.
        if (m_config.Render.PostProcessingEffects.Value)
            return true;

        // This value is cached since it doesn't take affect until a new world is loaded.
        if (!m_vanillaRender)
            return false;

        // Software sprite clipping emulate requires an FBO for MRT rendering and cannot use the backbuffer directly.
        return m_config.Render.DownScaleVanillaRenderSampleBuffer.Value <= 1;
    }

    private Dimension GetVirtualDimension()
    {
        var useVirtual = m_config.Window.Virtual.Enable && m_config.Window.Virtual.Dimension != Window.ClientDimension;
        if (useVirtual)
            return m_config.Window.Virtual.Dimension;

        return Window.ClientDimension;
    }

    public unsafe void UploadColorMap()
    {
        if (!(ShaderVars.PaletteColorMode || ShaderVars.EmulateInvulnerabilityColorMap))
            return;

        var colorMapData = ColorMapBuffer.Create(m_archiveCollection.Palette, m_archiveCollection.Definitions.Colormaps);
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
        ShaderVars.EmulateInvulnerabilityColorMap = m_config.Render.EmulateInvulnerabilityColorMap;
    }

    private void SetReverseZ()
    {
        if (!m_config.Developer.UseReversedZ)
        {
            // Not possible if post processing effects are disabled since it skips the intermediate FBO
            if (GLInfo.ClipControlSupported && !m_config.Render.PostProcessingEffects)
                Log.Warn("Post processing effects disabled: Not using reverse Z projection.");
            ShaderVars.ReversedZ = GLInfo.ClipControlSupported && m_config.Render.PostProcessingEffects;
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
        return viewport.Height / 480f / 2 * (float)config.FuzzAmount;
    }

    public static ShaderUniforms GetShaderUniforms(IConfig config, RenderInfo renderInfo)
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
            if (!player.DrawInvulnerableColorMap() && player.DrawFullBright())
                mix = 1.0f;
            if (player.DrawInvulnerableColorMap())
                drawInvulnerability = true;

            extraLight = player.GetExtraLightRender();

            if (ShaderVars.PaletteColorMode)
            {
                colorMapUniforms = GetColorMapUniforms(renderInfo.ViewerEntity, renderInfo.Camera);

                if (!config.Window.PaletteTrueColorOverlay)
                {
                    mix = 0.0f;
                    paletteIndex = PaletteUtil.GetPalette(config, player);
                }
            }
        }

        int maxDistance = config.Render.MaxDistance.Value;
        if (maxDistance <= 0)
            maxDistance = Constants.DefaultMaxDistance;

        return new ShaderUniforms(
            CalculateMvpMatrix(renderInfo),
            CalculateMvpMatrix(renderInfo, true),
            GetTimeFrac(),
            drawInvulnerability,
            mix,
            extraLight,
            GetDistanceOffset(renderInfo),
            colorMix,
            GetFuzzDiv(renderInfo.Config, renderInfo.Viewport),
            colorMapUniforms,
            paletteIndex,
            config.Render.LightMode,
            (float)config.Render.GammaCorrection,
            maxDistance,
            config.Render.Brightmaps,
            GetDownScaleAmount(config, renderInfo));
    }

    private static float GetDownScaleAmount(IConfig config, RenderInfo renderInfo)
    {
        if (renderInfo.Viewport.Height <= 480)
            return 1;

        return (float)config.Render.DownScaleVanillaRenderSampleBuffer;
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
        var useVirtualBuffer = ShouldUseVirtualBuffer();

        if (Window.ClientDimension.HasPositiveArea && 
            (m_mainFramebuffer.Dimension != Window.ClientDimension || m_useVirtualBuffer != useVirtualBuffer))
        {
            m_mainFramebuffer.Dispose();
            m_mainFramebuffer = GenerateMainFramebuffer();
        }

        if (RenderDimension.HasPositiveArea && 
            ((useVirtualBuffer && m_virtualFramebuffer == null) || 
            (m_virtualFramebuffer != null && !useVirtualBuffer) || 
            (m_virtualFramebuffer != null && m_virtualFramebuffer.Dimension != GetVirtualDimension())))
        {
            m_virtualFramebuffer?.Dispose();
            m_virtualFramebuffer = GenerateVirtualFramebuffer();
        }

        if (useVirtualBuffer && m_virtualFramebuffer != null)
            m_worldFramebuffer = m_virtualFramebuffer;
        else
            m_worldFramebuffer = m_mainFramebuffer;

        m_transitionRenderer.UpdateFramebufferDimensionsIfNeeded();
        m_useVirtualBuffer = useVirtualBuffer;
    }

    public void Render(RenderCommands renderCommands)
    {
        m_hudRenderer.Clear();
        UpdateFramebufferDimensionsIfNeeded();
        m_worldFramebuffer.Bind();
        BindColorMapBuffer();
        BindSectorColorMapBuffer();
        BindLightBuffer();
        BindMapDataBuffer();
        BindLineHeightsBuffer();

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
                    DrawVirtualFramebufferToMain();
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

        m_mainFramebuffer.Bind();
        DrawHudImagesIfAnyQueued(m_viewport, m_renderInfo.Uniforms);

        if (!virtualFrameBufferDraw)
            DrawVirtualFramebufferToMain();
    }

    private void BindColorMapBuffer()
    {
        m_colorMapBuffer?.BindTexture(BindTextures.Colormap);
    }

    private void BindSectorColorMapBuffer()
    {
        m_sectorColorMapsBuffer?.BindTexture(BindTextures.SectorColormap);
    }

    private void BindLightBuffer()
    {
        m_lightBufferStorage?.BindTexture(BindTextures.SectorLight);
    }

    private void BindMapDataBuffer()
    {
        m_mapDataBuffer?.BindTexture(BindTextures.MapLineData);
    }

    private void BindLineHeightsBuffer()
    {
        m_lineHeightsBuffer?.BindTexture(BindTextures.LineHeights);
    }

    public void PerformThrowableErrorChecks()
    {
        if (m_config.Developer.Render.Debug)
            GLHelper.AssertNoGLError();
    }

    public static void FlushPipeline()
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
        Log.Info("GL_ARB_shader_image_load_store {0}", GLInfo.MemoryBarrierSupported);
        Log.Info("GL_ARB_buffer_storage {0}", GLInfo.MapPersistentBitSupported);

        InfoPrinted = true;
    }

    private static void SetGLStates()
    {
        GL.Enable(EnableCap.DepthTest);

        GL.Enable(EnableCap.TextureCubeMapSeamless);

        GL.Enable(EnableCap.Blend);
        GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

        GL.Enable(EnableCap.CullFace);
        GL.FrontFace(FrontFaceDirection.Ccw);
        GL.CullFace(TriangleFace.Back);
        GL.PolygonMode(TriangleFace.FrontAndBack, PolygonMode.Fill);

        // Required for uv clamping in the vertex shader for pixel gap correction
        GL.ProvokingVertex(ProvokingVertexMode.FirstVertexConvention);
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

    public Image GetMainFramebufferData() => GenerateFrameBufferImage(m_mainFramebuffer);

    public Image GetScreenshotFrameBufferData()
    {
        // Need to re-render the world if everything is being draw to the world framebuffer
        if (m_worldFramebuffer.IsMainBackBuffer && m_lastDrawWorldCmd.World != null)
            HandleRenderWorldCommand(m_lastDrawWorldCmd, m_viewport);

        BlitToScreenshotBuffer();
        return GenerateFrameBufferImage(m_screenshotFramebuffer);
    }

    private Image GenerateFrameBufferImage(GLFramebuffer framebuffer)
    {
        var (w, h, rgba) = GetFramebufferDataRaw(framebuffer);
        if (w > m_imageRowFlip.Length)
            m_imageRowFlip = new uint[Math.Max(w, m_mainFramebuffer.Dimension.Width)];

        // OpenGL returns the Y pixel rows flipped
        var rowSize = w;
        for (int y = 0; y < h / 2; y++)
        {
            var topRowIndex = y * rowSize;
            var bottomRowIndex = (h - y - 1) * rowSize;

            Array.Copy(rgba, topRowIndex, m_imageRowFlip, 0, rowSize);
            Array.Copy(rgba, bottomRowIndex, rgba, topRowIndex, rowSize);
            Array.Copy(m_imageRowFlip, 0, rgba, bottomRowIndex, rowSize);
        }

        m_framebufferImage.SetPixels(rgba, (w, h));
        return m_framebufferImage;
    }

    private unsafe (int width, int height, uint[] rgba) GetFramebufferDataRaw(GLFramebuffer framebuffer)
    {
        GL.Finish();
        (int w, int h) = framebuffer.Dimension;
        if (m_frameBufferPixelData.Length < w * h)
        {
            // Keep array for framebuffer pixel data. Used for savegame and main buffer screenshots. Take the maximum to not realloc later.
            var allocWidth = Math.Max(w, m_mainFramebuffer.Dimension.Width);
            var allocHeight = Math.Max(h, m_mainFramebuffer.Dimension.Height);
            m_frameBufferPixelData = new uint[allocWidth * allocHeight];
        }

        framebuffer.BindRead();
        fixed (uint* rgbPtr = m_frameBufferPixelData)
        {
            IntPtr ptr = new(rgbPtr);
            GL.ReadPixels(0, 0, w, h, PixelFormat.Rgba, PixelType.UnsignedByte, ptr);
        }

        return (w, h, m_frameBufferPixelData);
    }

    private static void HandleClearCommand(ClearRenderCommand clearRenderCommand)
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
        ImageBox2I? crop = cmd.HasCrop ? cmd.CropArea : null;

        if (cmd.AreaIsTextureDimension)
        {
            Vec2I topLeft = (cmd.DrawArea.Top, cmd.DrawArea.Left);
            m_hudRenderer.DrawImage(
                cmd.TextureName, 
                cmd.ResourceNamespace, 
                topLeft, 
                cmd.MultiplyColor, 
                cmd.Alpha, 
                cmd.DrawColorMap, 
                cmd.DrawFuzz, 
                cmd.DrawPalette, 
                cmd.ColorMapIndex, 
                cmd.BrightmapName, 
                crop);
        }
        else
        {
            m_hudRenderer.DrawImage(
                cmd.TextureName, 
                cmd.ResourceNamespace, 
                cmd.DrawArea, 
                cmd.MultiplyColor, 
                cmd.Alpha, 
                cmd.DrawColorMap, 
                cmd.DrawFuzz, 
                cmd.DrawPalette, 
                cmd.ColorMapIndex, 
                cmd.BrightmapName, 
                crop);
        }
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

        var viewSector = cmd.World.ToSubsector(cmd.Camera.PositionInterpolated.X, cmd.Camera.PositionInterpolated.Y).Sector;
        var transferHeightsView = TransferHeights.GetView(viewSector, cmd.Camera.PositionInterpolated.Z);

        m_renderInfo.Set(cmd.Camera, cmd.GametickFraction, viewport, cmd.ViewerEntity, cmd.DrawAutomap,
            cmd.AutomapOffset, cmd.AutomapScale, m_config.Render, viewSector, transferHeightsView);

        m_automapRenderer.Render(cmd.World, m_renderInfo);
    }

    private void HandleRenderWorldCommand(DrawWorldCommand cmd, Rectangle viewport)
    {
        if (viewport.Width == 0 || viewport.Height == 0 || cmd.World.IsDisposed)
            return;

        var viewSector = cmd.World.ToSubsector(cmd.Camera.PositionInterpolated.X, cmd.Camera.PositionInterpolated.Y).Sector;
        var transferHeightsView = TransferHeights.GetView(viewSector, cmd.Camera.PositionInterpolated.Z);

        m_renderInfo.Set(cmd.Camera, cmd.GametickFraction, viewport, cmd.ViewerEntity, cmd.DrawAutomap,
            cmd.AutomapOffset, cmd.AutomapScale, m_config.Render, viewSector, transferHeightsView);
        m_renderInfo.Uniforms = GetShaderUniforms(m_config, m_renderInfo);

        DrawHudImagesIfAnyQueued(viewport, m_renderInfo.Uniforms);

        if (ShaderVars.ReversedZ)
        {
            GL.ClipControl(ClipOrigin.LowerLeft, ClipDepthMode.ZeroToOne);
            GL.DepthFunc(DepthFunction.Greater);
            GL.Enable(EnableCap.DepthTest);
            GL.ClearDepth(0.0);
        }

        m_lastDrawWorldCmd = cmd;
        UpdateBuffers();
        m_worldRenderer.Render(cmd.World, m_renderInfo, m_worldFramebuffer);

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
        // Bind main buffer for fuzz refraction sampling when player has partial invisibility
        if (m_virtualFramebuffer != null)
        {
            GL.ActiveTexture(BindTextures.OpaqueTexture);
            GL.BindTexture(TextureTarget.Texture2D, m_virtualFramebuffer.ColorAttachment0.Name);
        }
        m_hudRenderer.Render(viewport, m_mainFramebuffer.Dimension, m_virtualFramebuffer?.Dimension ?? m_mainFramebuffer.Dimension, uniforms);
        m_hudRenderer.Clear();
    }

    private void BlitToScreenshotBuffer()
    {
        var screenshotDimension = m_screenshotFramebuffer.Dimension;
        var virtualDimension = m_worldFramebuffer.Dimension;
        float scaleX = Math.Min(screenshotDimension.AspectRatio / virtualDimension.AspectRatio, 1.0f);
        int sourceWidth = (int)(virtualDimension.Width * scaleX);
        int offsetX = (virtualDimension.Width - sourceWidth) / 2;

        m_screenshotFramebuffer.BindDraw();
        m_worldFramebuffer.BindRead();
        GL.ClearColor(0, 0, 0, 1);
        GL.Clear(ClearBufferMask.ColorBufferBit);
        GL.BlitFramebuffer(
            offsetX, 0, offsetX + sourceWidth, virtualDimension.Height,
            0, 0, screenshotDimension.Width, screenshotDimension.Height,
            ClearBufferMask.ColorBufferBit, BlitFramebufferFilter.Linear);
    }

    private void DrawVirtualFramebufferToMain()
    {
        if (m_worldFramebuffer == m_mainFramebuffer || m_virtualFramebuffer == null)
            return;

        m_mainFramebuffer.BindDraw();
        UpdateVirtualTextureFilter(m_virtualFramebuffer);
        GL.Clear(ClearBufferMask.DepthBufferBit);
        m_framebufferRenderer.Render(m_virtualFramebuffer, CalculateVirtualMvp(m_virtualFramebuffer, GetVirtualDimension()));
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        m_config.Render.PixelGapCorrection.OnChanged -= PixelGapCorrection_OnChanged;

        m_mainFramebuffer.Dispose();
        m_virtualFramebuffer?.Dispose();
        Textures.Dispose();
        m_hudRenderer.Dispose();
        m_worldRenderer.Dispose();
        m_framebufferRenderer.Dispose();
        m_automapRenderer.Dispose();
        m_lightBufferStorage?.Dispose();
        m_sectorColorMapsBuffer?.Dispose();
        m_transitionRenderer?.Dispose();
        m_mapDataBuffer?.Dispose();
        m_lineHeightsBuffer?.Dispose();

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
