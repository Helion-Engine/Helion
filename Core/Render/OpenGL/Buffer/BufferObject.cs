using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Util;
using Helion.Util.Container;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Buffer;

/// <summary>
/// Represents a buffer object on the GPU.
/// </summary>
/// <typeparam name="T">The type to hold, which must be a struct that is
/// packed.</typeparam>
public abstract class BufferObject<T> : IDisposable where T : struct
{
    public static readonly int BytesPerElement = Marshal.SizeOf<T>();

    public DynamicArray<T> Data = new DynamicArray<T>();
    protected readonly int BufferId;
    protected bool Uploaded;
    private int m_dataVersion;
    public IntPtr m_vboArrayPtr;
    private GCHandle m_pinnedArray;

    public int Count => Data.Length;
    public bool Empty => Count == 0;
    public bool NeedsUpload => !Uploaded && Count > 0;
    public int TotalBytes => Count * BytesPerElement;

    protected BufferObject(string objectLabel)
    {
        BufferId = GL.GenBuffer();

        m_pinnedArray = GCHandle.Alloc(Data.Data, GCHandleType.Pinned);
        m_vboArrayPtr = m_pinnedArray.AddrOfPinnedObject();
        m_dataVersion = Data.Version;

        Bind();
        SetObjectLabel(objectLabel);
        Unbind();
    }

    ~BufferObject()
    {
        ReleaseUnmanagedResources();
    }

    public IntPtr GetVboArray()
    {
        if (m_dataVersion != Data.Version)
        {
            m_pinnedArray.Free();
            m_pinnedArray = GCHandle.Alloc(Data.Data, GCHandleType.Pinned);
            m_vboArrayPtr = m_pinnedArray.AddrOfPinnedObject();
            m_dataVersion = Data.Version;
        }

        return m_vboArrayPtr;
    }

    public void Add(T element)
    {
        Data.Add(element);
        Uploaded = false;
    }

    public void Add(params T[] elements)
    {
        if (elements.Length <= 0)
            return;

        Data.Add(elements);
        Uploaded = false;
    }

    public void Add(T[] elements, int length)
    {
        Data.Add(elements, length);
        Uploaded = false;
    }

    public void Upload()
    {
        if (Uploaded)
            return;

        PerformUpload();
        Uploaded = true;
    }

    public void UploadSubData(int start, int length)
    {
        BufferSubData(start, length);
    }

    protected virtual void BufferSubData(int start, int length)
    {
        // To be implemented by children for now.
    }

    public void UploadIfNeeded()
    {
        if (NeedsUpload)
        {
            Bind();
            Upload();
            Unbind();
        }
    }

    public void Clear()
    {
        Data.Clear();
        Uploaded = false;
    }

    public void Bind()
    {
        GL.BindBuffer(GetBufferType(), BufferId);
    }

    public void Unbind()
    {
        GL.BindBuffer(GetBufferType(), 0);
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    protected virtual void ReleaseUnmanagedResources()
    {
        GL.DeleteBuffer(BufferId);

        // Since VBOs can end up holding a lot of data, if we dispose of it
        // but take a while to lose the reference, we still want to leave
        // the option for the GC to retrieve memory.
        Data = null!;

        m_pinnedArray.Free();
    }

    protected abstract BufferTarget GetBufferType();
    protected abstract void PerformUpload();

    [Conditional("DEBUG")]
    private void SetObjectLabel(string objectLabel)
    {
        GLHelper.ObjectLabel(ObjectLabelIdentifier.Buffer, BufferId, objectLabel);
    }
}
