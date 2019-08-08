using System;
using Helion.Render.OldOpenGL.Util;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OldOpenGL.Buffers
{
    public class VertexArrayObject : IDisposable
    {
        public readonly VertexArrayAttributes Attributes;
        private readonly int vao;
        private bool disposed;

        public VertexArrayObject(GLCapabilities capabilities, VertexArrayAttributes vaoAttributes, string objectLabel = "")
        {
            Attributes = vaoAttributes;
            vao = GL.GenVertexArray();
            
            // We need to at least bind it first to allocate it, otherwise it's
            // undefined behavior to apply a label.
            BindAnd(() => { GLHelper.SetArrayObjectLabel(capabilities, vao, objectLabel); });
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
}