using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Helion.Util;

public static class StreamExtensions
{
    public static T ReadStructure<T>(this Stream stream, byte[]? buffer = null) where T : struct
    {
        if (buffer == null)
            buffer = new byte[Marshal.SizeOf(typeof(T))];

        int offset = 0;
        while (offset < buffer.Length)
            offset += stream.Read(buffer, offset, buffer.Length - offset);

        GCHandle handle = GCHandle.Alloc(buffer, GCHandleType.Pinned);
        T? obj = (T?)Marshal.PtrToStructure(handle.AddrOfPinnedObject(), typeof(T));
        handle.Free();
        return obj ?? default;
    }

    public static void WriteStructure<T>(this Stream stream, T obj, byte[]? buffer = null) where T : struct
    {
        int size = Marshal.SizeOf(typeof(T));
        if (buffer == null)
            buffer = new byte[size];

        IntPtr ptr = Marshal.AllocHGlobal(size);
        Marshal.StructureToPtr(obj, ptr, true);
        Marshal.Copy(ptr, buffer, 0, size);
        stream.Write(buffer, 0, size);
        Marshal.FreeHGlobal(ptr);
    }
}
