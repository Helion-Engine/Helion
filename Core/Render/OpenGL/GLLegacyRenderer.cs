using System;
using System.Diagnostics;
using System.Drawing;
using GlmSharp;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Render.Common.Renderers;
using Helion.Render.Common.Textures;
using Helion.Render.OpenGL.Commands;
using Helion.Render.OpenGL.Commands.Types;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;
using Helion.Render.OpenGL.Renderers;
using Helion.Render.OpenGL.Renderers.Legacy.Hud;
using Helion.Render.OpenGL.Renderers.Legacy.World;
using Helion.Render.OpenGL.Shared;
using Helion.Render.OpenGL.Texture;
using Helion.Render.OpenGL.Texture.Legacy;
using Helion.Render.OpenGL.Util;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Timing;
using Helion.Window;
using NLog;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL;

public class GLLegacyRenderer : IRenderer
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static bool InfoPrinted;

    public IWindow Window { get; }
    public IRenderableSurface DefaultSurface => Default;
    public IRendererTextureManager Textures => m_textureManager;
    public IRenderableSurface Default { get; }
    internal readonly IConfig m_config;
    internal readonly FpsTracker m_fpsTracker;
    internal readonly ArchiveCollection m_archiveCollection;
    private readonly GLCapabilities m_capabilities;
    private readonly IGLFunctions gl;
    private readonly IGLTextureManager m_textureManager;
    private readonly WorldRenderer m_worldRenderer;
    private readonly HudRenderer m_hudRenderer;
    private readonly RenderInfo m_renderInfo = new();

    public IImageDrawInfoProvider ImageDrawInfoProvider => m_textureManager.ImageDrawInfoProvider;

    public GLLegacyRenderer(IWindow window, IConfig config, ArchiveCollection archiveCollection, IGLFunctions functions,
        FpsTracker fpsTracker)
    {
        Window = window;
        gl = functions;
        m_capabilities = new GLCapabilities(functions);
        m_config = config;
        m_archiveCollection = archiveCollection;
        m_fpsTracker = fpsTracker;

        GLRenderType renderType = GetRenderTypeFromCapabilities();
        m_textureManager = CreateTextureManager(renderType, archiveCollection);
        m_worldRenderer = CreateWorldRenderer(renderType);
        m_hudRenderer = CreateHudRenderer(renderType);

        Default = new GLSurface(window, this);

        PrintGLInfo(m_capabilities);
        SetGLDebugger();
        SetGLStates();
        WarnForInvalidStates(config);
    }

    ~GLLegacyRenderer()
    {
        FailedToDispose(this);
        ReleaseUnmanagedResources();
    }

    public static mat4 CalculateMvpMatrix(RenderInfo renderInfo, bool onlyXY = false)
    {
        float w = renderInfo.Viewport.Width;
        float h = renderInfo.Viewport.Height * 0.825f;
        // Default FOV is 63.2. Config default is 90 so we need to convert. (90 - 63.2 = 26.8).
        float fovY = (float)MathHelper.ToRadians(renderInfo.Config.FieldOfView - 26.8);

        mat4 model = mat4.Identity;
        mat4 view = renderInfo.Camera.CalculateViewMatrix(onlyXY);

        // Optimially this should be handled in the shader. Setting this variable and using it for a low zNear is good enough for now.
        // If we are being crushed or clipped into a line with a middle texture then use a lower zNear.
        float zNear = (float)((renderInfo.ViewerEntity.LowestCeilingZ - renderInfo.ViewerEntity.HighestFloorZ - renderInfo.ViewerEntity.ViewZ) * 0.68);
        if (renderInfo.ViewerEntity.ViewLineClip)
            zNear = 0.2f;

        zNear = MathHelper.Clamp(zNear, 0.2f, 7.9f);
        mat4 projection = mat4.PerspectiveFov(fovY, w, h, zNear, 65536.0f);
        return projection * view * model;
    }

    private static void WarnForInvalidStates(IConfig config)
    {
        if (config.Render.Anisotropy > 1 && config.Render.Filter.Texture.Value != FilterType.Trilinear)
            Log.Warn($"Anisotropic filter should be paired with trilinear filtering (you have {config.Render.Filter.Texture.Value}), you will not get the best results!");
    }

    public IRenderableSurface GetOrCreateSurface(string name, Dimension dimension) => Default;

    public void Render(RenderCommands renderCommands)
    {
        m_hudRenderer.Clear();

        // This has to be tracked beyond just the rendering command, and it
        // also prevents something from going terribly wrong if there is no
        // call to setting the viewport.
        Rectangle viewport = new(0, 0, 800, 600);
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
                    HandleRenderWorldCommand(renderCommands.WorldCommands[cmd.Index], viewport);
                    break;
                case RenderCommandType.Viewport:
                    HandleViewportCommand(renderCommands.ViewportCommands[cmd.Index], out viewport);
                    break;
                default:
                    Fail($"Unsupported render command type: {cmd.Type}");
                    break;
            }
        }

        DrawHudImagesIfAnyQueued(viewport);
    }

    public void PerformThrowableErrorChecks()
    {
        if (m_config.Developer.Render.Debug)
            GLHelper.AssertNoGLError(gl);
    }

    public void FlushPipeline()
    {
        GL.Finish();
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    private static void PrintGLInfo(GLCapabilities capabilities)
    {
        if (InfoPrinted)
            return;

        Log.Info("OpenGL v{0}", capabilities.Version);
        Log.Info("OpenGL Shading Language: {0}", capabilities.Info.ShadingVersion);
        Log.Info("OpenGL Vendor: {0}", capabilities.Info.Vendor);
        Log.Info("OpenGL Hardware: {0}", capabilities.Info.Renderer);
        Log.Info("OpenGL Extensions: {0}", capabilities.Extensions.Count);

        InfoPrinted = true;
    }

    private void SetGLStates()
    {
        gl.Enable(EnableType.DepthTest);

        if (m_config.Render.Multisample > 1)
            gl.Enable(EnableType.Multisample);

        if (m_capabilities.Version.Supports(3, 2))
            gl.Enable(EnableType.TextureCubeMapSeamless);

        gl.Enable(EnableType.Blend);
        gl.BlendFunc(BlendingFactorType.SrcAlpha, BlendingFactorType.OneMinusSrcAlpha);

        gl.Enable(EnableType.CullFace);
        gl.FrontFace(FrontFaceType.CounterClockwise);
        gl.CullFace(CullFaceType.Back);
        gl.PolygonMode(PolygonFaceType.FrontAndBack, PolygonModeType.Fill);
    }

    [Conditional("DEBUG")]
    private void SetGLDebugger()
    {
        // Note: This means it's not set if `RenderDebug` changes. As far
        // as I can tell, we can't unhook actions, but maybe we could do
        // some glDebugControl... setting that changes them all to don't
        // cares if we have already registered a function? See:
        // https://www.khronos.org/opengl/wiki/GLAPI/glDebugMessageControl
        if (!m_capabilities.Version.Supports(4, 3) || !m_config.Developer.Render.Debug)
            return;

        gl.Enable(EnableType.DebugOutput);
        gl.Enable(EnableType.DebugOutputSynchronous);

        // TODO: We should filter messages we want to get since this could
        //       pollute us with lots of messages and we wouldn't know it.
        //       https://www.khronos.org/opengl/wiki/GLAPI/glDebugMessageControl
        gl.DebugMessageCallback((level, message) =>
        {
            switch (level)
            {
            case DebugLevel.Low:
                Log.Warn("OpenGL minor issue: {0}", message);
                return;
            case DebugLevel.Medium:
                Log.Error("OpenGL warning: {0}", message);
                return;
            case DebugLevel.High:
                Log.Error("OpenGL major error: {0}", message);
                return;
            default:
                throw new ArgumentOutOfRangeException($"Unsupported enumeration debug callback: {level}");
            }
        });
    }

    private GLRenderType GetRenderTypeFromCapabilities()
    {
        if (m_capabilities.Version.Supports(3, 1))
        {
            Log.Info("Using legacy OpenGL renderer");
            return GLRenderType.Legacy;
        }

        throw new HelionException("OpenGL implementation too old or not supported");
    }

    private IGLTextureManager CreateTextureManager(GLRenderType renderType, ArchiveCollection archiveCollection)
    {
        switch (renderType)
        {
        case GLRenderType.Modern:
            throw new NotImplementedException("Modern GL renderer not implemented yet");
        case GLRenderType.Standard:
            throw new NotImplementedException("Standard GL renderer not implemented yet");
        default:
            return new LegacyGLTextureManager(m_config, m_capabilities, gl, archiveCollection);
        }
    }

    private WorldRenderer CreateWorldRenderer(GLRenderType renderType)
    {
        switch (renderType)
        {
        case GLRenderType.Modern:
            throw new NotImplementedException("Modern GL renderer not implemented yet");
        case GLRenderType.Standard:
            throw new NotImplementedException("Standard GL renderer not implemented yet");
        default:
            Precondition(m_textureManager is LegacyGLTextureManager, "Created wrong type of texture manager (should be legacy)");
            return new LegacyWorldRenderer(m_config, m_archiveCollection, m_capabilities, gl, (LegacyGLTextureManager)m_textureManager);
        }
    }

    private HudRenderer CreateHudRenderer(GLRenderType renderType)
    {
        switch (renderType)
        {
        case GLRenderType.Modern:
            throw new NotImplementedException("Modern GL renderer not implemented yet");
        case GLRenderType.Standard:
            throw new NotImplementedException("Standard GL renderer not implemented yet");
        default:
            Precondition(m_textureManager is LegacyGLTextureManager, "Created wrong type of texture manager (should be legacy)");
            return new LegacyHudRenderer(m_capabilities, gl, (LegacyGLTextureManager)m_textureManager, m_archiveCollection.DataCache);
        }
    }

    private void HandleClearCommand(ClearRenderCommand clearRenderCommand)
    {
        Color color = clearRenderCommand.ClearColor;
        gl.ClearColor(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);

        ClearType clearMask = 0;
        if (clearRenderCommand.Color)
            clearMask |= ClearType.ColorBufferBit;
        if (clearRenderCommand.Depth)
            clearMask |= ClearType.DepthBufferBit;
        if (clearRenderCommand.Stencil)
            clearMask |= ClearType.StencilBufferBit;

        gl.Clear(clearMask);
    }

    private void HandleDrawImage(DrawImageCommand cmd)
    {
        if (cmd.AreaIsTextureDimension)
        {
            Vec2I topLeft = (cmd.DrawArea.Top, cmd.DrawArea.Left);
            m_hudRenderer.DrawImage(cmd.TextureName, topLeft, cmd.MultiplyColor, cmd.Alpha, cmd.DrawInvulnerability);
        }
        else
            m_hudRenderer.DrawImage(cmd.TextureName, cmd.DrawArea, cmd.MultiplyColor, cmd.Alpha, cmd.DrawInvulnerability);
    }

    private void HandleDrawShape(DrawShapeCommand cmd)
    {
        m_hudRenderer.DrawShape(cmd.Rectangle, cmd.Color, cmd.Alpha);
    }

    private void HandleDrawText(DrawTextCommand cmd)
    {
        m_hudRenderer.DrawText(cmd.Text, cmd.DrawArea, cmd.Alpha);
        var dataCache = m_archiveCollection.DataCache;
        dataCache.FreeRenderableString(cmd.Text);
    }

    private void HandleRenderWorldCommand(DrawWorldCommand cmd, Rectangle viewport)
    {
        if (viewport.Width == 0 || viewport.Height == 0)
            return;

        if (cmd.DrawAutomap)
        {
            // TODO: If drawing automap, draw black box everywhere.
        }

        DrawHudImagesIfAnyQueued(viewport);

        m_renderInfo.Set(cmd.Camera, cmd.GametickFraction, viewport, cmd.ViewerEntity, cmd.DrawAutomap,
            cmd.AutomapOffset, cmd.AutomapScale, m_config.Render);
        m_worldRenderer.Render(cmd.World, m_renderInfo);
    }

    private void HandleViewportCommand(ViewportCommand viewportCommand, out Rectangle viewport)
    {
        Vec2I offset = viewportCommand.Offset;
        Dimension dimension = viewportCommand.Dimension;
        viewport = new Rectangle(offset.X, offset.Y, dimension.Width, dimension.Height);

        gl.Viewport(offset.X, offset.Y, dimension.Width, dimension.Height);
    }

    private void DrawHudImagesIfAnyQueued(Rectangle viewport)
    {
        m_hudRenderer.Render(viewport);
    }

    private void ReleaseUnmanagedResources()
    {
        m_textureManager.Dispose();
        m_hudRenderer.Dispose();
        m_worldRenderer.Dispose();
    }
}
