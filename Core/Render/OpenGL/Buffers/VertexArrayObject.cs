using System;
using System.Collections.Generic;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assert;

namespace Helion.Render.OpenGL.Buffers
{
    public class VertexArrayObject : IDisposable
    {
        public readonly List<VaoAttribute> Attributes = new List<VaoAttribute>();
        private readonly int vao;
        private bool disposed;

        public VertexArrayObject(params VaoAttribute[] attributes)
        {
            Precondition(attributes.Length > 0, "Cannot have a VAO with no attributes");
            
            Attributes.AddRange(attributes);
            vao = GL.GenVertexArray();
        }

        private void ReleaseUnmanagedResources()
        {
            Precondition(!disposed, "Attempting to dispose a VAO more than once");
            
            GL.DeleteVertexArray(vao);
            
            disposed = true;
        }

        public void Bind()
        {
            GL.BindVertexArray(vao);
        }

        public void Unbind()
        {
            GL.BindVertexArray(0);
        }

        public void BindAnd(Action action)
        {
            Bind();
            action.Invoke();
            Unbind();
        }

        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        ~VertexArrayObject()
        {
            ReleaseUnmanagedResources();
        }
    }

    public class VaoAttribute
    {
        // TODO
    }
}