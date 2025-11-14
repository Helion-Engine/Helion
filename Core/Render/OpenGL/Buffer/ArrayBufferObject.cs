using System;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Buffer.Array;

public abstract class ArrayBufferObject<T> : BufferObject<T> where T : struct
{
    protected override BufferTarget Target => BufferTarget.ArrayBuffer;
    protected abstract BufferUsageHint Hint { get; }

    private IntPtr m_ptr;

    protected unsafe ArrayBufferObject(string objectLabel, int capacity = DefaultCapacity) : base(objectLabel, capacity)
    {
        fixed (T* ptr = &Data.Data[0])
        {
            m_ptr = (IntPtr)ptr;
        }
    }

    protected override void PerformUpload()
    {
        GL.BufferData(Target, BytesPerElement * Data.Length, Data.Data, Hint);
    }

    protected override void PerformUploadCapacity()
    {
        GL.BufferData(Target, BytesPerElement * Data.Capacity, Data.Data, Hint);
    }

    protected unsafe override void BufferSubData(int index, int length)
    {
        fixed (T* buffer = &Data.Data[0])
        {
            var ptr = (IntPtr)buffer;
            // If the underlying array was resized then the new array needs to be uploaded
            // This should be handled with BufferObject.UploadIfNeeded
            if (ptr != m_ptr)
            {
                m_ptr = ptr;
                Uploaded = false;
                return;
            }

            IntPtr offset = new(BytesPerElement * index);
            int size = BytesPerElement * length;

            GL.BufferSubData(Target, offset, size, ptr + (BytesPerElement * index));
        }
    }
}
