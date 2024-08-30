using Helion.Render.OpenGL.Util;
using OpenTK.Graphics.OpenGL;
using System;

namespace Helion.Render.OpenGL.Textures;

public class GLBufferTextureStorage
{
    private readonly GLBufferTexture m_bufferTexture;
    private GLMappedBuffer<float> m_mappedBuffer;
    private bool m_mapped;

    public GLBufferTextureStorage(string label, float[] data, SizedInternalFormat format, bool persistentBufferStorage)
    {
        m_bufferTexture = new(label, data, format, persistentBufferStorage);
    }

    public GLMappedBuffer<float> GetMappedBufferAndBind()
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
                m_bufferTexture.UnbindBuffer();
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

        m_bufferTexture.UnbindBuffer();
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
