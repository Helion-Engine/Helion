using Helion.Audio;
using Helion.Util;
using NFluidsynth;
using NLog;
using System;
using System.IO;
using static Helion.Util.Assertion.Assert;

namespace Helion.Client.Music;

public class FluidSynthMusicPlayer : IMusicPlayer
{
    private static readonly NLog.Logger Log = LogManager.GetCurrentClassLogger();
    private readonly Settings m_settings;
    private string m_lastFile = string.Empty;
    private Player? m_player;
    private Synth m_synth;
    private bool m_soundFontLoaded;
    private bool m_disposed;
    private float m_volume = 1;
    private uint m_soundFontCounter = 0;

    public FluidSynthMusicPlayer(FileInfo soundFontFile)
    {
        m_settings = new Settings();
        m_settings[ConfigurationKeys.SynthAudioChannels].IntValue = 2;
        m_synth = new(m_settings);
        ChangeSoundFont(soundFontFile);
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

        try
        {
            Stop();

            if (!string.IsNullOrEmpty(m_lastFile))
                TempFileManager.DeleteFile(m_lastFile);

            m_lastFile = TempFileManager.GetFile();
            File.WriteAllBytes(m_lastFile, data);

            using (m_player = new Player(m_synth))
            {
                using var adriver = new AudioDriver(m_synth.Settings, m_synth);
                if (options.HasFlag(MusicPlayerOptions.Loop))
                    m_player.SetLoop(-1);

                SetVolumeInternal();

                m_player.Add(m_lastFile);
                m_player.Play();
                m_player.Join();
            }

            m_player = null;
            return true;
        }
        catch (Exception ex)
        {
            Log.Warn("Error starting FluidSynth music playback.");
            Log.Info(ex.ToString());
        }

        return false;
    }

    public void ChangeSoundFont(FileInfo soundFontPath)
    {
        if (m_soundFontLoaded)
        {
            m_synth.UnloadSoundFont(m_soundFontCounter, true);
            m_soundFontLoaded = false;
        }

        try
        {
            m_synth.LoadSoundFont(soundFontPath.FullName, true);
            for (int i = 0; i < 16; i++)
                m_synth.SoundFontSelect(i, 0);

            m_soundFontCounter++;
            m_soundFontLoaded = true;
        }
        catch (Exception ex)
        {
            Log.Warn($"Could not load SoundFont {soundFontPath}.");
            Log.Info(ex.ToString());
        }
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
        try
        {
            m_settings.Dispose();
            m_synth.Dispose();
        }
        catch (Exception ex)
        {
            Log.Warn("Error unloading FluidSynth music player");
            Log.Info(ex.ToString());
        }
        m_disposed = true;
    }
}
