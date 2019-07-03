using Helion.Projects;
using Helion.Render.OpenGL.Renderers.Console;
using Helion.Render.OpenGL.Renderers.World;
using Helion.Render.OpenGL.Texture;
using Helion.Render.Shared;
using Helion.Resources.Images;
using Helion.World;
using NLog;
using OpenTK.Graphics.OpenGL4;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using OpenTK;
using static Helion.Util.Assert;

namespace Helion.Render.OpenGL
{
    public class GLRenderer : IDisposable
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private bool disposed = false;
        private GLTextureManager textureManager;
        private ConsoleRenderer consoleRenderer;
        private WorldRenderer worldRenderer;
        private readonly GLInfo info;

        public GLRenderer(GLInfo glInfo, Project project)
        {
            info = glInfo;
            textureManager = new GLTextureManager(glInfo, project);
            consoleRenderer = new ConsoleRenderer(textureManager);
            worldRenderer = new WorldRenderer(textureManager);

            SetGLStates();
            SetGLDebugger();
        }

        ~GLRenderer() => Dispose(false);

        public static Matrix4 CreateMVP(RenderInfo renderInfo)
        {
            // TODO: Get config values for this.
            float aspectRatio = (float)renderInfo.Viewport.Width / renderInfo.Viewport.Height;
            Matrix4.CreatePerspectiveFieldOfView(Util.MathHelper.QuarterPi, aspectRatio, 16.0f, 8192.0f, out Matrix4 projection);

            // Note that we have no model matrix, everything is already in the
            // world space.
            //
            // Unfortunately, C#/OpenTK do not follow C++/glm/glsl conventions
            // of left multiplication. Instead of doing p * v * m, it has to
            // be done in the opposite direction (m * v * p) due to a design
            // decision according to a lead developer. This will seem wrong
            // for anyone used to the C++/OpenGL way of multiplying.
            Matrix4 view = Camera.ViewMatrix(renderInfo.CameraInfo);
            Matrix4 mvp = view * projection;
            
            return mvp;
        }

        private void SetGLStates()
        {
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Multisample);

            if (info.Version.Supports(3, 2))
                GL.Enable(EnableCap.TextureCubeMapSeamless);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            GL.Enable(EnableCap.CullFace);
            GL.FrontFace(FrontFaceDirection.Ccw);
            GL.CullFace(CullFaceMode.Back);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        }

        [Conditional("DEBUG")]
        private void SetGLDebugger()
        {
            if (!info.Version.Supports(4, 3)) 
                return;
            
            GL.Enable(EnableCap.DebugOutput);
            GL.DebugMessageCallback((source, type, id, severity, length, message, userParam) =>
            {
                string msg = Marshal.PtrToStringAnsi(message, length);

                switch (severity)
                {
                case DebugSeverity.DebugSeverityHigh:
                case DebugSeverity.DebugSeverityMedium:
                    log.Error("[GLDebug type={0}] {1}", type, msg);
                    break;
                case DebugSeverity.DebugSeverityLow:
                    log.Warn("[GLDebug type={0}] {1}", type, msg);
                    break;
                default:
                    break;
                }
            }, IntPtr.Zero);
        }

        public void Clear(Size windowDimension)
        {
            GL.Clear(ClearBufferMask.ColorBufferBit | ClearBufferMask.DepthBufferBit | ClearBufferMask.StencilBufferBit);
            GL.ClearColor(0.1f, 0.1f, 0.1f, 1.0f);
            GL.Viewport(windowDimension);
        }

        public void RenderStart(Rectangle viewport)
        {
            GL.ActiveTexture(TextureUnit.Texture0);
            GL.Viewport(viewport);
        }

        public void RenderConsole(Util.Console console)
        {
            consoleRenderer.Render(console);
        }

        public void RenderWorld(WorldBase world, RenderInfo renderInfo)
        {
            worldRenderer.Render(world, renderInfo);
        }

        public void HandleTextureEvent(object sender, ImageManagerEventArgs imageEvent)
        {
            switch (imageEvent.Type)
            {
            case ImageManagerEventType.CreateOrUpdate:
                if (imageEvent.Image != null)
                    textureManager.CreateOrUpdateTexture(imageEvent.Image, imageEvent.Name, imageEvent.Namespace);
                else
                    Fail("Image create/update event cannot have a null image");
                break;

            case ImageManagerEventType.Delete:
                textureManager.DeleteTexture(imageEvent.Name);
                break;

            default:
                Fail("Unexpected image event enumeration");
                break;
            }
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
            {
                worldRenderer.Dispose();
                consoleRenderer.Dispose();
                textureManager.Dispose();
            }

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
