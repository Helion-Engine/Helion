using System;
using System.Buffers;
using System.IO;

namespace Helion.Util.Streams;

public sealed class PoolBufferedStream(Stream stream, int bufferSize = 81920) : Stream
{
    private readonly Stream m_stream = stream;
    private readonly byte[] m_buffer = ArrayPool<byte>.Shared.Rent(bufferSize);
    private int m_bufferPos;
    private bool m_disposed;

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => true;
    public override long Length => throw new NotSupportedException();
    public override long Position { get => throw new NotSupportedException(); set => throw new NotSupportedException(); }

    public override void Write(byte[] buffer, int offset, int count)
    {
        var remaining = m_buffer.Length - m_bufferPos;
        if (count > remaining)
        {
            FlushBuffer();
            if (count > m_buffer.Length)
            {
                m_stream.Write(buffer, offset, count);
                return;
            }
        }

        Buffer.BlockCopy(buffer, offset, m_buffer, m_bufferPos, count);
        m_bufferPos += count;
    }

    public override void Flush()
    {
        FlushBuffer();
        m_stream.Flush();
    }

    private void FlushBuffer()
    {
        if (m_bufferPos > 0)
        {
            m_stream.Write(m_buffer, 0, m_bufferPos);
            m_bufferPos = 0;
        }
    }

    protected override void Dispose(bool disposing)
    {
        if (!m_disposed)
        {
            if (disposing)
            {
                Flush();
                ArrayPool<byte>.Shared.Return(m_buffer);
                m_stream.Dispose();
            }
            m_disposed = true;
        }
        base.Dispose(disposing);
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        return m_stream.Read(buffer, offset, count);
    }

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
}
