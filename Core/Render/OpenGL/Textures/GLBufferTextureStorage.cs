using Helion.Render.OpenGL.Util;
using Helion.Util.Assertion;
using OpenTK.Graphics.OpenGL;
using System;

namespace Helion.Render.OpenGL.Textures;

public class GLBufferTextureStorage<T> where T : struct
{
    public const int FourComponentLength = 4;

    private readonly GLBufferTexture<T> m_bufferTexture;
    private GLMappedBuffer<T> m_mappedBuffer;
    private bool m_mapped;
    private int m_dataLength;

    public GLBufferTextureStorage(string label, T[] data, SizedInternalFormat format, bool persistentBufferStorage)
    {
        // This can be removed when OpenGL 3.3 support is dropped.
        Assert.Precondition(AssertFormat(format), "OpenGL 3.3 does not support three component formats for TBOs.");

        m_dataLength = data.Length;
        m_bufferTexture = new(label, data, format, persistentBufferStorage);
    }

    private static bool AssertFormat(SizedInternalFormat format)
    {
        var stringFormat = format.ToString("g");
        var isThreeComponent = stringFormat.StartsWith("Rgb", StringComparison.Ordinal) &&
                               !stringFormat.StartsWith("Rgba", StringComparison.Ordinal);
        return !isThreeComponent;
    }

    public int DataLength() => m_dataLength;

    public GLMappedBuffer<T> GetMappedBufferAndBind()
    {
        m_bufferTexture.BindBuffer();
        if (m_bufferTexture.PersistentBufferStorage)
        {
            // OpenGL 4.4 only feature. If set with MapPersistentBit then the mapped buffer can persist forever.
            if (!m_mapped)
            {
                m_mapped = true;
                m_bufferTexture.BindBuffer();
                m_mappedBuffer = m_bufferTexture.MapWithDisposable();
                GLBufferTexture<T>.UnbindBuffer();
            }

            return m_mappedBuffer;
        }

        m_mappedBuffer = m_bufferTexture.MapWithDisposable();
        return m_mappedBuffer;
    }

    public void BindTexture(TextureUnit unit)
    {
        GL.ActiveTexture(unit);
        m_bufferTexture.BindTexture();
        m_bufferTexture.BindTexBuffer();
    }

    public void Unbind()
    {
        if (!m_bufferTexture.PersistentBufferStorage)
            m_mappedBuffer.Dispose();

        GLBufferTexture<T>.UnbindBuffer();
    }

    public void Map(Action<IntPtr> action)
    {
        m_bufferTexture.Map(action);
    }

    public void Dispose()
    {
        if (m_bufferTexture.PersistentBufferStorage)
            m_mappedBuffer.Dispose();
        m_bufferTexture.Dispose();
    }
}
