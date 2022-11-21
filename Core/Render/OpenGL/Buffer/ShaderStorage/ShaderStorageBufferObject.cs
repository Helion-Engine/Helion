using Helion;
using Helion.Render;
using Helion.Render.OpenGL.Buffer;
using Helion.Render.OpenGL.Buffer.ShaderStorage;
using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;
using Helion.Render.OpenGL.Util;

namespace Helion.Render.OpenGL.Buffer.ShaderStorage;

public class ShaderStorageBufferObject<T> : ShaderDataBufferObject<T> where T : struct
{
    public ShaderStorageBufferObject(GLCapabilities capabilities, IGLFunctions functions, BindingPoint bindPoint, string objectLabel = "") :
        base(capabilities, functions, bindPoint, objectLabel)
    {
    }

    public ShaderStorageBufferObject(GLCapabilities capabilities, IGLFunctions functions, int bindIndex, string objectLabel = "") :
        base(capabilities, functions, bindIndex, objectLabel)
    {
    }

    protected override BufferType GetBufferType() => BufferType.ShaderStorageBuffer;

    protected override void PerformUpload()
    {
        gl.BufferStorage(BufferType.ShaderStorageBuffer, TotalBytes, Data.Data, BufferStorageFlagType.DynamicStorage);
        gl.BindBufferBase(BufferRangeType.ShaderStorageBuffer, BindIndex, BufferId);
    }
}
