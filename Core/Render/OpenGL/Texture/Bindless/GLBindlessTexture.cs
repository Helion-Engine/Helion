using Helion.Render.OpenGL.Context;
using Helion.Util.Geometry;

namespace Helion.Render.OpenGL.Texture.Bindless
{
    public class GLBindlessTexture : GLTexture
    {
        public bool IsResident { get; private set; }
        private readonly long m_bindlessHandle;
        
        public GLBindlessTexture(int id, int textureId, Dimension dimension, IGLFunctions functions, long bindlessHandle) : 
            base(id, textureId, dimension, functions)
        {
            m_bindlessHandle = bindlessHandle;
        }

        public void MakeResident()
        {
            gl.MakeTextureHandleResidentARB(m_bindlessHandle);
            IsResident = true;
        }

        public void MakeNonResident()
        {
            gl.MakeTextureHandleNonResident(m_bindlessHandle);
            IsResident = false;
        }
        
        protected override void ReleaseUnmanagedResources()
        {
            if (IsResident)
                MakeNonResident();
        }
    }
}