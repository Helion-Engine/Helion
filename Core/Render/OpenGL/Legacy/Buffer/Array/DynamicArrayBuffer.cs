using Helion.Render.OpenGL.Legacy.Context;
using Helion.Render.OpenGL.Legacy.Context.Types;

namespace Helion.Render.OpenGL.Legacy.Buffer.Array
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