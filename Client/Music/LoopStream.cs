using NAudio.Wave;

namespace Helion.Client.Music;

public class LoopStream : WaveStream
{
    readonly WaveStream m_sourceStream;

    public LoopStream(WaveStream sourceStream)
    {
        m_sourceStream = sourceStream;
    }
    
    public override WaveFormat WaveFormat => m_sourceStream.WaveFormat;
    public override long Length => m_sourceStream.Length;

    public override long Position
    {
        get => m_sourceStream.Position;
        set => m_sourceStream.Position = value;
    }

    public override int Read(byte[] buffer, int offset, int count)
    {
        int totalBytesRead = 0;

        while (totalBytesRead < count)
        {
            int bytesRead = m_sourceStream.Read(buffer, offset + totalBytesRead, count - totalBytesRead);
            if (bytesRead == 0)
            {
                if (m_sourceStream.Position == 0)
                    break;

                m_sourceStream.Position = 0;
            }
            totalBytesRead += bytesRead;
        }

        return totalBytesRead;
    }
}
