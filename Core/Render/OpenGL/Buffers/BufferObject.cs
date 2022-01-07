using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using Helion.Util.Container;

namespace Helion.Render.OpenGL.Buffers;

public abstract class BufferObject<T> : IDisposable where T : struct
{
    public static readonly int BytesPerElement = Marshal.SizeOf<T>();

    protected readonly DynamicArray<T> Data = new();

    public int TotalBytes => Count * BytesPerElement;
    public int Count => Data.Length;

    public T this[int index] => Data[index];

    public void Clear()
    {
        Data.Clear();
    }

    public abstract void Dispose();
}
