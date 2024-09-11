namespace Helion.Client.Music;

using Helion.Audio;
using Helion.Util.Configs.Components;
using Helion.Util.Extensions;
using Helion.Util.Sounds.Mus;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

public class MusicPlayer : IMusicPlayer
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private uint m_lastDataHash;
    private bool m_disposed;

    private readonly ConfigAudio m_configAudio;
    private readonly ConcurrentQueue<PlayParams> m_playQueue = [];
    private readonly Dictionary<uint, byte[]> m_convertedMus = [];
    private readonly CancellationTokenSource m_cancelPlayQueue = new();
    private readonly Task m_playQueueTask;
    private Thread? m_playStartThread;
    private ZMusicWrapper.ZMusicPlayer m_zMusicPlayer;
    private FluidSynthMusicPlayer m_fluidSynthPlayer;

    public MusicPlayer(ConfigAudio configAudio)
    {
        m_configAudio = configAudio;
        m_playQueueTask = Task.Factory.StartNew(PlayQueueTask, m_cancelPlayQueue.Token,
                TaskCreationOptions.LongRunning, TaskScheduler.Default);
        m_zMusicPlayer = new ZMusicWrapper.ZMusicPlayer(new AudioStreamFactory(), string.Empty, (float)(configAudio.MusicVolume.Value * .5));
        m_fluidSynthPlayer = new FluidSynthMusicPlayer(configAudio.SoundFontFile.Value);
    }

    private readonly struct PlayParams(byte[] data, MusicPlayerOptions options)
    {
        public readonly byte[] Data = data;
        public readonly MusicPlayerOptions Options = options;
    }

    public bool Play(byte[] data, MusicPlayerOptions options)
    {
        if (m_disposed)
            return false;

        m_playQueue.Clear();
        m_playQueue.Enqueue(new PlayParams(data, options));
        return true;
    }

    public void ChangeSoundFont()
    {
        if (m_disposed)
        {
            return;
        }

        m_fluidSynthPlayer.EnsureSoundFont(m_configAudio.SoundFontFile);
    }

    private void PlayQueueTask()
    {
        while (!m_disposed)
        {
            if (m_playQueue.TryDequeue(out var playParams))
                CreateAndPlayMusic(playParams);

            if (m_cancelPlayQueue.IsCancellationRequested)
                break;

            Thread.Sleep(10);
        }
    }

    private void CreateAndPlayMusic(PlayParams playParams)
    {
        var data = playParams.Data;
        var options = playParams.Options;
        uint hash = data.CalculateCrc32();
        if (options.HasFlag(MusicPlayerOptions.IgnoreAlreadyPlaying))
        {
            if (hash == m_lastDataHash)
                return;
        }

        m_lastDataHash = hash;

        Stop();
        SetVolume((float)m_configAudio.MusicVolume.Value);
        bool isMidi = m_zMusicPlayer.IsMIDI(data, out string? error);

        if (!string.IsNullOrEmpty(error))
        {
            // ZMusic can't make sense of this, so just log it and give up.
            Log.Warn("Unknown/unsupported music format.");
            return;
        }

        if (!isMidi)
        {
            // MP3, OGG, MOD, XM, IT, etc. -- use ZMusic.
            // No need for the "play thread" wrapper here.
            m_zMusicPlayer.Play(data, playParams.Options.HasFlag(MusicPlayerOptions.Loop));
        }
        else
        {
            if (m_convertedMus.TryGetValue(m_lastDataHash, out var converted) || MusToMidi.TryConvert(data, out converted))
            {
                m_convertedMus[m_lastDataHash] = converted;
                data = converted;
            }
            else if (MusToMidi.TryConvertNoHeader(data, out converted))
            {
                m_convertedMus[m_lastDataHash] = converted;
                data = converted;
            }
            else
            {
                // Warn and give up
                Log.Warn("Unknown/unsupported MIDI-like music format");
                return;
            }

            m_playStartThread = new Thread(() => m_fluidSynthPlayer.Play(data, playParams.Options));
            m_playStartThread.Start();
        }
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

        m_cancelPlayQueue.Cancel();
        m_playQueueTask.Wait(1000);

        m_zMusicPlayer.Dispose();
        m_disposed = true;
    }

    public void SetVolume(float volume)
    {
        m_zMusicPlayer.Volume = (float)(volume * .5);
        m_fluidSynthPlayer.SetVolume(volume);
    }

    public void Stop()
    {
        if (m_disposed)
            return;

        m_zMusicPlayer.Stop();
        m_fluidSynthPlayer.Stop();
    }
}
