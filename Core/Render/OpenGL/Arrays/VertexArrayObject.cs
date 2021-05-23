using System;
using Helion.Render.OpenGL.Buffers;
using OpenTK.Graphics.OpenGL4;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Arrays
{
    public class VertexArrayObject<TVertex> : IDisposable where TVertex : struct
    {
        private int m_glName;
        private bool m_disposed;

        public VertexArrayObject()
        {
            m_glName = GL.GenVertexArray();
        }

        ~VertexArrayObject()
        {
            FailedToDispose(this);
            PerformDispose();
        }
        
        public void SetDebugLabel(string name)
        {
            GLUtil.Label($"VAO: {name}", ObjectLabelIdentifier.VertexArray, m_glName);
        }

        public void BindAttributes(VertexBufferObject<TVertex> vbo)
        {
            Bind();
            vbo.Bind();

            foreach (var vaoAttribute in VertexArrayAttribute.FindAttributes<TVertex>())
                vaoAttribute.EnableAttribute();
            
            vbo.Unbind();
            Unbind();
        }

        public void Bind()
        {
            GL.BindVertexArray(m_glName);
        }

        public void Unbind()
        {
            GL.BindVertexArray(0);
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
                
            GL.DeleteVertexArray(m_glName);
            m_glName = 0;
            
            m_disposed = true;
        }
    }
}
