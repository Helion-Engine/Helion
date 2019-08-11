using Helion.Render.OpenGL.Context;
using Helion.Util.Geometry;

namespace Helion.Render.OpenGL.Texture.Bindless
{
    public class GLBindlessTexture : GLTexture
    {
        public readonly long BindlessHandle;
        public bool IsResident { get; private set; }
        
        public GLBindlessTexture(int id, int textureId, Dimension dimension, IGLFunctions functions, long bindlessHandle) : 
            base(id, textureId, dimension, functions)
        {
            BindlessHandle = bindlessHandle;
        }

        public void MakeResident()
        {
            gl.MakeTextureHandleResidentARB(BindlessHandle);
            IsResident = true;
        }

        public void MakeNonResident()
        {
            gl.MakeTextureHandleNonResident(BindlessHandle);
            IsResident = false;
        }
        
        protected override void ReleaseUnmanagedResources()
        {
            if (IsResident)
                MakeNonResident();

            base.ReleaseUnmanagedResources();
        }
    }
}