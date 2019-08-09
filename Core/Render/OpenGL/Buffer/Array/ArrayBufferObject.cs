using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;
using Helion.Render.OpenGL.Util;

namespace Helion.Render.OpenGL.Buffer.Array
{
    public abstract class ArrayBufferObject<T> : BufferObject<T> where T : struct
    {
        protected ArrayBufferObject(GLCapabilities capabilities, GLFunctions functions, string objectLabel = "") : 
            base(capabilities, functions, objectLabel)
        {
        }

        protected override BufferType GetBufferType() => BufferType.ArrayBuffer;
        
        protected override void PerformUpload()
        {
            gl.BufferData(GetBufferType(), BytesPerElement * Data.Length, Data.Data, GetBufferUsageType());
        }

        protected override void SetObjectLabel(GLCapabilities capabilities, string objectLabel)
        {
            GLHelper.ObjectLabel(gl, capabilities, ObjectLabelType.Buffer, BufferId, objectLabel);
        }
        
        protected abstract BufferUsageType GetBufferUsageType();
    }
}