using System;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Buffers
{
    // TODO: Perform sanity checks against GL_MAX_TEXTURE_BUFFER_SIZE.
    
    public class TextureBufferObject<T> : BufferObject<T> where T : struct
    {
        private readonly int tbo;
        
        public TextureBufferObject(BufferUsageHint usageHint = BufferUsageHint.DynamicDraw) : 
            base(BufferTarget.TextureBuffer, usageHint, GL.GenBuffer())
        {
            tbo = GL.GenTexture();
            BindTextureToBuffer();
        }

        protected override void ReleaseUnmanagedResources()
        {
            GL.DeleteTexture(tbo);
            
            base.ReleaseUnmanagedResources();
        }

        // TODO: Want to support updating a range and pushing that through to
        //       the texture buffer. Can we do that with this[] indexing and
        //       not kill performance? Use glMapBuffer of glSubBufferData...?

        private void BindTextureToBuffer()
        {
            GL.BindTexture(TextureTarget.TextureBuffer, tbo);
            GL.TexBuffer(TextureBufferTarget.TextureBuffer, SizedInternalFormat.R32f, bufferHandle);
            GL.BindTexture(TextureTarget.TextureBuffer, 0);
        }

        public void BindTexture()
        {
            GL.BindTexture(TextureTarget.TextureBuffer, tbo);
        }
        
        public void UnbindTexture()
        {
            GL.BindTexture(TextureTarget.TextureBuffer, 0);
        }

        public void BindTextureAnd(Action func)
        {
            BindTexture();
            func.Invoke();
            UnbindTexture();
        }
    }
}