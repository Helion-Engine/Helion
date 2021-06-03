using Helion.Render.Legacy.Context;
using Helion.Render.Legacy.Context.Types;

namespace Helion.Render.Legacy.Buffer.Array
{
    public class StaticArrayBuffer<T> : ArrayBufferObject<T> where T : struct
    {
        public StaticArrayBuffer(GLCapabilities capabilities, IGLFunctions functions, string objectLabel = "") : 
            base(capabilities, functions, objectLabel)
        {
        }

        protected override BufferUsageType GetBufferUsageType() => BufferUsageType.StaticDraw;
    }
}