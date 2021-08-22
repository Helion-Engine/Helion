using System;
using System.Collections.Generic;
using System.Diagnostics;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Capabilities;
using Helion.Render.OpenGL.Textures.Buffer.Data;
using Helion.Render.OpenGL.Textures.Types;
using Helion.Render.OpenGL.Util;
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

        public readonly GLTextureBuffer2D Texture;
        private readonly IResources m_resources;
        private readonly Dimension m_dimension;
        private readonly DataBufferSection<TextureData> m_textureData;
        private readonly DataBufferSection<FrameData> m_frameData;
        private readonly DataBufferSection<SectorPlaneData> m_sectorData;
        private bool m_disposed;

        private int TexelPitch => m_dimension.Width;

        public GLTextureDataBuffer(IResources resources)
        {
            m_dimension = CalculateDimension();
            Log.Debug($"Creating texture buffer of size {m_dimension}");
            
            Texture = new GLTextureBuffer2D("Data buffer", m_dimension);
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
            // Because texture counts are not loaded when we create this, and
            // because we know we have at least 2048^2 texels at a minimum, we
            // can assume 32k textures safely. In the future we can make this
            // dynamic, but this will be sufficient for now.
            const int expectedTextures =  32 * 1024;
            int texelsPerTexture = TexelPitch / TextureData.TexelSize;
            int texelsNeeded = texelsPerTexture * expectedTextures;

            int rowsNeeded = texelsNeeded / TexelPitch;
            if (rowsNeeded == 0 || texelsNeeded % TexelPitch != 0)
                rowsNeeded++;
            
            return new DataBufferSection<TextureData>(0, rowsNeeded, TexelPitch, true);
        }
        
        private DataBufferSection<FrameData> CreateFrameData()
        {
            List<EntityFrame> frames = m_resources.EntityFrameTable.Frames;
            
            int texelsPerFrame = TexelPitch / FrameData.TexelSize;
            int texelsNeeded = texelsPerFrame * frames.Count;

            int rowsNeeded = texelsNeeded / TexelPitch;
            if (rowsNeeded == 0 || texelsNeeded % TexelPitch != 0)
                rowsNeeded++;

            int rowStart = m_textureData.RowStart + m_textureData.RowCount;
            return new DataBufferSection<FrameData>(rowStart, rowsNeeded, TexelPitch, true);
        }
        
        private DataBufferSection<SectorPlaneData> CreateSectorPlaneData()
        {
            // We don't store any right now, we'll make space for a single one
            // so we allocate at least one row.
            const int count = 1; // TODO
            
            int texelsPerFrame = TexelPitch / SectorPlaneData.TexelSize;
            int texelsNeeded = texelsPerFrame * count;
            int rowsNeeded = texelsNeeded / TexelPitch;
            if (rowsNeeded == 0 || texelsNeeded % TexelPitch != 0)
                rowsNeeded++;
            
            int rowStart = m_frameData.RowStart + m_frameData.RowCount;
            return new DataBufferSection<SectorPlaneData>(rowStart, rowsNeeded, TexelPitch);
        }
        
        private Vec2I GetCoordinateForTexture(int index)
        {
            int texelOffset = index * TextureData.TexelSize;
            int x = texelOffset % TexelPitch;
            int y = texelOffset / TexelPitch;
            return (x, y);
        }

        [Conditional("DEBUG")]
        private static void LogAndAssertIndexFailure(string dataType, int index, int count)
        {
            string errorMsg = $"Trying to write texture out of range of {dataType} buffer: {index} / {count}";
            Log.Error(errorMsg);
            Fail(errorMsg);
        }
        
        public void SetTexture(GLTextureHandle handle)
        {
            int index = handle.Index;
            Log.ConditionalTrace("Setting texture buffer with texture {Index}", index);
            
            if (index < 0 || index >= m_textureData.Count)
            {
                LogAndAssertIndexFailure("texture", index, m_textureData.Count);
                return;
            }
            
            TextureData data = new(handle.Area.Float, handle.UV);
            m_textureData.Set(index, data);

            Vec2I coordinate = GetCoordinateForTexture(index);
            Texture.Write(coordinate, data, TextureData.TexelSize, Binding.Bind);
        }

        public void SetFrame(int index, FrameData data)
        {
            Log.ConditionalTrace("Setting texture buffer with frame {Index}", index);
            
            if (index < 0 || index >= m_frameData.Count)
            {
                LogAndAssertIndexFailure("frame", index, m_frameData.Count);
                return;
            }
            
            // TODO
        }
        
        public void SetSectorPlane(int index, SectorPlaneData data)
        {
            Log.ConditionalTrace("Setting texture buffer with sector plane {Index}", index);
            
            if (index < 0 || index >= m_sectorData.Count)
            {
                LogAndAssertIndexFailure("sector plane", index, m_sectorData.Count);
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
