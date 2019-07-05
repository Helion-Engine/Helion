using Helion.Render.Commands;
using Helion.Render.OpenGL.Util;
using NLog;
using OpenTK.Graphics.OpenGL;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;

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

        public void Render(RenderCommands renderCommands)
        {
            // TODO
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