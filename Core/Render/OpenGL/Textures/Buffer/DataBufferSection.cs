using System;
using System.Runtime.InteropServices;
using Helion.Util.Container;
using static Helion.Render.OpenGL.Textures.Buffer.GLTextureDataBuffer;
using static Helion.Util.Assertion.Assert;
using static Helion.Util.MathHelper;

namespace Helion.Render.OpenGL.Textures.Buffer
{
    /// <summary>
    /// Represents a series of rows in the texture data buffer. Allows for easy
    /// upload of new elements as needed and data copying if required.
    /// </summary>
    public class DataBufferSection<T> where T : struct
    {
        public static readonly int StructSize = GetSizeOrThrowIfNotPow2();
        public static readonly int TexelSize = StructSize / (sizeof(float) * FloatsPerTexel);

        public readonly int RowStart;
        public readonly int RowCount;
        private readonly int m_rowTexelPitch;
        private readonly int m_elementsPerRow;
        private readonly int m_texelsPerElement;
        private readonly int m_maxElementsAllowed;
        private readonly DynamicArray<T> m_data;

        public int Count => m_data.Length;
        public T[] Data => m_data.Data;

        public DataBufferSection(int rowStart, int rowCount, int rowTexelPitch)
        {
            Precondition(IsPow2(rowTexelPitch), "Row texel pitch must be a power of two");

            RowStart = rowStart;
            RowCount = rowCount;
            m_rowTexelPitch = rowTexelPitch;
            m_elementsPerRow = rowTexelPitch / StructSize;
            m_texelsPerElement = StructSize / BytesPerTexel;
            m_maxElementsAllowed = rowCount * m_elementsPerRow;
            m_data = new DynamicArray<T>(m_maxElementsAllowed);
        }
        
        public DataBufferSection(int rowStart, int rowCount, int rowTexelPitch, T fillValue) :
            this(rowStart, rowCount, rowTexelPitch)
        {
            for (int i = 0; i < m_maxElementsAllowed; i++)
                m_data.Add(fillValue);
        }

        private static int GetSizeOrThrowIfNotPow2()
        {
            int size = Marshal.SizeOf<T>();
            
            if (size % sizeof(float) != 0)
                throw new Exception($"Data buffer type {typeof(T).Name} does not align to a float");
            if (size % (FloatsPerTexel * sizeof(float)) != 0)
                throw new Exception($"Data buffer type {typeof(T).Name} does not align to a texel");
            
            // This last check also makes sure we can pack an entire row full of
            // structs without padding.
            if (!IsPow2(size))
                throw new Exception($"Data buffer type {typeof(T).Name} is not a power of two");
            
            return size;
        }

        public void Set(int index, T data)
        {
            m_data[index] = data;
        }
    }
}
