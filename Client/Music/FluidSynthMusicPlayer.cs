using System;
using System.IO;
using System.Threading;
using Helion.Audio;
using Helion.Util;
using Helion.Util.Configs;
using Helion.Util.Extensions;
using NFluidsynth;
using static Helion.Util.Assertion.Assert;

namespace Helion.Client.Music;

public class FluidSynthMusicPlayer : IMusicPlayer
{
    private readonly string m_soundFontFile;
    private readonly Settings m_settings;
    private readonly IConfig m_config;
    private string m_lastDataHash = string.Empty;
    private string m_lastFile = string.Empty;
    private Player? m_player;
    private Thread? m_thread;
    private bool m_disposed;

    private class PlayParams
    {
        public string File { get; set; } = string.Empty;
        public bool Loop { get; set; }
    }

    public FluidSynthMusicPlayer(IConfig config, string soundFontFile)
    {
        m_config = config;
        m_soundFontFile = soundFontFile;
        m_settings = new Settings();
        m_settings[ConfigurationKeys.SynthAudioChannels].IntValue = 2;

        m_config.Audio.MusicVolume.OnChanged += OnMusicVolumeChange;
    }

    ~FluidSynthMusicPlayer()
    {
        FailedToDispose(this);
        PerformDispose();
    }

    private void OnMusicVolumeChange(object? sender, double newVolume)
    {
        SetVolume((float)newVolume);
    }

    public void SetVolume(float volume)
    {
        var setting = m_settings[ConfigurationKeys.SynthGain];
        setting.DoubleValue = volume * setting.DoubleDefault;
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

        if (!string.IsNullOrEmpty(m_lastFile))
            TempFileManager.DeleteFile(m_lastFile);

        m_lastFile = TempFileManager.GetFile();
        File.WriteAllBytes(m_lastFile, data);
        m_thread = new Thread(new ParameterizedThreadStart(PlayThread));
        m_thread.Start(new PlayParams() { File = m_lastFile, Loop = loop });

        return true;
    }

    private void PlayThread(object? param)
    {
        if (param is not PlayParams playParams)
            return;

        using (Synth synth = new Synth(m_settings))
        {
            synth.LoadSoundFont(m_soundFontFile, true);
            for (int i = 0; i < 16; i++)
                synth.SoundFontSelect(i, 0);

            using (m_player = new Player(synth))
            {
                using (var adriver = new AudioDriver(synth.Settings, synth))
                {
                    if (playParams.Loop)
                        m_player.SetLoop(-1);
                    m_player.Add(playParams.File);
                    m_player.Play();
                    m_player.Join();
                }
            }
        }

        m_player = null;
    }


    public void Stop()
    {
        m_player?.Stop();
        m_thread?.Join();
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

        m_config.Audio.MusicVolume.OnChanged -= OnMusicVolumeChange;

        Stop();
        m_settings.Dispose();
        m_disposed = true;
    }
}
