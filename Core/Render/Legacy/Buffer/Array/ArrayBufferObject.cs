using Helion.Render.Legacy.Context;
using Helion.Render.Legacy.Context.Types;
using System.Linq;

namespace Helion.Render.Legacy.Buffer.Array;

public abstract class ArrayBufferObject<T> : BufferObject<T> where T : struct
{
    protected ArrayBufferObject(GLCapabilities capabilities, IGLFunctions functions, string objectLabel = "") :
        base(capabilities, functions, objectLabel)
    {
    }

    protected override BufferType GetBufferType() => BufferType.ArrayBuffer;

    protected override void PerformUpload()
    {
        gl.BufferData(GetBufferType(), BytesPerElement * Data.Length, Data.Data, GetBufferUsageType());
    }

    protected override void BufferSubData(int index, int length)
    {
        gl.BufferSubData<T>(GetBufferType(), BytesPerElement * index, BytesPerElement * length, GetVboArray(), BytesPerElement * index);
    }

    protected abstract BufferUsageType GetBufferUsageType();
}
