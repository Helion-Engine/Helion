using GlmSharp;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Render.Common.Renderers;
using Helion.Render.Common.Textures;
using Helion.Render.OpenGL;
using Helion.Render.OpenGL.Commands;
using Helion.Render.OpenGL.Commands.Types;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Framebuffer;
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
using Helion.World;
using NLog;
using OpenTK.Graphics.OpenGL;
using System;
using System.Diagnostics;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render;

public class Renderer : IDisposable
{
    const float ZNearMin = 0.2f;
    const float ZNearMax = 7.9f;
    public static readonly Color DefaultBackground = (16, 16, 16);
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private static bool InfoPrinted;

    public readonly IWindow Window;
    public readonly GLSurface Default;
    public readonly LegacyGLTextureManager Textures;
    internal readonly IConfig m_config;
    internal readonly FpsTracker m_fpsTracker;
    internal readonly ArchiveCollection m_archiveCollection;
    private readonly WorldRenderer m_worldRenderer;
    private readonly HudRenderer m_hudRenderer;
    private readonly RenderInfo m_renderInfo = new();
    private readonly FramebufferRenderer m_framebufferRenderer;
    private bool m_disposed;

    public Dimension RenderDimension => UseVirtualResolution ? m_config.Window.Virtual.Dimension : Window.Dimension;
    public IImageDrawInfoProvider DrawInfo => Textures.ImageDrawInfoProvider;
    private bool UseVirtualResolution => m_config.Window.Virtual.Enable && m_config.Window.Virtual.Dimension.Value.HasPositiveArea;

    public Renderer(IWindow window, IConfig config, ArchiveCollection archiveCollection, FpsTracker fpsTracker)
    {
        Window = window;
        m_config = config;
        m_archiveCollection = archiveCollection;
        m_fpsTracker = fpsTracker;

        SetGLDebugger();

        Textures = new LegacyGLTextureManager(config, archiveCollection);
        m_worldRenderer = new LegacyWorldRenderer(config, archiveCollection, Textures);
        m_hudRenderer = new LegacyHudRenderer(Textures, archiveCollection.DataCache);
        m_framebufferRenderer = new(config, window);
        Default = new(window, this);

        PrintGLInfo();
        SetGLStates();
    }

    public void UpdateToNewWorld(IWorld world)
    {
        m_worldRenderer.UpdateToNewWorld(world);
    }

    ~Renderer()
    {
        Dispose(false);
    }

    public static mat4 CalculateMvpMatrix(RenderInfo renderInfo, bool onlyXY = false)
    {
        float w = renderInfo.Viewport.Width;
        float h = renderInfo.Viewport.Height * 0.825f;
        // Default FOV is 63.2. Config default is 90 so we need to convert. (90 - 63.2 = 26.8).
        float fovY = (float)MathHelper.ToRadians(renderInfo.Config.FieldOfView - 26.8);

        mat4 model = mat4.Identity;
        mat4 view = renderInfo.Camera.CalculateViewMatrix(onlyXY);

        mat4 projection = mat4.PerspectiveFov(fovY, w, h, GetZNear(renderInfo), 65536.0f);
        return projection * view * model;
    }

    private static float GetZNear(RenderInfo renderInfo)
    {
        // Optimially this should be handled in the shader. Setting this variable and using it for a low zNear is good enough for now.
        // If we are being crushed or clipped into a line with a middle texture then use a lower zNear.
        float zNear = (float)((renderInfo.ViewerEntity.LowestCeilingZ - renderInfo.ViewerEntity.HighestFloorZ - renderInfo.ViewerEntity.ViewZ) * 0.68);
        if (renderInfo.ViewerEntity.ViewLineClip || renderInfo.ViewerEntity.ViewPlaneClip)
            zNear = ZNearMin;

        return MathHelper.Clamp(zNear, ZNearMin, ZNearMax);
    }

    public static float GetDistanceOffset(RenderInfo renderInfo) =>
        (ZNearMax - GetZNear(renderInfo)) * 2;

    public void Render(RenderCommands renderCommands)
    {
        m_hudRenderer.Clear();

        if (UseVirtualResolution)
            SetupAndBindVirtualFramebuffer();

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

        if (UseVirtualResolution)
            DrawFramebufferOnDefault();
    }

    private void SetupAndBindVirtualFramebuffer()
    {
        Dimension dimension = m_config.Window.Virtual.Dimension;
        m_framebufferRenderer.UpdateToDimensionIfNeeded(dimension);

        m_framebufferRenderer.Framebuffer.Bind();
    }

    private void DrawFramebufferOnDefault()
    {
        m_framebufferRenderer.Framebuffer.Unbind();

        m_framebufferRenderer.Render();
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
        if (viewport.Width == 0 || viewport.Height == 0 || cmd.World.IsDisposed)
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

        GL.Viewport(offset.X, offset.Y, dimension.Width, dimension.Height);
    }

    private void DrawHudImagesIfAnyQueued(Rectangle viewport)
    {
        m_hudRenderer.Render(viewport);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        Textures.Dispose();
        m_hudRenderer.Dispose();
        m_worldRenderer.Dispose();
        m_framebufferRenderer.Dispose();

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
