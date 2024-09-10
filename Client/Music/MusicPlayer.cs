using Helion.Audio;
using Helion.Util.Configs.Components;
using Helion.Util.Extensions;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace Helion.Client.Music;

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
    private ZMusicWrapper.ZMusicPlayer m_musicPlayer;

    public MusicPlayer(ConfigAudio configAudio)
    {
        m_configAudio = configAudio;
        m_playQueueTask = Task.Factory.StartNew(PlayQueueTask, m_cancelPlayQueue.Token,
                TaskCreationOptions.LongRunning, TaskScheduler.Default);
        m_musicPlayer = new ZMusicWrapper.ZMusicPlayer(new AudioStreamFactory(), configAudio.SoundFontFile.Value, (float)(configAudio.MusicVolume.Value * .5));
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

        m_musicPlayer.ChangeSoundFont(m_configAudio.SoundFontFile);
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
        m_musicPlayer.Play(data, playParams.Options.HasFlag(MusicPlayerOptions.Loop));
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

        m_musicPlayer.Dispose();
        m_disposed = true;
    }

    public void SetVolume(float volume)
    {
        m_musicPlayer.Volume = (float)(volume * .5);
    }

    public void Stop()
    {
        if (m_disposed)
            return;

        m_musicPlayer?.Stop();
    }
}
