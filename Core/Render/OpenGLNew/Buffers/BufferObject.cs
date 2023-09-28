using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Helion.Render.OpenGLNew.Util;
using Helion.Util.Container;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGLNew.Buffers;

public abstract class BufferObject<T> : IBindable, IDisposable where T : struct
{
    public const int DefaultCapacity = 128;
    public static readonly int BytesPerElement = Marshal.SizeOf<T>();

    public int ObjectId { get; private set; }
    protected internal DynamicArray<T> m_data { get; }
    private readonly MappedBuffer<T> m_mappedBufferHelper;
    private readonly BufferTarget m_target;
    private readonly BufferUsageHint m_hint;
    private bool m_disposed;

    public int Count => m_data.Length;
    public bool Empty => m_data.Empty();

    protected BufferObject(string label, BufferTarget target, BufferUsageHint hint, int capacity = DefaultCapacity)
    {
        Debug.Assert(capacity > 0, "Buffer object needs a positive capacity");
        
        ObjectId = GL.GenBuffer();
        m_target = target;
        m_hint = hint;
        m_data = new(capacity);
        m_mappedBufferHelper = new(this, m_target);

        Bind();
        GLUtil.ObjectLabel(ObjectLabelIdentifier.Buffer, ObjectId, label);
        Unbind();
    }

    ~BufferObject()
    {
        ReleaseUnmanagedResources();
    }
    
    public T this[int index] => m_data[index]; 

    public void Add(T element)
    {
        if (m_data.Capacity == m_data.Length && m_mappedBufferHelper.Mapped)
            throw new("Reallocation of buffer object will happen while it is mapped");
        
        m_data.Add(element);
    }
    
    public void Add(ReadOnlySpan<T> elements)
    {
        if (elements.IsEmpty)
            return;
        
        if (m_data.Capacity > m_data.Length + elements.Length && m_mappedBufferHelper.Mapped)
            throw new("Reallocation of buffer object will occur while it is mapped");
        
        for (int i = 0; i < elements.Length; i++)
            m_data.Add(elements[i]);
    }
    
    public void Upload()
    {
        Debug.Assert(!m_disposed, $"Trying to upload data on a disposed {m_target}");

        if (m_mappedBufferHelper.Mapped)
            throw new("Trying to upload while a buffer mapping is still active");
        
        GL.BufferData(m_target, BytesPerElement * m_data.Length, m_data.Data, m_hint);
    }

    public MappedBuffer<T> GetBufferMapper()
    {
        Debug.Assert(!m_disposed, $"Trying to get a buffer mapped when the buffer was disposed for {m_target}");
        
        return m_mappedBufferHelper;
    }
    
    public void SubData(int index, int length)
    {
        Debug.Assert(!m_disposed, $"Trying to sub data on a disposed {m_target}");
        Debug.Assert(index + length < m_data.Length, $"Performing sub data out of range {m_target}");
        
        int offsetBytes = index * BytesPerElement;
        int sizeBytes = length * BytesPerElement;
        GL.BufferSubData(m_target, offsetBytes, sizeBytes, m_data.Data);
    }
    
    public void SubData(int index, ReadOnlySpan<T> elements)
    {
        Debug.Assert(!m_disposed, $"Trying to sub data on a disposed {m_target}");
        Debug.Assert(index + elements.Length > m_data.Length, $"Performing sub data (with data span) out of range {m_target}");

        int endIndex = Math.Min(index + elements.Length, m_data.Length);

        // Commit the changes locally first.
        int elementIndex = 0;
        for (int i = index; i < endIndex; i++)
            m_data[i] = elements[elementIndex++];

        // Now that the changes are written, push them.
        SubData(index, endIndex - index);
    }

    public void Bind()
    {
        Debug.Assert(!m_disposed, $"Trying to bind a disposed {m_target}");
        
        GL.BindBuffer(m_target, ObjectId);
    }

    public void Unbind()
    {
        Debug.Assert(!m_disposed, $"Trying to unbind from a disposed {m_target}");
        
        GL.BindBuffer(m_target, GLUtil.NoObject);
    }

    private void ReleaseUnmanagedResources()
    {
        if (m_disposed)
            return;
    
        m_mappedBufferHelper.Dispose();
        
        GL.DeleteBuffer(ObjectId);
        ObjectId = GLUtil.NoObject;

        m_disposed = true;
    }

    public virtual void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }
}