using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Helion.Render.Legacy.Context;
using Helion.Render.Legacy.Context.Types;
using Helion.Render.Legacy.Util;
using Helion.Util.Container;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.Legacy.Buffer;

/// <summary>
/// Represents a buffer object on the GPU.
/// </summary>
/// <typeparam name="T">The type to hold, which must be a struct that is
/// packed.</typeparam>
public abstract class BufferObject<T> : IDisposable where T : struct
{
    /// <summary>
    /// How many bytes are used per each element.
    /// </summary>
    public static readonly int BytesPerElement = Marshal.SizeOf<T>();

    /// <summary>
    /// The ID of the buffer allocated by glGenBuffers().
    /// </summary>
    protected readonly int BufferId;

    /// <summary>
    /// The functions used to call various GL commands.
    /// </summary>
    protected readonly IGLFunctions gl;

    /// <summary>
    /// The data of the buffer.
    /// </summary>
    /// <remarks>
    /// Any actions on the client should keep this in sync with the
    /// respective buffer on the GPU. If not, any time a new upload is
    /// performed, the previous data on the GPU will be overwritten.
    /// </remarks>
    public DynamicArray<T> Data = new DynamicArray<T>();

    /// <summary>
    /// Whether it was uploaded or not. This is only called upon binding
    /// and if a new item was added since the last upload, or it was
    /// cleared since the last upload.
    /// </summary>
    protected bool Uploaded;

    /// <summary>
    /// How many items have been added to this buffer.
    /// </summary>
    public int Count => Data.Length;

    /// <summary>
    /// Checks if this buffer has no elements.
    /// </summary>
    public bool Empty => Count == 0;

    /// <summary>
    /// If an upload is needed.
    /// </summary>
    public bool NeedsUpload => !Uploaded && Count > 0;

    /// <summary>
    /// How many bytes should be allocated to hold all the data currently.
    /// </summary>
    public int TotalBytes => Count * BytesPerElement;

    /// <summary>
    /// Creates a new buffer object and sets the label if the capabilities
    /// exist.
    /// </summary>
    /// <param name="capabilities">The GL capabilities.</param>
    /// <param name="functions">The GL functions.</param>
    /// <param name="objectLabel">The label, otherwise none is set if this
    /// is omitted or empty. It is an optional parameter.</param>
    protected BufferObject(GLCapabilities capabilities, IGLFunctions functions, string objectLabel = "")
    {
        gl = functions;
        BufferId = gl.GenBuffer();

        Bind();
        SetObjectLabel(capabilities, objectLabel);
        Unbind();
    }

    ~BufferObject()
    {
        FailedToDispose(this);
        ReleaseUnmanagedResources();
    }

    /// <summary>
    /// Writes data to the index provided and sends it to the buffer.
    /// </summary>
    /// <remarks>
    /// This can have significant overhead compared to mapping the buffer
    /// and setting it that way. This should only be used when you know
    /// there will be few calls to it since a bunch of uploads may have a
    /// significant performance overhead.
    /// </remarks>
    /// <param name="index">The index to write at.</param>
    /// <exception cref="IndexOutOfRangeException">If the index it out of
    /// range.</exception>
    public virtual T this[int index]
    {
        set
        {
            if (index < 0 || index >= Count)
                throw new IndexOutOfRangeException($"Trying to set buffer object out of range: {index} (size = {Count})");
            gl.BufferSubData(GetBufferType(), index * BytesPerElement, BytesPerElement, value);
        }
    }

    /// <summary>
    /// Adds a single element.
    /// </summary>
    /// <param name="element">The element to add.</param>
    public void Add(T element)
    {
        Data.Add(element);
        Uploaded = false;
    }

    /// <summary>
    /// Adds a series of elements. Does nothing if no elements are present
    /// in the array/params.
    /// </summary>
    /// <param name="elements">The elements to add.</param>
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

    /// <summary>
    /// Performs an upload of the data if required.
    /// </summary>
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
        Uploaded = true;
    }

    protected virtual void BufferSubData(int start, int length)
    {

    }

    /// <summary>
    /// Checks to see if an upload is needed, and uploads. This will bind
    /// the object, upload, and unbind, meaning any bind state will be
    /// altered by this.
    /// </summary>
    public void UploadIfNeeded()
    {
        if (NeedsUpload)
        {
            Bind();
            Upload();
            Unbind();
        }
    }

    /// <summary>
    /// Clears the data.
    /// </summary>
    public void Clear()
    {
        Data.Clear();
        Uploaded = false;
    }

    /// <summary>
    /// Binds the buffer.
    /// </summary>
    public void Bind()
    {
        gl.BindBuffer(GetBufferType(), BufferId);
    }

    /// <summary>
    /// Unbinds the buffer.
    /// </summary>
    public void Unbind()
    {
        gl.BindBuffer(GetBufferType(), 0);
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }

    protected virtual void ReleaseUnmanagedResources()
    {
        gl.DeleteBuffer(BufferId);

        // Since VBOs can end up holding a lot of data, if we dispose of it
        // but take a while to lose the reference, we still want to leave
        // the option for the GC to retrieve memory.
#nullable disable
        Data = null;
#nullable enable
    }

    /// <summary>
    /// Gets the buffer type.
    /// </summary>
    /// <returns>The type of buffer this is.</returns>
    protected abstract BufferType GetBufferType();

    /// <summary>
    /// Performs the upload of data. This function may assume that the
    /// buffer has been bound.
    /// </summary>
    protected abstract void PerformUpload();

    [Conditional("DEBUG")]
    private void SetObjectLabel(GLCapabilities capabilities, string objectLabel)
    {
        GLHelper.ObjectLabel(gl, capabilities, ObjectLabelType.Buffer, BufferId, objectLabel);
    }
}
