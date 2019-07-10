using System;
using Helion.Render.OpenGL.Buffers;
using Helion.Render.OpenGL.Util;
using Helion.Util.Container;
using static Helion.Util.Assert;

namespace Helion.Render.OpenGL.Texture
{
    /// <summary>
    /// Responsible for writing texture information to a texture buffer object
    /// so the shaders can access the data by an index-based handle.
    /// </summary>
    public class GLTextureDataBuffer : IDisposable
    {
        /// <summary>
        /// The texture buffer object which holds all the texture info data.
        /// </summary>
        public readonly TextureBufferObject<TextureInfo> TextureInfoBuffer;
        
        /// <summary>
        /// Unique contiguous indices that can be used as texture lookup
        /// indices.
        /// </summary>
        private readonly AvailableIndexTracker m_availableTextureIndex = new AvailableIndexTracker();

        /// <summary>
        /// Creates an empty texture data buffer wrapper object.
        /// </summary>
        /// <param name="capabilities">The GL capabilities.</param>
        public GLTextureDataBuffer(GLCapabilities capabilities)
        {
            TextureInfoBuffer = new TextureBufferObject<TextureInfo>(capabilities, "Texture Info TBO");
        }

        ~GLTextureDataBuffer()
        {
            ReleaseUnmanagedResources();
        }
        
        /// <summary>
        /// Allocates a handle. This must be returned to this object by calling
        /// <see cref="FreeTextureDataIndex"/> when done with the handle.
        /// </summary>
        /// <returns>A newly allocated handle as an integer.</returns>
        public int AllocateTextureDataIndex() => m_availableTextureIndex.Next();
        
        /// <summary>
        /// Tracks the texture by writing the data into the buffer for usage in
        /// the shaders.
        /// </summary>
        /// <param name="texture">The texture to track.</param>
        public void Track(GLTexture texture)
        {
            Precondition(m_availableTextureIndex.IsTracked(texture.TextureInfoIndex), "GL texture index is not tracked by this object");

            if (texture.TextureInfoIndex == TextureInfoBuffer.Count)
                TextureInfoBuffer.Add(new TextureInfo(texture));
            else
                throw new NotImplementedException("Do not support texture data buffer overwriting of discarded spaces yet");
        }

        /// <summary>
        /// Makes the index free to be used by another texture.
        /// </summary>
        /// <param name="textureInfoIndex">The index to free.</param>
        public void Remove(int textureInfoIndex)
        {
            Precondition(m_availableTextureIndex.IsTracked(textureInfoIndex), "GL texture index is not tracked by this object, cannot remove");

            m_availableTextureIndex.MakeAvailable(textureInfoIndex);

            throw new NotImplementedException("Buffer objects do not support deletion currently");
        }

        public void BindAnd(Action func)
        {
            TextureInfoBuffer.BindTextureAnd(GLConstants.TextureInfoUnit, func);
        }

        /// <inheritdoc/>
        public void Dispose()
        {
            ReleaseUnmanagedResources();
            GC.SuppressFinalize(this);
        }

        private void ReleaseUnmanagedResources()
        {
            TextureInfoBuffer.Dispose();
        }
    }
}