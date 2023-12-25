using Helion.Audio;
using NAudio.Wave;
using System;
using System.IO;

namespace Helion.Client.Music;

public enum NAudioMusicType
{
    Mp3,
    Ogg
}

public class NAudioMusicPlayer : IMusicPlayer
{
    private readonly NAudioMusicType m_type;
    private WaveOutEvent? m_waveOut;
    private bool m_stop;
    private bool m_disposed;

    public NAudioMusicPlayer(NAudioMusicType type)
    {
        m_type = type;
    }

    public static bool IsMp3(byte[] data)
    {
        try
        {
            var stream = new MemoryStream(data);
            using var reader = new Mp3FileReader(stream);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public static bool IsOgg(byte[] data) =>
        data.Length > 3 && data[0] == 'O' && data[1] == 'g' && data[2] == 'g';

    public bool Play(byte[] data, MusicPlayerOptions options)
    {
        if (m_disposed)
            return false;

        var stream = new MemoryStream(data);
        WaveStream? audioStream = m_type switch
        {
            NAudioMusicType.Mp3 => new Mp3FileReader(stream),
            _ => new NAudio.Vorbis.VorbisWaveReader(stream),
        };

        var playStream = options.HasFlag(MusicPlayerOptions.Loop) ? new LoopStream(audioStream) : audioStream;
        m_waveOut = new WaveOutEvent();
        m_waveOut.Stop();
        m_waveOut.Init(playStream);

        try
        {
            while (!m_stop)
                m_waveOut.Play();
        }
        catch { }

        audioStream.Dispose();
        return true;
    }

    public void Dispose()
    {
        PerformDispose();
        GC.SuppressFinalize(this);
    }

    public void PerformDispose()
    {
        if (m_disposed)
            return;

        m_waveOut?.Stop();
        m_waveOut?.Dispose();
        m_disposed = true;
    }

    public void SetVolume(float volume)
    {
        if (m_waveOut != null)
            m_waveOut.Volume = volume;
    }

    public void Stop()
    {
        if (m_disposed)
            return;
        m_stop = true;
        m_waveOut?.Stop();
    }
}
