namespace Helion.Client.Music;

using Helion.Audio;
using Helion.Util;
using NFluidsynth;
using NLog;
using System;
using System.IO;
using ZMusicWrapper;
using static Helion.Util.Assertion.Assert;

public class FluidSynthMusicPlayer : IMusicPlayer
{
    private const int Channels = 2;
    private const int SampleRate = 44100;

    private static readonly NLog.Logger Log = LogManager.GetCurrentClassLogger();
    private string m_lastFile = string.Empty;
    private string? m_soundFontLoaded;
    private bool m_disposed;
    private float m_volume = 1;
    private uint m_soundFontCounter = 0;

    private readonly IOutputStreamFactory m_streamFactory;
    private readonly Settings m_settings;
    private Player? m_player;
    private Synth m_synth;
    private IOutputStream? m_stream;
    private Action<short[]>? m_fillBlockAction;

    public FluidSynthMusicPlayer(string soundFontFile, IOutputStreamFactory streamFactory, float sourceVolume)
    {
        m_settings = new Settings();
        m_synth = new(m_settings);
        m_streamFactory = streamFactory;
        EnsureSoundFont(soundFontFile);
        m_volume = sourceVolume;
    }

    public void SetVolume(float newVolume)
    {
        if (m_disposed)
            return;

        m_volume = newVolume;
        m_stream?.SetVolume(newVolume);
    }

    public unsafe bool Play(byte[] data, MusicPlayerOptions options)
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
            m_player.Play();

            m_stream = m_streamFactory.GetOutputStream(SampleRate, Channels);
            m_stream.SetVolume(m_volume);

            float[] sampleBuffer = new float[m_stream.BlockLength * 2];
            m_fillBlockAction = block =>
            {
                if (m_player.Status == FluidPlayerStatus.Playing)
                {
                    m_synth.WriteSampleFloat(m_stream.BlockLength, sampleBuffer, 0, 2, sampleBuffer, 1, 2);
                    for (int i = 0; i < block.Length; i++)
                    {
                        short sample = (short)Math.Clamp((int)(32768 * sampleBuffer[i]), short.MinValue, short.MaxValue);
                        block[i] = sample;
                    }
                }
                else
                {
                    m_stream.Stop();
                }
            };

            m_stream.Play(m_fillBlockAction);

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

        m_stream?.Stop();
        m_stream?.Dispose();
        m_stream = null;

        m_fillBlockAction = null;

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
            // Pause
            OutputChanging();

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

            // Resume
            OutputChanged();
        }
        catch (Exception ex)
        {
            Log.Warn($"Could not load SoundFont {soundFontPath}.");
            Log.Info(ex.ToString());
        }
    }

    public void OutputChanging()
    {
        if (m_disposed)
            return;

        m_stream?.Stop();
        m_stream?.Dispose();
    }
    public void OutputChanged()
    {
        if (m_disposed)
            return;

        if (m_fillBlockAction != null)
        {
            m_stream = m_streamFactory.GetOutputStream(SampleRate, Channels);
            m_stream.SetVolume(m_volume);
            m_stream.Play(m_fillBlockAction);
        }
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
