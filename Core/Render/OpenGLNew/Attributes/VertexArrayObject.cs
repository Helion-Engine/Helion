using System;
using System.Diagnostics;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGLNew.Util;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGLNew.Attributes;

public class VertexArrayObject : IDisposable
{
    private int m_objectId;
    private bool m_disposed;

    public VertexArrayObject(string label)
    {
        m_objectId = GL.GenVertexArray();

        Bind();
        GLUtil.ObjectLabel(ObjectLabelIdentifier.VertexArray, m_objectId, $"VAO: {label}");
        Unbind();
    }

    ~VertexArrayObject()
    {
        ReleaseManagedResources();
    }

    public void ApplyAttributes<TVertex>(VertexBufferObject<TVertex> vbo) where TVertex : struct
    {
        foreach (VertexAttributeInfo attr in VertexAttributeTracker.GetAttributes<TVertex>())
        {
            if (attr.IsIntegral)
                GL.VertexAttribIPointer(attr.Index, attr.Size, (VertexAttribIntegerType)attr.DataType, attr.Stride, attr.Offset);                
            else
                GL.VertexAttribPointer(attr.Index, attr.Size, (VertexAttribPointerType)attr.DataType, attr.Normalized, attr.Stride, attr.Offset);
            
            GL.EnableVertexAttribArray(attr.Index);
        }
    }
    
    public void Bind()
    {
        Debug.Assert(!m_disposed, $"Trying to upload data on a disposed VAO");
        
        GL.BindVertexArray(m_objectId);
    }

    public void Unbind()
    {
        Debug.Assert(!m_disposed, $"Trying to upload data on a disposed VAO");
        
        GL.BindVertexArray(GLUtil.NoObject);
    }

    private void ReleaseManagedResources()
    {
        if (m_disposed)
            return;

        GL.DeleteVertexArray(m_objectId);
        m_objectId = GLUtil.NoObject;

        m_disposed = true;
    }

    public void Dispose()
    {
        ReleaseManagedResources();
        GC.SuppressFinalize(this);
    }
}