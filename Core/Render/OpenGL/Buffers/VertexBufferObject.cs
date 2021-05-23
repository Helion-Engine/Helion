using System;
using System.Collections.Generic;
using Helion.Util.Container;
using Helion.Util.Extensions;
using OpenTK.Graphics.OpenGL4;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Buffers
{
    public class VertexBufferObject<TVertex> : BufferObject<TVertex> where TVertex : struct
    {
        private readonly BufferUsageHint m_hint;
        private bool m_uploaded;
        private int m_glName;
        private bool m_disposed;

        private bool UploadNeeded => !m_uploaded && Count > 0;

        public VertexBufferObject(BufferUsageHint hint)
        {
            m_glName = GL.GenBuffer();
            m_hint = hint;
        }

        ~VertexBufferObject()
        {
            FailedToDispose(this);
            PerformDispose();
        }
        
        public void SetDebugLabel(string name)
        {
            GLUtil.Label($"VBO: {name}", ObjectLabelIdentifier.Buffer, m_glName);
        }

        public void Add(TVertex element)
        {
            Data.Add(element);
            m_uploaded = false;
        }
        
        public void AddRange(DynamicArray<TVertex> elements)
        {
            if (elements.Empty())
                return;
            
            Data.AddRange(elements.Data);
            m_uploaded = false;
        }
        
        public void AddRange(IList<TVertex> elements)
        {
            if (elements.Empty())
                return;
            
            Data.AddRange(elements);
            m_uploaded = false;
        }
        
        public void UploadIfNeeded()
        {
            if (!UploadNeeded)
                return;
            
            GL.BufferData(BufferTarget.ArrayBuffer, TotalBytes, Data.Data, m_hint);
            m_uploaded = true;
        }
        
        public void BindAndUploadIfNeeded()
        {
            if (!UploadNeeded)
                return;

            Bind();
            GL.BufferData(BufferTarget.ArrayBuffer, TotalBytes, Data.Data, m_hint);
            Unbind();
            
            m_uploaded = true;
        }
        
        public void Bind()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, m_glName);
        }
        
        public void Unbind()
        {
            GL.BindBuffer(BufferTarget.ArrayBuffer, 0);
        }

        public void BindAnd(Action action)
        {
            Bind();
            action();
            Unbind();
        }

        public override void Dispose()
        {
            PerformDispose();
            GC.SuppressFinalize(this);
        }

        private void PerformDispose()
        {
            if (m_disposed)
                return;

            GL.DeleteBuffer(m_glName);
            m_glName = 0;

            m_disposed = true;
        }
    }
}
