namespace Helion.Client.Music;

using Helion.Audio;
using Helion.Util;
using NFluidsynth;
using NLog;
using System;
using System.IO;
using static Helion.Util.Assertion.Assert;

public class FluidSynthMusicPlayer : IMusicPlayer
{
    private static readonly NLog.Logger Log = LogManager.GetCurrentClassLogger();
    private string m_lastFile = string.Empty;
    private string? m_soundFontLoaded;
    private bool m_disposed;
    private float m_volume = 1;
    private uint m_soundFontCounter = 0;

    private readonly Settings m_settings;
    private Player? m_player;
    private Synth m_synth;
    private AudioDriver m_audioDriver;

    public FluidSynthMusicPlayer(string soundFontFile)
    {
        m_settings = new Settings();
        m_settings[ConfigurationKeys.SynthAudioChannels].IntValue = 2;
        m_synth = new(m_settings);
        m_audioDriver = new(m_synth.Settings, m_synth);
        EnsureSoundFont(soundFontFile);
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

        try
        {
            Stop();

            if (!string.IsNullOrEmpty(m_lastFile))
                TempFileManager.DeleteFile(m_lastFile);

            m_lastFile = TempFileManager.GetFile();
            File.WriteAllBytes(m_lastFile, data);

            m_player = new(m_synth);

            if (options.HasFlag(MusicPlayerOptions.Loop))
                m_player.SetLoop(-1);

            m_player.Add(m_lastFile);
            SetVolumeInternal();
            m_player.Play();

            return true;
        }
        catch (Exception ex)
        {
            Log.Warn("Error starting FluidSynth music playback.");
            Log.Info(ex.ToString());
        }

        return false;
    }

    public void Stop()
    {
        if (m_disposed)
            return;

        m_player?.Stop();
        m_player?.Join();
        m_synth.SystemReset();

        m_player?.Dispose();
        m_player = null;
    }

    public void EnsureSoundFont(string soundFontPath)
    {
        try
        {
            if (soundFontPath != m_soundFontLoaded)
            {
                if (!string.IsNullOrEmpty(m_soundFontLoaded))
                {
                    m_synth.UnloadSoundFont(m_soundFontCounter, true);
                    m_soundFontLoaded = string.Empty;
                }

                m_synth.LoadSoundFont(soundFontPath, true);
                for (int i = 0; i < 16; i++)
                    m_synth.SoundFontSelect(i, 0);

                m_soundFontCounter++;
                m_soundFontLoaded = soundFontPath;
            }
        }
        catch (Exception ex)
        {
            Log.Warn($"Could not load SoundFont {soundFontPath}.");
            Log.Info(ex.ToString());
        }
    }

    public void OutputChanging()
    {
        // FluidSynth handles this internally.
    }
    public void OutputChanged()
    {
        // FluidSynth handles this internally.
    }

    ~FluidSynthMusicPlayer()
    {
        FailedToDispose(this);
        PerformDispose();
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

        Stop(); // disposes m_player
        try
        {
            m_audioDriver.Dispose();
            m_synth.Dispose();
            m_settings.Dispose();
        }
        catch (Exception ex)
        {
            Log.Warn("Error unloading FluidSynth music player");
            Log.Info(ex.ToString());
        }
        m_disposed = true;
    }
}
