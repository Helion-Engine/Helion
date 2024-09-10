﻿using Helion.Audio;
using Helion.Util.Configs.Components;
using Helion.Util.Extensions;
using Helion.Util.Sounds.Mus;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace Helion.Client.Music;

public class MusicPlayer : IMusicPlayer
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private uint m_lastDataHash;
    private float m_volume;
    private bool m_disposed;

    private readonly ConfigAudio m_configAudio;
    private readonly ConcurrentQueue<PlayParams> m_playQueue = [];
    private readonly Dictionary<uint, byte[]> m_convertedMus = [];
    private readonly CancellationTokenSource m_cancelPlayQueue = new();
    private readonly Task m_playQueueTask;
    private Thread? m_playThread;
    private IMusicPlayer? m_musicPlayer;
    private PlayParams m_playParams = default;

    public MusicPlayer(ConfigAudio configAudio)
    {
        m_configAudio = configAudio;
        m_playQueueTask = Task.Factory.StartNew(PlayQueueTask, m_cancelPlayQueue.Token,
                TaskCreationOptions.LongRunning, TaskScheduler.Default);
        m_musicPlayer = new ZMusicPlayer();
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

        throw new NotImplementedException();
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


        //if (m_convertedMus.TryGetValue(m_lastDataHash, out var converted) || MusToMidi.TryConvert(data, out converted))
        //{
        //    m_convertedMus[m_lastDataHash] = converted;
        //    data = converted;

        //    // do we still need MUStoMIDI?
        //    // 
        //}
        //else if (IsMod(data))
        //{
        //    Log.Error("MOD music format not supported");
        //}
        //else if (NAudioMusicPlayer.IsMp3(data))
        //{
        //    m_musicPlayer = GetNaudioPlayer(NAudioMusicType.Mp3);
        //}
        //else if (NAudioMusicPlayer.IsOgg(data))
        //{
        //    m_musicPlayer = GetNaudioPlayer(NAudioMusicType.Ogg);
        //}
        //else if (MusToMidi.TryConvertNoHeader(data, out converted))
        //{
        //    m_convertedMus[m_lastDataHash] = converted;
        //    m_musicPlayer = m_fluidSynthPlayer;
        //    data = converted;
        //}
        //else
        //{
        //    Log.Warn("Unknown/unsupported music format");
        //}

        // trivial return so we can launch Helion without crashing for now
        return;


        //m_playParams = new(data, playParams.Options);
        //m_playThread = new Thread(PlayThread);
        //m_playThread.Start();
    }

    private void PlayThread()
    {
        if (m_musicPlayer == null)
            return;

        m_musicPlayer.SetVolume(m_volume);
        m_musicPlayer.Play(m_playParams.Data, m_playParams.Options);
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

        StopImpl(true);

        m_cancelPlayQueue.Cancel();
        m_playQueueTask.Wait(1000);

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
        StopImpl(false);
    }

    private void StopImpl(bool disposing = false)
    {
        if (m_disposed)
            return;

        m_musicPlayer?.Stop();

        if (disposing && m_playThread?.Join(1000) == true)
            m_musicPlayer?.Dispose();
        else
            Log.Error("Music player failed to terminate.");
    }
}
