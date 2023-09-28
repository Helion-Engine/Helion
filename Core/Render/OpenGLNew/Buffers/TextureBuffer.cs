using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using Helion.Render.OpenGLNew.Capabilities;
using Helion.Render.OpenGLNew.Util;
using OpenTK.Graphics.OpenGL;
using zdbspSharp;

namespace Helion.Render.OpenGLNew.Buffers;

public class TextureBuffer<T> : IDisposable where T : struct
{
    private static readonly int BytesPerElement = Marshal.SizeOf<T>();

    private readonly SizedInternalFormat m_format;
    private readonly DynamicArray<T> m_data = new();
    private int m_bufferObjectId;
    private int m_textureObjectId;
    private bool m_disposed;

    public bool Empty => Length == 0;
    public int Length => m_data.Length;

    public TextureBuffer(string label, int capacity, SizedInternalFormat format, UsageHint hint)
    {
        ThrowIfNotEnoughTexelSpace(capacity, label);
        
        m_format = format;
        m_bufferObjectId = GL.GenBuffer();
        m_textureObjectId = GL.GenTexture();
        
        m_data.Resize(capacity);

        BindBuffer();
        GL.BufferData(BufferTarget.TextureBuffer, capacity, m_data.Data, hint.ToBufferUsageHint());
        GLUtil.ObjectLabel(ObjectLabelIdentifier.Buffer, m_bufferObjectId, $"[TBO (buffer)] {label}");
        UnbindBuffer();
        
        BindTexture();
        GLUtil.ObjectLabel(ObjectLabelIdentifier.Texture, m_textureObjectId, $"[TBO (texture)] {label}");
        UnbindTexture();
    }

    private static (int PrimitiveSizeBytes, int NumComponents) CalculateSpaceRequirements(SizedInternalFormat format)
    {
        return format switch
        {
            SizedInternalFormat.R8 => (sizeof(byte), 1),
            SizedInternalFormat.R16 => (sizeof(ushort), 1),
            SizedInternalFormat.R16f => (sizeof(short), 1),
            SizedInternalFormat.R32f => (sizeof(float), 1),
            SizedInternalFormat.R8i => (sizeof(byte), 1),
            SizedInternalFormat.R16i => (sizeof(short), 1),
            SizedInternalFormat.R32i => (sizeof(int), 1),
            SizedInternalFormat.R8ui => (sizeof(byte), 1),
            SizedInternalFormat.R16ui => (sizeof(ushort), 1),
            SizedInternalFormat.R32ui => (sizeof(uint), 1),
            SizedInternalFormat.Rg8 => (sizeof(byte), 2),
            SizedInternalFormat.Rg16 => (sizeof(ushort), 2),
            SizedInternalFormat.Rg16f => (sizeof(short), 2),
            SizedInternalFormat.Rg32f => (sizeof(float), 2),
            SizedInternalFormat.Rg8i => (sizeof(byte), 2),
            SizedInternalFormat.Rg16i => (sizeof(short), 2),
            SizedInternalFormat.Rg32i => (sizeof(int), 2),
            SizedInternalFormat.Rg8ui => (sizeof(byte), 2),
            SizedInternalFormat.Rg16ui => (sizeof(ushort), 2),
            SizedInternalFormat.Rg32ui => (sizeof(uint), 2),
            SizedInternalFormat.Rgb32f => (sizeof(float), 3),
            SizedInternalFormat.Rgb32i => (sizeof(int), 3),
            SizedInternalFormat.Rgb32ui => (sizeof(uint), 3),
            SizedInternalFormat.Rgba8 => (sizeof(uint), 4),
            SizedInternalFormat.Rgba16 => (sizeof(short), 4),
            SizedInternalFormat.Rgba16f => (sizeof(short), 4),
            SizedInternalFormat.Rgba32f => (sizeof(float), 4),
            SizedInternalFormat.Rgba8i => (sizeof(byte), 4),
            SizedInternalFormat.Rgba16i => (sizeof(short), 4),
            SizedInternalFormat.Rgba32i => (sizeof(int), 4),
            SizedInternalFormat.Rgba8ui => (sizeof(byte), 4),
            SizedInternalFormat.Rgba16ui => (sizeof(ushort), 4),
            SizedInternalFormat.Rgba32ui => (sizeof(uint), 4),
            _ => throw new($"Unsupported texture buffer sized internal format {format}")
        };
    }

    private void ThrowIfNotEnoughTexelSpace(int capacity, string label)
    {
        int requestedBytes = capacity * BytesPerElement;
        (int primitiveSize, int numComponents) = CalculateSpaceRequirements(m_format);
        int numTexels = requestedBytes / (primitiveSize * numComponents);

        if (numTexels > GLLimits.MaxTextureBufferSize)
            throw new($"GPU limits for texture buffer {label} supports only {GLLimits.MaxTextureBufferSize} texels, which is more than {numTexels} needed texels from {capacity} {typeof(T).Name}s");
    }

    ~TextureBuffer()
    {
        ReleaseUnmanagedResources();
    }

    public void BindBuffer()
    {
        Debug.Assert(!m_disposed, "Trying to bind a disposed BufferTexture's buffer");
        
        GL.BindBuffer(BufferTarget.TextureBuffer, m_bufferObjectId);
    }

    public void UnbindBuffer()
    {
        Debug.Assert(!m_disposed, "Trying to unbind a disposed BufferTexture's buffer");
        
        GL.BindBuffer(BufferTarget.TextureBuffer, GLUtil.NoObject);
    }

    public void BindTexture()
    {
        Debug.Assert(!m_disposed, "Trying to bind a disposed BufferTexture's texture");
        
        GL.BindTexture(TextureTarget.TextureBuffer, m_textureObjectId);
    }

    public void UnbindTexture()
    {
        Debug.Assert(!m_disposed, "Trying to unbind a disposed BufferTexture's texture");
        
        GL.BindTexture(TextureTarget.TextureBuffer, GLUtil.NoObject);
    }

    public void BindBufferToTexture()
    {
        Debug.Assert(!m_disposed, "Trying to unbind a disposed BufferTexture's buffer to a texture");
        
        GL.TexBuffer(TextureBufferTarget.TextureBuffer, m_format, m_bufferObjectId);
    }

    private void ReleaseUnmanagedResources()
    {
        if (m_disposed)
            return;
    
        GL.DeleteBuffer(m_bufferObjectId);
        GL.DeleteTexture(m_textureObjectId);
        m_bufferObjectId = GLUtil.NoObject;
        m_textureObjectId = GLUtil.NoObject;

        m_disposed = true;
    }

    public void Dispose()
    {
        ReleaseUnmanagedResources();
        GC.SuppressFinalize(this);
    }
}