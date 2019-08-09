using Helion.Render.OpenGL.Context;
using Helion.Util.Geometry;
using OpenTK.Graphics.OpenGL4;

namespace Helion.Render.OpenGL.Texture.Bindless
{
    public class GLBindlessTexture : GLTexture
    {
        public bool IsResident { get; private set; }
        private readonly ulong BindlessHandle;
        
        public GLBindlessTexture(int id, int textureId, Dimension dimension, IGLFunctions functions, ulong bindlessHandle) : 
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
        }
    }
}