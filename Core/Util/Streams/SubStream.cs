using System;
using System.IO;

namespace Helion.Util.Streams;

public sealed class SubStream : Stream
{
    private readonly Stream m_base;
    private readonly long m_start;
    private readonly long m_end;
    private readonly bool m_disposeBaseStream;

    public SubStream(Stream baseStream, long start, int length, bool disposeBaseStream = false)
    {
        m_base = baseStream;
        m_start = start;
        m_end = start + length;
        m_base.Seek(m_start, SeekOrigin.Begin);
        m_disposeBaseStream = disposeBaseStream;
    }

    protected override void Dispose(bool disposing)
    {
        if (!m_disposeBaseStream)
            return;

        if (disposing)
            m_base.Dispose();
        base.Dispose(disposing);
    }


    public override bool CanRead => m_base.CanRead;
    public override bool CanSeek => true;
    public override bool CanWrite => false;
    public override long Length => m_end - m_start;
    public override long Position
    {
        get => m_base.Position - m_start;
        set => m_base.Position = m_start + value;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        long remaining = m_end - m_base.Position;
        if (remaining <= 0)
            return 0;
        return m_base.Read(buffer, offset, (int)Math.Min(count, remaining));
    }

    public override long Seek(long offset, SeekOrigin origin)
    {
        long target = origin switch
        {
            SeekOrigin.Begin => m_end + offset,
            SeekOrigin.Current => m_base.Position + offset,
            SeekOrigin.End => m_end + offset,
            _ => throw new ArgumentOutOfRangeException(nameof(origin))
        };
        return m_base.Seek(target, SeekOrigin.Begin) - m_end;
    }

    public override void Flush() => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();
    public override void Write(byte[] buffer, int offset, int count) => throw new NotSupportedException();
}
