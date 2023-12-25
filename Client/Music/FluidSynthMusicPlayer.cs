using System;
using System.IO;
using Helion.Audio;
using Helion.Util;
using Helion.Util.Extensions;
using NFluidsynth;
using static Helion.Util.Assertion.Assert;

namespace Helion.Client.Music;

public class FluidSynthMusicPlayer : IMusicPlayer
{
    private readonly string m_soundFontFile;
    private readonly Settings m_settings;
    private string m_lastFile = string.Empty;
    private Player? m_player;
    private bool m_disposed;
    private float m_volume = 1;

    private class PlayParams
    {
        public string File { get; set; } = string.Empty;
        public bool Loop { get; set; }
    }

    public FluidSynthMusicPlayer(string soundFontFile)
    {
        m_soundFontFile = soundFontFile;
        m_settings = new Settings();
        m_settings[ConfigurationKeys.SynthAudioChannels].IntValue = 2;
    }

    ~FluidSynthMusicPlayer()
    {
        FailedToDispose(this);
        PerformDispose();
    }

    public void SetVolume(float volume)
    {
        m_volume = volume;
        if (m_player == null || m_disposed)
            return;
        SetVolumeInternal();
    }

    private void SetVolumeInternal()
    {
        var setting = m_settings[ConfigurationKeys.SynthGain];
        setting.DoubleValue = m_volume * setting.DoubleDefault;
    }

    public bool Play(byte[] data, MusicPlayerOptions options)
    {
        if (m_disposed)
            return false;

        Stop();

        if (!string.IsNullOrEmpty(m_lastFile))
            TempFileManager.DeleteFile(m_lastFile);

        m_lastFile = TempFileManager.GetFile();
        File.WriteAllBytes(m_lastFile, data);

        using (Synth synth = new(m_settings))
        {
            synth.LoadSoundFont(m_soundFontFile, true);
            for (int i = 0; i < 16; i++)
                synth.SoundFontSelect(i, 0);

            using (m_player = new Player(synth))
            {
                using var adriver = new AudioDriver(synth.Settings, synth);
                if (options.HasFlag(MusicPlayerOptions.Loop))
                    m_player.SetLoop(-1);

                SetVolumeInternal();

                m_player.Add(m_lastFile);
                m_player.Play();
                m_player.Join();
            }
        }

        m_player = null;
        return true;
    }

    public void Stop()
    {
        if (m_player == null || m_disposed)
            return;

        m_player.Stop();
        m_player = null;
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
        m_settings.Dispose();
        m_disposed = true;
    }
}
