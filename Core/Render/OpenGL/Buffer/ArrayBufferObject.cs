using Helion.Render.OpenGL.Context;
using OpenTK.Graphics.OpenGL;
using System;
using System.Linq;

namespace Helion.Render.OpenGL.Buffer.Array;

public abstract class ArrayBufferObject<T> : BufferObject<T> where T : struct
{
    protected override BufferTarget Target => BufferTarget.ArrayBuffer;
    protected abstract BufferUsageHint Hint { get; }

    protected ArrayBufferObject(string objectLabel) : base(objectLabel)
    {
    }

    protected override void PerformUpload()
    {
        GL.BufferData(Target, BytesPerElement * Data.Length, Data.Data, Hint);
    }

    protected override void BufferSubData(int index, int length)
    {
        IntPtr offset = new(BytesPerElement * index);
        int size = BytesPerElement * length;
        IntPtr ptr = GetVboArray() + (BytesPerElement * index);

        GL.BufferSubData(Target, offset, size, ptr);
    }
}

public abstract class DynamicArrayBuffer<T> : ArrayBufferObject<T> where T : struct
{
    protected override BufferUsageHint Hint => BufferUsageHint.DynamicDraw;

    protected DynamicArrayBuffer(string objectLabel) : base(objectLabel)
    {
    }
}

public abstract class StaticArrayBuffer<T> : ArrayBufferObject<T> where T : struct
{
    protected override BufferUsageHint Hint => BufferUsageHint.StaticDraw;

    protected StaticArrayBuffer(string objectLabel) : base(objectLabel)
    {
    }
}

public abstract class StreamArrayBuffer<T> : ArrayBufferObject<T> where T : struct
{
    protected override BufferUsageHint Hint => BufferUsageHint.StreamDraw;

    protected StreamArrayBuffer(string objectLabel) : base(objectLabel)
    {
    }
}
