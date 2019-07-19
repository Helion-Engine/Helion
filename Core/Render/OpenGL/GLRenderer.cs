using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using Helion.Render.Commands;
using Helion.Render.Commands.Types;
using Helion.Render.OpenGL.Renderers.World;
using Helion.Render.OpenGL.Texture;
using Helion.Render.OpenGL.Util;
using Helion.Render.Shared;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configuration;
using Helion.Util.Geometry;
using NLog;
using OpenTK;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL
{
    /// <summary>
    /// An OpenGL based renderer.
    /// </summary>
    public class GLRenderer : IRenderer, IDisposable
    {
        private static readonly GLCapabilities Capabilities = new GLCapabilities();
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static bool InfoPrinted;

        private readonly Config m_config;
        private readonly GLTextureManager m_textureManager;
        private readonly WorldRenderer m_worldRenderer;

        /// <summary>
        /// Creates an OpenGL-driven renderer.
        /// </summary>
        /// <param name="config">The configuration settings.</param>
        /// <param name="archiveCollection">The collection which has texture
        /// data that we will hook into.</param>
        public GLRenderer(Config config, ArchiveCollection archiveCollection)
        {
            m_config = config;
            m_textureManager = new GLTextureManager(config, Capabilities, archiveCollection);
            m_worldRenderer = new WorldRenderer(config, Capabilities, archiveCollection, m_textureManager);

            PrintGLInfo();
            SetGLStates();
            SetGLDebugger();
        }

        ~GLRenderer()
        {
            ReleaseUnmanagedResources();
        }

        /// <summary>
        /// Creates a model view projection matrix from the provided data.
        /// </summary>
        /// <param name="renderInfo">The rendering information.</param>
        /// <param name="fovX">The field of view along the X axis in radians.
        /// </param>
        /// <returns>A projection perspective matrix.</returns>
        public static Matrix4 CreateMVP(RenderInfo renderInfo, float fovX)
        {
            Precondition(fovX > 0 && fovX <= MathHelper.Pi, "Field of view X radians are out of range");
            
            float aspectRatio = (float)renderInfo.Viewport.Width / renderInfo.Viewport.Height;
            float fovY = Camera.FieldOfViewXToY(fovX, aspectRatio);

            // Note that we have no model matrix, everything is already in the
            // world space.
            // TODO: Find a better way of doing hardcoded near/farZ values.
            Matrix4 model = Matrix4.Identity;
            Matrix4 view = renderInfo.Camera.CalculateViewMatrix();
            Matrix4 projection = Matrix4.CreatePerspectiveFieldOfView(fovY, aspectRatio, 16.0f, 8192.0f);

            // Unfortunately, C#/OpenTK do not follow C++/glm/glsl conventions
            // of left multiplication. Instead of doing p * v * m, it has to
            // be done in the opposite direction (m * v * p) due to a design
            // decision according to a lead developer. This will seem wrong
            // for anyone used to the C++/OpenGL way of multiplying.
            return model * view * projection;
        }

        /// <summary>
        /// Renders all of the render commands.
        /// </summary>
        /// <param name="renderCommands">The rendering commands.</param>
        public void Render(RenderCommands renderCommands)
        {
            Rectangle currentViewport = new Rectangle(0, 0, 1024, 768);
            
            foreach (IRenderCommand renderCommand in renderCommands.GetCommands())
            {
                switch (renderCommand)
                {
                case ClearRenderCommand cmd:
                    HandleClearCommand(cmd);
                    break;
                case DrawWorldCommand cmd:
                    RenderInfo renderInfo = new RenderInfo(cmd.Camera, cmd.GametickFraction, currentViewport);
                    m_worldRenderer.Render(cmd.World, renderInfo);
                    break;
                case ViewportCommand cmd:
                    currentViewport = new Rectangle(cmd.Offset.X, cmd.Offset.Y, cmd.Dimension.Width, cmd.Dimension.Height);
                    HandleViewportCommand(cmd);
                    break;
                default:
                    Fail($"Unsupported render command type: {renderCommand}");
                    break;
                }
            }
            
            if (m_config.Engine.Developer.RenderDebug)
                GLHelper.ThrowIfErrorDetected();
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        private static void PrintGLInfo()
        {
            if (InfoPrinted)
                return;
            
            Log.Info("Loaded OpenGL v{0}", Capabilities.Version);
            Log.Info("OpenGL Shading Language: {0}", Capabilities.ShadingVersion);
            Log.Info("Vendor: {0}", Capabilities.Vendor);
            Log.Info("Hardware: {0}", Capabilities.Renderer);

            InfoPrinted = true;
        }

        private static void SetGLStates()
        {
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Multisample);

            if (Capabilities.Version.Supports(3, 2))
                GL.Enable(EnableCap.TextureCubeMapSeamless);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.Enable(EnableCap.CullFace);
            GL.FrontFace(FrontFaceDirection.Ccw);
            GL.CullFace(CullFaceMode.Back);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        }

        private static void HandleClearCommand(ClearRenderCommand clearRenderCommand)
        {
            Color color = clearRenderCommand.ClearColor;
            GL.ClearColor(color.R / 255.0f, color.G / 255.0f, color.B / 255.0f, color.A / 255.0f);
            
            ClearBufferMask clearMask = ClearBufferMask.None;
            if (clearRenderCommand.Color)
                clearMask |= ClearBufferMask.ColorBufferBit;
            if (clearRenderCommand.Depth)
                clearMask |= ClearBufferMask.DepthBufferBit;
            if (clearRenderCommand.Stencil)
                clearMask |= ClearBufferMask.StencilBufferBit;
            
            GL.Clear(clearMask);
        }

        private static void HandleViewportCommand(ViewportCommand viewportCommand)
        {
            Vec2I offset = viewportCommand.Offset;
            Dimension dimension = viewportCommand.Dimension;
            GL.Viewport(offset.X, offset.Y, dimension.Width, dimension.Height);
        }

        [Conditional("DEBUG")]
        private void SetGLDebugger()
        {
            // Note: This means it's not set if `RenderDebug` changes. As far
            // as I can tell, we can't unhook actions, but maybe we could do
            // some glDebugControl... setting that changes them all to don't
            // cares if we have already registered a function? See:
            // https://www.khronos.org/opengl/wiki/GLAPI/glDebugMessageControl
            if (!Capabilities.Version.Supports(4, 3) || !m_config.Engine.Developer.RenderDebug) 
                return;
            
            GL.Enable(EnableCap.DebugOutput);
            GL.Enable(EnableCap.DebugOutputSynchronous);
            
            // TODO: We should filter messages we want to get since this could
            //       pollute us with lots of messages and we wouldn't know it.
            //       https://www.khronos.org/opengl/wiki/GLAPI/glDebugMessageControl
            GL.DebugMessageCallback((source, type, id, severity, length, message, userParam) =>
            {
                string msg = Marshal.PtrToStringAnsi(message, length);

                switch (severity)
                {
                case DebugSeverity.DebugSeverityHigh:
                case DebugSeverity.DebugSeverityMedium:
                    Log.Error("[GLDebug type={0}] {1}", type, msg);
                    break;
                case DebugSeverity.DebugSeverityLow:
                    Log.Warn("[GLDebug type={0}] {1}", type, msg);
                    break;
                }
            }, IntPtr.Zero);
        }

        private void ReleaseUnmanagedResources()
        {
            m_worldRenderer.Dispose();
            m_textureManager.Dispose();
        }
    }
}