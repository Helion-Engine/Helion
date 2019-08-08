using System;
using System.Drawing;
using Helion.Render.Commands;
using Helion.Render.Commands.Types;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Enums;
using Helion.Render.OpenGL.Util;
using Helion.Resources.Archives.Collection;
using Helion.Util.Configuration;
using Helion.Util.Geometry;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL
{
    public class GLRenderer : IRenderer, IDisposable
    {
        private readonly Config m_config;
        private readonly ArchiveCollection m_archiveCollection;
        private readonly GLCapabilities m_capabilities;
        private readonly GLFunctions gl;
        private bool m_disposed;
        
        public GLRenderer(Config config, ArchiveCollection archiveCollection, GLFunctions functions)
        {
            m_config = config;
            m_archiveCollection = archiveCollection;
            m_capabilities = new GLCapabilities(functions);
            gl = functions;
        }

        ~GLRenderer()
        {
            Fail("Did not dispose of GLRenderer, finalizer run when it should not be");
            ReleaseUnmanagedResources();
        }

        public void Render(RenderCommands renderCommands)
        {
            foreach (IRenderCommand renderCommand in renderCommands.GetCommands())
            {
                switch (renderCommand)
                {
                case ClearRenderCommand cmd:
                    HandleClearCommand(cmd);
                    break;
                case DrawWorldCommand cmd:
//                    RenderInfo renderInfo = new RenderInfo(cmd.Camera, cmd.GametickFraction, currentViewport);
//                    m_worldRenderer.Render(cmd.World, renderInfo);
                    break;
                case ViewportCommand cmd:
                    HandleViewportCommand(cmd);
                    break;
                default:
                    Fail($"Unsupported render command type: {renderCommand}");
                    break;
                }
            }
            
            GLHelper.AssertNoGLError(gl);
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
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
        
        private void HandleViewportCommand(ViewportCommand viewportCommand)
        {
            Vec2I offset = viewportCommand.Offset;
            Dimension dimension = viewportCommand.Dimension;
            gl.Viewport(offset.X, offset.Y, dimension.Width, dimension.Height);
        }

        private void ReleaseUnmanagedResources()
        {
            Precondition(!m_disposed, "Trying to dispose the GLRenderer twice");
            
            // TODO: Release unmanaged resources here.

            m_disposed = true;
        }
    }
}