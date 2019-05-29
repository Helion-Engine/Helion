using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Linq;
using static Helion.Util.Assert;

namespace Helion.Render.OpenGL.Shared.Buffer.Vao
{
    public class VertexArrayObject : IDisposable
    {
        private bool disposed;
        private int vao;
        public List<VertexArrayObjectElement> Attributes = new List<VertexArrayObjectElement>();

        public VertexArrayObject(params VertexArrayObjectElement[] attributes)
        {
            Array.ForEach(attributes, Attributes.Add);
            Precondition(NoDuplicatedIndices(), "Duplicate VAO indices found, should only have one index per attribute");

            vao = GL.GenVertexArray();
        }

        ~VertexArrayObject() => Dispose(false);

        private bool NoDuplicatedIndices() => Attributes.GroupBy(attr => attr.Index).All(g => g.Count() == 1);

        public void Bind() => GL.BindVertexArray(vao);

        public void Unbind() => GL.BindVertexArray(0);

        public void BindAnd(Action action)
        {
            Bind();
            action.Invoke();
            Unbind();
        }

        protected virtual void Dispose(bool disposing)
        {
            if (disposed)
                return;

            if (disposing)
                GL.DeleteVertexArray(vao);

            disposed = true;
        }

        public void Dispose()
        {
            Dispose(true);
            GC.SuppressFinalize(this);
        }
    }
}
