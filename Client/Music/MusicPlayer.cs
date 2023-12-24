using Helion.Audio;
using Helion.Util.Configs;
using Helion.Util.Extensions;
using Helion.Util.Sounds.Mus;
using NLog;
using System;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Threading;

namespace Helion.Client.Music;

public class MusicPlayer : IMusicPlayer
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly IConfig m_config;
    private IMusicPlayer? m_musicPlayer;
    private string m_lastDataHash = string.Empty;
    private bool m_disposed;

    private Thread? m_thread;

    private class PlayParams
    {
        public readonly byte[] Data;
        public readonly bool Loop;

        public PlayParams(byte[] data, bool loop)
        {
            Data = data;
            Loop = loop;
        }
    }

    public MusicPlayer(IConfig config)
    {
        m_config = config;
        m_config.Audio.MusicVolume.OnChanged += OnMusicVolumeChange;
    }

    public bool Play(byte[] data, bool loop = true, bool ignoreAlreadyPlaying = true)
    {
        if (m_disposed)
            return false;

        string? hash = null;
        if (ignoreAlreadyPlaying)
        {
            hash = data.CalculateCrc32();
            if (hash == m_lastDataHash)
                return true;
        }

        m_lastDataHash = hash ?? data.CalculateCrc32();

        Stop();
        m_musicPlayer?.Dispose();
        m_musicPlayer = null;

        if (GetMidiConversion(data, out var converted))
        {
            m_musicPlayer = new FluidSynthMusicPlayer($"SoundFonts{Path.DirectorySeparatorChar}Default.sf2");
            data = converted;
        }
        else if (data.Length > 3 && data[0] == 'O' && data[1] == 'g' && data[2] == 'g')
            m_musicPlayer = new NAudioMusicPlayer(NAudioMusicType.Ogg);
        else if (NAudioMusicPlayer.IsMp3(data))
            m_musicPlayer = new NAudioMusicPlayer(NAudioMusicType.Mp3);            

        if (m_musicPlayer != null)
        {
            m_thread = new Thread(new ParameterizedThreadStart(PlayThread));
            m_thread.Start(new PlayParams(data, loop));
            return true;
        }

        Log.Info("Unknown/unsupported music format");
        return false;
    }

    private static bool GetMidiConversion(byte[] data, [NotNullWhen(true)] out byte[]? converted)
    {
        if (data.Length > 4 && data[0] == 'M' && data[1] == 'T' && data[2] == 'h' && data[3] == 'd')
        {
            converted = data;
            return true;
        }

        converted = MusToMidi.Convert(data);
        return converted != null;
    }

    private void PlayThread(object? param)
    {
        var playParams = (PlayParams)param!;
        m_musicPlayer?.Play(playParams.Data, playParams.Loop, false);
    }

    private void OnMusicVolumeChange(object? sender, double newVolume)
    {
        SetVolume((float)newVolume);
    }

    public void Dispose()
    {
        PerformDispose();
        GC.SuppressFinalize(this);
    }

    protected void PerformDispose()
    {
        if (m_disposed)
            return;

        Stop();
        m_musicPlayer?.Dispose();
        m_config.Audio.MusicVolume.OnChanged -= OnMusicVolumeChange;
        m_disposed = true;
    }

    public void SetVolume(float volume)
    {
        m_musicPlayer?.SetVolume(volume);
    }

    public void Stop()
    {
        if (m_disposed)
            return;
        m_musicPlayer?.Stop();
        m_thread?.Join();
    }
}
