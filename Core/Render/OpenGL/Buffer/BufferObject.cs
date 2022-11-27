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

    public readonly string Label;
    public DynamicArray<T> Data = new DynamicArray<T>();
    protected readonly int BufferId;
    protected bool Uploaded;
    private int m_dataVersion;
    private IntPtr m_vboArrayPtr;
    private GCHandle m_pinnedArray;
    private bool m_disposed;

    protected abstract BufferTarget Target { get; }
    protected abstract string LabelPrefix { get; }

    public int Count => Data.Length;
    public bool Empty => Count == 0;
    public bool NeedsUpload => !Uploaded && Count > 0;
    public int TotalBytes => Count * BytesPerElement;

    protected BufferObject(string label)
    {
        Label = label;
        BufferId = GL.GenBuffer();
        m_pinnedArray = GCHandle.Alloc(Data.Data, GCHandleType.Pinned);
        m_vboArrayPtr = m_pinnedArray.AddrOfPinnedObject();
        m_dataVersion = Data.Version;

        Bind();
        GLHelper.ObjectLabel(ObjectLabelIdentifier.Buffer, BufferId, $"{LabelPrefix}: {label}");
        Unbind();
    }

    ~BufferObject()
    {
        Dispose(false);
    }

    protected abstract void PerformUpload();

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

    protected abstract void BufferSubData(int start, int length);

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
        GL.BindBuffer(Target, BufferId);
    }

    public void Unbind()
    {
        GL.BindBuffer(Target, 0);
    }

    protected virtual void Dispose(bool disposing)
    {
        if (m_disposed)
            return;

        GL.DeleteBuffer(BufferId);
        Data = null!; // Encourage the GC to collect things.
        m_pinnedArray.Free();

        m_disposed = true;
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
}
