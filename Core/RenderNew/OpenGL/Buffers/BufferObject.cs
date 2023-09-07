using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Helion.RenderNew.OpenGL.Util;
using Helion.Util.Container;
using OpenTK.Graphics.OpenGL;

namespace Helion.RenderNew.OpenGL.Buffers;

public abstract class BufferObject<T> : IDisposable where T : struct
{
    protected const int DefaultCapacity = 128;
    private static readonly int BytesPerElement = Marshal.SizeOf<T>();
    
    public readonly string Label;
    protected readonly int Name;
    protected readonly DynamicArray<T> Data;
    protected bool NeedsUpload;
    private readonly BufferTarget m_target;
    private readonly BufferUsageHint m_hint;
    private bool m_disposed;

    protected BufferObject(string label, BufferTarget target, BufferUsageHint hint, int capacity)
    {
        Label = label;
        Name = GL.GenBuffer();
        Data = new(capacity);
        m_target = target;
        m_hint = hint;

        Bind();
        GLHelper.ObjectLabel(ObjectLabelIdentifier.Buffer, Name, Label);
        Unbind();
    }

    ~BufferObject()
    {
        Dispose(false);
    }

    public void Bind()
    {
        GL.BindBuffer(m_target, Name);
    }

    public void Unbind()
    {
        GL.BindBuffer(m_target, 0);
    }

    public void UploadIfNeeded()
    {
        if (!NeedsUpload)
            return;
        
        GL.BufferData(m_target, BytesPerElement * Data.Length, Data.Data, m_hint);
        NeedsUpload = false;
    }

    public void Dispose()
    {
        if (m_disposed)
            return;
        
        Dispose(true);
        GC.SuppressFinalize(this);
    }

    protected virtual void Dispose(bool disposing)
    {
        ReleaseUnmanagedResources();
        
        m_disposed = true;
    }

    private void ReleaseUnmanagedResources()
    {
        GL.DeleteBuffer(Name);
    }
}