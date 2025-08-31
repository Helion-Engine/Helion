using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
namespace Helion.Util.Container;

public static class ArrayExtensions
{
    public static void ZeroArray<T>(this T[] array) where T : struct
    {
        ref var reference = ref MemoryMarshal.GetArrayDataReference(array);
        Unsafe.InitBlockUnaligned(ref Unsafe.As<T, byte>(ref reference), 0, (uint)(Marshal.SizeOf<T>() * array.Length));
    }
}
