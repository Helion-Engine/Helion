using Helion.Render.OpenGL.Context;
using OpenTK.Graphics.OpenGL;
using System;
using System.Linq;

namespace Helion.Render.OpenGL.Buffer.Array;

public abstract class ArrayBufferObject<T> : BufferObject<T> where T : struct
{
    protected ArrayBufferObject(string objectLabel) : base(objectLabel)
    {
    }

    protected override BufferTarget GetBufferType() => BufferTarget.ArrayBuffer;

    protected override void PerformUpload()
    {
        GL.BufferData(GetBufferType(), BytesPerElement * Data.Length, Data.Data, GetBufferUsageType());
    }

    protected override void BufferSubData(int index, int length)
    {
        IntPtr offset = new(BytesPerElement * index);
        int size = BytesPerElement * length;
        IntPtr ptr = GetVboArray();

        GL.BufferSubData(GetBufferType(), offset, size, ptr);
    }

    protected abstract BufferUsageHint GetBufferUsageType();
}
