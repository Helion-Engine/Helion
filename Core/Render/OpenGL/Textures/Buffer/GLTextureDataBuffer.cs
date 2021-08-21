using System;
using System.Drawing;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Planes;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Capabilities;
using Helion.Render.OpenGL.Textures.Buffer.Data;
using Helion.Render.OpenGL.Textures.Types;
using Helion.Resources;
using Helion.Util;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Textures.Buffer
{
    public class GLTextureDataBuffer : IDisposable
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly GLTexture2D Texture;
        private readonly IResources m_resources;
        private readonly Dimension m_dimension;
        private bool m_disposed;
        private int m_rowMask;
        private BufferOffset m_textureOffset;
        private BufferOffset m_entityOffset;
        private BufferOffset m_sectorOffset;

        private int TexelPitch => m_dimension.Width;
        private int TexelPitchFloats => TexelPitch * 4;

        public GLTextureDataBuffer(IResources resources)
        {
            m_dimension = CalculateDimension();
            Log.Debug($"Creating texture buffer of size {m_dimension}");
            
            m_rowMask = CalculateRowMask(m_dimension.Width);
            Texture = new GLTexture2D("Texture buffer data", m_dimension);
            m_resources = resources;
            m_textureOffset = CalculateTextureOffset();
            m_entityOffset = CalculateFrameOffset(m_textureOffset.RowStart + m_textureOffset.RowCount);
            m_sectorOffset = CalculateSectorOffset(m_entityOffset.RowStart + m_entityOffset.RowCount);
        }
        
        private static int CalculateRowMask(int widthTexelsPow2) => widthTexelsPow2 - 1; 

        private static Dimension CalculateDimension()
        {
            int dim = Math.Min(4096, GLCapabilities.Limits.MaxTexture2DSize);
            Invariant(dim > 0, "Should never have a zero width max size GL texture");

            // Floor to the largest power of two.
            int highestBit = MathHelper.HighestBitIndex(dim);
            dim = 1 << highestBit;
            return (dim, dim);
        }
        
        ~GLTextureDataBuffer()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        private BufferOffset CalculateTextureOffset()
        {
            // We want a buffer of 2x, since more might get loaded in.
            int expectedSize =  m_resources.Textures.EstimatedTextureCount * 2;
            
            // Each texture is a [float x1, y1, x2, y2; u1, v1, u2, v2], so 2 texels.
            int numTexels = expectedSize * 2;
            
            // Calculate equal or next largest power of two.
            bool exactFit = numTexels % TexelPitch == 0;
            int numRows = (numTexels / TexelPitch) + (exactFit ? 0 : 1);
            
            // The textures come first, so we start at the first row.
            return new BufferOffset(0, numRows);
        }
        
        private BufferOffset CalculateFrameOffset(int rowStart)
        {
            int frameCount = m_resources.EntityFrameTable.Frames.Count;
            
            // Each frame is: [int textureIndex, int flags, vec2 offset], which goes in one texel.
            int numTexels = frameCount;
            
            bool exactFit = numTexels % TexelPitch == 0;
            int numRows = (numTexels / TexelPitch) + (exactFit ? 0 : 1);
            
            return new BufferOffset(rowStart, numRows);
        }
        
        private BufferOffset CalculateSectorOffset(int rowStart)
        {
            // TODO: [vec4 planeStart; vec4 planeEnd; vec4 rgba; int textureIndex, float lightLevel]

            // It's up to someone else to populate this.
            return new BufferOffset(rowStart, 0);
        }
        
        private void UploadAllTextures()
        {
            // TODO
        }
        
        private void UploadAllFrames()
        {
            // TODO
        }

        public void SetTexture(int index, TextureData data)
        {
            // TODO: Check if out of range.
            
            // TODO
        }
        
        public void SetFrame(int index, FrameData data)
        {
            // TODO: Check if out of range.
            
            // TODO
        }
        
        public void SetSector(int index, SectorData data)
        {
            // TODO: Check if out of range.
            
            // TODO
        }
        
        public void Dispose()
        {
            GC.SuppressFinalize(this);
            PerformDispose();
        }

        private void PerformDispose()
        {
            if (m_disposed)
                return;
            
            Texture.Dispose();

            m_disposed = true;
        }
    }
}
