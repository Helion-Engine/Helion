using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;

namespace Helion.Render.OpenGL.Buffer.Array
{
    public class DynamicArrayBuffer<T> : ArrayBufferObject<T> where T : struct
    {
        public DynamicArrayBuffer(GLCapabilities capabilities, IGLFunctions functions, string objectLabel = "") : 
            base(capabilities, functions, objectLabel)
        {
        }

        protected override BufferUsageType GetBufferUsageType() => BufferUsageType.DynamicDraw;
    }
}