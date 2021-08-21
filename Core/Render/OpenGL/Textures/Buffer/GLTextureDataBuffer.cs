using System;
using System.Collections.Generic;
using Helion.Geometry;
using Helion.Render.OpenGL.Capabilities;
using Helion.Render.OpenGL.Textures.Buffer.Data;
using Helion.Render.OpenGL.Textures.Types;
using Helion.Resources;
using Helion.Util;
using Helion.World.Entities.Definition.States;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Textures.Buffer
{
    /// <summary>
    /// A texture buffer that shaders can reference for
    /// </summary>
    public class GLTextureDataBuffer : IDisposable
    {
        public const int FloatsPerTexel = 4;
        public const int BytesPerTexel = FloatsPerTexel * sizeof(float);
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly GLTexture2D Texture;
        private readonly IResources m_resources;
        private readonly Dimension m_dimension;
        private readonly DataBufferSection<TextureData> m_textureData;
        private readonly DataBufferSection<FrameData> m_frameData;
        private readonly DataBufferSection<SectorPlaneData> m_sectorData;
        private bool m_disposed;
        private int m_rowMask;

        private int TexelPitch => m_dimension.Width;

        public GLTextureDataBuffer(IResources resources)
        {
            m_dimension = CalculateDimension();
            Log.Debug($"Creating texture buffer of size {m_dimension}");
            
            m_rowMask = CalculateRowMask(m_dimension.Width);
            Texture = new GLTexture2D("Texture buffer data", m_dimension);
            Log.Error("ERROR: Using a mipmapped texture buffer");
            m_resources = resources;
            m_textureData = CreateTextureData();
            m_frameData = CreateFrameData();
            m_sectorData = CreateSectorPlaneData();
        }

        ~GLTextureDataBuffer()
        {
            FailedToDispose(this);
            PerformDispose();
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
        
        private DataBufferSection<TextureData> CreateTextureData()
        {
            // We want a buffer of 2x, since more might get loaded in.
            int expectedTextures =  m_resources.Textures.EstimatedTextureCount * 2;
            int texelsPerTexture = TexelPitch / DataBufferSection<TextureData>.TexelSize;
            int texelsNeeded = texelsPerTexture * expectedTextures;

            int rowsNeeded = texelsNeeded / TexelPitch;
            if (texelsNeeded % TexelPitch != 0)
                rowsNeeded++;
            
            return new DataBufferSection<TextureData>(0, rowsNeeded, TexelPitch);
        }
        
        private DataBufferSection<FrameData> CreateFrameData()
        {
            List<EntityFrame> frames = m_resources.EntityFrameTable.Frames;
            
            int texelsPerFrame = TexelPitch / DataBufferSection<FrameData>.TexelSize;
            int texelsNeeded = texelsPerFrame * frames.Count;

            int rowsNeeded = texelsNeeded / TexelPitch;
            if (texelsNeeded % TexelPitch != 0)
                rowsNeeded++;

            int rowStart = m_textureData.RowStart + m_textureData.RowCount;
            return new DataBufferSection<FrameData>(rowStart, rowsNeeded, TexelPitch);
        }
        
        private DataBufferSection<SectorPlaneData> CreateSectorPlaneData()
        {
            // We don't store any right now, we'll make space for a single one
            // so we allocate at least one row.
            const int count = 1;
            
            int texelsPerFrame = TexelPitch / DataBufferSection<SectorPlaneData>.TexelSize;
            int texelsNeeded = texelsPerFrame * count;
            int rowsNeeded = texelsNeeded / TexelPitch;
            if (texelsNeeded % TexelPitch != 0)
                rowsNeeded++;
            
            int rowStart = m_frameData.RowStart + m_frameData.RowCount;
            return new DataBufferSection<SectorPlaneData>(rowStart, rowsNeeded, TexelPitch);
        }

        public void SetTexture(int index, TextureData data)
        {
            if (index < 0 || index >= m_textureData.Count)
            {
                string errorMsg = $"Trying to write texture out of range of texture buffer: {index} / {m_textureData.Count}";
                Log.Error(errorMsg);
                Fail(errorMsg);
                return;
            }
            
            // TODO
        }
        
        public void SetFrame(int index, FrameData data)
        {
            if (index < 0 || index >= m_frameData.Count)
            {
                string errorMsg = $"Trying to write frame out of range of texture buffer: {index} / {m_frameData.Count}";
                Log.Error(errorMsg);
                Fail(errorMsg);
                return;
            }
            
            // TODO
        }
        
        public void SetSectorPlane(int index, SectorPlaneData planeData)
        {
            if (index < 0 || index >= m_sectorData.Count)
            {
                string errorMsg = $"Trying to write sector plane out of range of texture buffer: {index} / {m_sectorData.Count}";
                Log.Error(errorMsg);
                Fail(errorMsg);
                return;
            }
            
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
