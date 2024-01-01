using Helion.Audio;
using Helion.Util.Configs;
using Helion.Util.Extensions;
using Helion.Util.Sounds.Mus;
using NLog;
using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace Helion.Client.Music;

public class MusicPlayer : IMusicPlayer
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();
    private readonly IConfig m_config;
    private IMusicPlayer? m_musicPlayer;
    private string m_lastDataHash = string.Empty;
    private float m_volume;
    private bool m_disposed;

    private Thread? m_thread;

    private class PlayParams
    {
        public readonly byte[] Data;
        public readonly MusicPlayerOptions Options;

        public PlayParams(byte[] data, MusicPlayerOptions options)
        {
            Data = data;
            Options = options;
        }
    }

    public MusicPlayer(IConfig config)
    {
        m_config = config;
        m_config.Audio.MusicVolume.OnChanged += OnMusicVolumeChange;
    }

    public bool Play(byte[] data, MusicPlayerOptions options)
    {
        if (m_disposed)
            return false;

        string? hash = null;
        if (options.HasFlag(MusicPlayerOptions.IgnoreAlreadyPlaying))
        {
            hash = data.CalculateCrc32();
            if (hash == m_lastDataHash)
                return true;
        }

        m_lastDataHash = hash ?? data.CalculateCrc32();

        Stop();
        m_musicPlayer?.Dispose();
        m_musicPlayer = null;

        if (MusToMidi.TryConvert(data, out var converted))
        {
            m_musicPlayer = CreateFluidSynthPlayer();
            data = converted;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Ogg/mp3 currently only works in Windows
            if (NAudioMusicPlayer.IsOgg(data))
            {
                m_musicPlayer = new NAudioMusicPlayer(NAudioMusicType.Ogg);
            }
            else if (NAudioMusicPlayer.IsMp3(data))
            {
                m_musicPlayer = new NAudioMusicPlayer(NAudioMusicType.Mp3);
            }

        }
        else if (MusToMidi.TryConvertNoHeader(data, out converted))
        {
            m_musicPlayer = CreateFluidSynthPlayer();
            data = converted;
        }

        if (m_musicPlayer != null)
        {
            m_thread = new Thread(new ParameterizedThreadStart(PlayThread));
            m_thread.Start(new PlayParams(data, options));
            return true;
        }

        Log.Warn("Unknown/unsupported music format");
        return false;
    }

    private static IMusicPlayer CreateFluidSynthPlayer() => 
        new FluidSynthMusicPlayer($"SoundFonts{Path.DirectorySeparatorChar}Default.sf2");

    private void PlayThread(object? param)
    {
        if (m_musicPlayer == null)
            return;

        var playParams = (PlayParams)param!;
        m_musicPlayer.SetVolume(m_volume);
        m_musicPlayer.Play(playParams.Data, playParams.Options);
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
        m_volume = volume;
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
