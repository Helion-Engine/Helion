using System;
using Helion.Util.Assertion;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Buffer.Array;

public abstract class ArrayBufferObject<T> : BufferObject<T> where T : struct
{
    protected override BufferTarget Target => BufferTarget.ArrayBuffer;
    protected abstract BufferUsageHint Hint { get; }

    private IntPtr m_ptr;
    private bool m_initialized;
    private int m_uploadedSize;

    protected unsafe ArrayBufferObject(string objectLabel, int capacity = DefaultCapacity) : base(objectLabel, capacity)
    {
        fixed (T* ptr = &Data.Data[0])
        {
            m_ptr = (IntPtr)ptr;
        }
    }

    protected override void PerformUpload()
    {
        m_initialized = true;
        m_uploadedSize = BytesPerElement * Data.Length;
        GL.BufferData(Target, m_uploadedSize, Data.Data, Hint);
    }

    protected override void PerformUploadCapacity()
    {
        m_initialized = true;
        m_uploadedSize = BytesPerElement * Data.Capacity;
        GL.BufferData(Target, m_uploadedSize, Data.Data, Hint);
    }

    protected unsafe override bool BufferSubData(int index, int length)
    {
        if (!Uploaded || !m_initialized)
        {
            Uploaded = false;
            return false;
        }

        fixed (T* buffer = &Data.Data[0])
        {
            var ptr = (IntPtr)buffer;
            // If the underlying array was resized then the new array needs to be uploaded
            // This should be handled with BufferObject.UploadIfNeeded
            if (ptr != m_ptr)
            {
                m_ptr = ptr;
                Uploaded = false;
                return false;
            }

            IntPtr offset = new(BytesPerElement * index);
            int size = BytesPerElement * length;

            Assert.Precondition(m_uploadedSize >= offset + size, "Offset and size are out of bounds for the GPU");
            GL.BufferSubData(Target, offset, size, ptr + (BytesPerElement * index));
        }
        return true;
    }
}
