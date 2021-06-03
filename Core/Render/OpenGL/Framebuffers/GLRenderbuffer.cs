using System;
using Helion.Geometry;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Framebuffers
{
    public class GLRenderbuffer : IDisposable
    {
        public int RenderbufferName { get; private set; }
        private bool m_disposed;
        
        public GLRenderbuffer(Dimension dimension)
        {
            RenderbufferName = GL.GenRenderbuffer();

            SetStorage(dimension);
        }

        ~GLRenderbuffer()
        {
            FailedToDispose(this);
            PerformDispose();
        }
        
        public void SetDebugLabel(string name)
        {
            GLUtil.Label($"Renderbuffer: {name}", ObjectLabelIdentifier.Renderbuffer, RenderbufferName);
        }
        
        private void SetStorage(Dimension dimension)
        {
            BindAnd(() =>
            {
                (int w, int h) = dimension;
                GL.RenderbufferStorage(RenderbufferTarget.Renderbuffer, RenderbufferStorage.Depth24Stencil8, w, h);
            });
        }

        public void Bind()
        {
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, RenderbufferName);
        }
        
        public void Unbind()
        {
            GL.BindRenderbuffer(RenderbufferTarget.Renderbuffer, 0);
        }
        
        public void BindAnd(Action action)
        {
            Bind();
            action();
            Unbind();
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            PerformDispose();
        }

        private void PerformDispose()
        {
            if (m_disposed)
                return;
            
            GL.DeleteRenderbuffer(RenderbufferName);
            RenderbufferName = 0;

            m_disposed = true;
        }
    }
}
