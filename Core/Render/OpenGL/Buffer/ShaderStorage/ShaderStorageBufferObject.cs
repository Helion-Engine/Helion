using Helion.Render.OpenGL.Context;
using Helion.Render.OpenGL.Context.Types;
using Helion.Render.OpenGL.Util;

namespace Helion.Render.OpenGL.Buffer.ShaderStorage
{
    public class ShaderStorageBufferObject<T> : BufferObject<T> where T : struct
    {
        private readonly int m_bindIndex;
        
        public ShaderStorageBufferObject(GLCapabilities capabilities, IGLFunctions functions, BindingPoint bindPoint, string objectLabel = "") : 
            this(capabilities, functions, (int)bindPoint, objectLabel)
        {
        }
        
        public ShaderStorageBufferObject(GLCapabilities capabilities, IGLFunctions functions, int bindIndex, string objectLabel = "") : 
            base(capabilities, functions, objectLabel)
        {
            m_bindIndex = bindIndex;
        }
        
        // TODO

        protected override BufferType GetBufferType() => BufferType.ShaderStorageBuffer;

        protected override void PerformUpload()
        {
            // This one should use:
            // GL.BufferStorage(BufferTarget.ShaderStorageBuffer, byteCount, data, BufferStorageFlags.DynamicStorageBit);
            
            // A child implementation of a mapped buffer should use:
            // ALSO: an [] impl should only update with the pointer if it's not
            // in the "to be added" range so if 10 units have been uploaded and
            // a change happens at index 5, it uses the mapped pointer. If an
            // updated happens at index 12, then it will update the data buffer
            // instead (since the pointer would be out of range and segfault).
            // GL.BufferStorage(BufferTarget.ShaderStorageBuffer, byteCount, data, GL_MAP_WRITE_BIT | GL_MAP_PERSISTENT_BIT | GL_MAP_COHERENT_BIT);
        }
    }
}