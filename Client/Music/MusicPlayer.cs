using Helion.Audio;
using Helion.Util.Configs.Components;
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

    private string m_lastDataHash = string.Empty;
    private float m_volume;
    private bool m_disposed;

    private ConfigAudio m_configAudio;
    private Thread? m_thread;
    private IMusicPlayer? m_musicPlayer;
    private MusicType? m_lastMusicType;

    private enum MusicType
    {
        MIDI,
        MP3,
        OGG,
        NONE
    };

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

    public MusicPlayer(ConfigAudio configAudio)
    {
        m_configAudio = configAudio;
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

        if (MusToMidi.TryConvert(data, out var converted))
        {
            SelectMusicPlayer(MusicType.MIDI);
            data = converted;
        }
        else if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
        {
            // Ogg/mp3 currently only works in Windows
            if (NAudioMusicPlayer.IsOgg(data))
            {
                SelectMusicPlayer(MusicType.OGG);
            }
            else if (NAudioMusicPlayer.IsMp3(data))
            {
                SelectMusicPlayer(MusicType.MP3);
            }

        }
        else if (MusToMidi.TryConvertNoHeader(data, out converted))
        {
            SelectMusicPlayer(MusicType.MIDI);
            data = converted;
        }
        else
        {
            SelectMusicPlayer(MusicType.NONE);
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

    public bool ChangesMasterVolume() => m_musicPlayer is NAudioMusicPlayer;

    private void SelectMusicPlayer(MusicType musicType)
    {
        if (musicType == MusicType.MIDI && m_musicPlayer is FluidSynthMusicPlayer)
        {
            // We already have a FluidSynth player; avoid reloading it, so it can cache SoundFonts.
            return;
        }

        m_musicPlayer?.Dispose();
        switch(musicType)
        {
            case MusicType.MIDI:
                m_musicPlayer = new FluidSynthMusicPlayer(Path.Combine(m_configAudio.SoundFontFolder, m_configAudio.SoundFontFile));
                break;
            case MusicType.OGG:
                m_musicPlayer = new NAudioMusicPlayer(NAudioMusicType.Ogg);
                break;
            case MusicType.MP3:
                m_musicPlayer = new NAudioMusicPlayer(NAudioMusicType.Mp3);
                break;
            default:
                break;
        }
    }

    public void ChangeSoundFont()
    {
        (m_musicPlayer as FluidSynthMusicPlayer)?.ChangeSoundFont(Path.Combine(m_configAudio.SoundFontFolder, m_configAudio.SoundFontFile));
    }

    private void PlayThread(object? param)
    {
        if (m_musicPlayer == null)
            return;

        var playParams = (PlayParams)param!;
        m_musicPlayer.SetVolume(m_volume);
        m_musicPlayer.Play(playParams.Data, playParams.Options);
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

        if (m_thread == null)
            return;

        if (!m_thread.Join(1000))
            Log.Error($"Music player failed to terminate.");
    }
}
