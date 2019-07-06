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
using static Helion.Util.Assert;

namespace Helion.Render.OpenGL
{
    public class OpenGLRenderer : IRenderer, IDisposable
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        public readonly GLInfo GLInfo = new GLInfo();
        private bool disposed;

        public OpenGLRenderer()
        {
            SetGLStates();
            SetGLDebugger();
        }
        
        ~OpenGLRenderer()
        {
            ReleaseUnmanagedResources();
        }

        private void ReleaseUnmanagedResources()
        {
            // TODO: Dispose
        }

        private void SetGLStates()
        {
            GL.Enable(EnableCap.DepthTest);
            GL.Enable(EnableCap.Multisample);

            if (GLInfo.Version.Supports(3, 2))
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
            if (!GLInfo.Version.Supports(4, 3)) 
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
            if (disposed)
                return;
            
            disposed = true;
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }
    }
}