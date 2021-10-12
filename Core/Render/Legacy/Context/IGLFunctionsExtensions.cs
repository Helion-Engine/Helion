using Helion.Render.Legacy.Context.Types;

namespace Helion.Render.Legacy.Context;

public static class IGLFunctionsExtensions
{
    public static void BufferSubData<T>(this IGLFunctions func, BufferType type, int byteOffset, int numBytes, T value)
        where T : struct
    {
        func.BufferSubData(type, byteOffset, numBytes, new[] { value });
    }
}

