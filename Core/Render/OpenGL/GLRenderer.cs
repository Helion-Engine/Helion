using Helion.Render.Commands;
using Helion.Render.Commands.Types;
using Helion.Render.OpenGL.Util;
using Helion.Util.Geometry;
using NLog;
using OpenTK.Graphics.OpenGL;
using System;
using System.Diagnostics;
using System.Drawing;
using System.Runtime.InteropServices;
using Helion.Render.OpenGL.Renderers.World;
using Helion.Render.OpenGL.Texture;
using Helion.Util.Configuration;
using static Helion.Util.Assert;

namespace Helion.Render.OpenGL
{
    public class GLRenderer : IRenderer, IDisposable
    {
        private static readonly GLCapabilities Capabilities = new GLCapabilities();
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private static bool InfoPrinted;

        private readonly GLTextureManager m_textureManager;
        private readonly WorldRenderer m_worldRenderer;

        public GLRenderer(Config config)
        {
            m_textureManager = new GLTextureManager(config, Capabilities);
            m_worldRenderer = new WorldRenderer();
            
            PrintGLInfo();
            SetGLStates();
            SetGLDebugger();
        }
        
        ~GLRenderer()
        {
            ReleaseUnmanagedResources();
        }

        public void Render(RenderCommands renderCommands)
        {
            foreach (IRenderCommand renderCommand in renderCommands.GetCommands())
            {
                switch (renderCommand)
                {
                case ClearRenderCommand clearRenderCommand:
                    HandleClearCommand(clearRenderCommand);
                    break;
                case DrawWorldCommand drawWorldCommand:
                    // TODO
                    break;
                case ViewportCommand viewportCommand:
                    HandleViewportCommand(viewportCommand);
                    break;
                default:
                    Fail($"Unsupported render command type: {renderCommand}");
                    break;
                }
            }
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }
        
        private void ReleaseUnmanagedResources()
        {
            m_worldRenderer.Dispose();
            m_textureManager.Dispose();
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

        private void SetGLStates()
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

        [Conditional("DEBUG")]
        private void SetGLDebugger()
        {
            if (!Capabilities.Version.Supports(4, 3)) 
                return;
            
            GL.Enable(EnableCap.DebugOutput);
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

        private void HandleClearCommand(ClearRenderCommand clearRenderCommand)
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
        
        private void HandleViewportCommand(ViewportCommand viewportCommand)
        {
            Vec2I offset = viewportCommand.Offset;
            Dimension dimension = viewportCommand.Dimension;
            GL.Viewport(offset.X, offset.Y, dimension.Width, dimension.Height);
        }
    }
}