using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Buffers
{
    public class TextureBufferObject<T> : BufferObject<T> where T : struct
    {
        public TextureBufferObject(BufferUsageHint usageHint = BufferUsageHint.DynamicDraw) : 
            base(BufferTarget.TextureBuffer, usageHint, GL.GenBuffer())
        {
        }
        
        // TODO: Want to support updating a range and pushing that through to
        //       the texture buffer. Can we do that with this[] indexing and
        //       not kill performance? Use glMapBuffer of glSubBufferData...?
    }
}