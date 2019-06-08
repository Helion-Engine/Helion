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
using System.Drawing;
using System.Runtime.InteropServices;
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

        private void SetGLStates()
        {
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Multisample);

            if (info.Version.Supports(3, 2))
                GL.Enable(EnableCap.TextureCubeMapSeamless);

            GL.Enable(EnableCap.Blend);
            GL.BlendFunc(BlendingFactor.SrcAlpha, BlendingFactor.OneMinusSrcAlpha);

            // Note that we cull CCW faces even though we supply our VBOs with
            // CCW rotations. The view transformation causes the faces to end
            // up being CW, so we want to cull any that are CCW.
            GL.Enable(EnableCap.CullFace);
            GL.FrontFace(FrontFaceDirection.Cw);
            GL.CullFace(CullFaceMode.Back);
            GL.PolygonMode(MaterialFace.FrontAndBack, PolygonMode.Fill);
        }

        private void SetGLDebugger()
        {
            if (info.Version.Supports(4, 3))
            {
                GL.Enable(EnableCap.DebugOutput);
                GL.DebugMessageCallback((DebugSource source, DebugType type, int id, DebugSeverity severity, 
                                         int length, IntPtr message, IntPtr userParam) =>
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
