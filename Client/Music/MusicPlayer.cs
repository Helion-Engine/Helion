namespace Helion.Client.Music;

using Helion.Audio;
using Helion.Resources.Archives.Collection;
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
    private readonly ArchiveCollection m_archiveCollection;
    private readonly ConcurrentQueue<PlayParams> m_playQueue = [];
    private readonly Dictionary<uint, byte[]> m_convertedMus = [];
    private readonly CancellationTokenSource m_cancelPlayQueue = new();
    private readonly Task m_playQueueTask;
    private Thread? m_playStartThread;
    private ZMusicWrapper.ZMusicPlayer m_zMusicPlayer;
    private FluidSynthMusicPlayer m_fluidSynthPlayer;
    private bool m_genMidiPatchLoaded;
    private PlayParams? m_currentTrack;

    public MusicPlayer(ConfigAudio configAudio, ArchiveCollection archiveCollection)
    {
        m_configAudio = configAudio;
        m_archiveCollection = archiveCollection;
        m_playQueueTask = Task.Factory.StartNew(PlayQueueTask, m_cancelPlayQueue.Token,
                TaskCreationOptions.LongRunning, TaskScheduler.Default);

        AudioStreamFactory streamFactory = new AudioStreamFactory();

        m_zMusicPlayer = new ZMusicWrapper.ZMusicPlayer(
            streamFactory,
            configAudio.Synthesizer == Synth.OPL3 ? ZMusicWrapper.MidiDevice.OPL3 : ZMusicWrapper.MidiDevice.FluidSynth,
            configAudio.SoundFontFile,
            null,
            (float)(configAudio.MusicVolume.Value * .5));
        m_fluidSynthPlayer = new FluidSynthMusicPlayer(
            configAudio.SoundFontFile.Value,
            streamFactory,
            (float)m_configAudio.MusicVolume);
        SetSynthesizer();
    }

    public void OutputChanging()
    {
        m_zMusicPlayer.OnDeviceChanging();
        m_fluidSynthPlayer.OutputChanging();
    }

    public void OutputChanged()
    {
        m_zMusicPlayer.OnDeviceChanged();
        m_fluidSynthPlayer.OutputChanged();
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

    public void SetSynthesizer()
    {
        if (m_disposed)
        {
            return;
        }

        ZMusicWrapper.MidiDevice currentDevice = m_zMusicPlayer.PreferredDevice;
        ZMusicWrapper.MidiDevice newDevice = m_configAudio.Synthesizer == Synth.OPL3
            ? ZMusicWrapper.MidiDevice.OPL3
            : ZMusicWrapper.MidiDevice.FluidSynth;

        if (currentDevice != newDevice)
        {
            m_zMusicPlayer.PreferredDevice = newDevice;
            if (m_currentTrack?.Data != null)
            {
                MusicPlayerOptions newOptions = (m_currentTrack?.Options ?? MusicPlayerOptions.None) & ~MusicPlayerOptions.IgnoreAlreadyPlaying;
                this.Play(m_currentTrack?.Data!, newOptions);
            }
        }
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
        m_currentTrack = playParams;
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
        SetVolume((float)m_configAudio.MusicVolumeNormalized);
        bool isMidi = m_zMusicPlayer.IsMIDI(data, out string? error);

        if (!string.IsNullOrEmpty(error))
        {
            // ZMusic can't make sense of this, so just log it and give up.
            Log.Warn("Unknown/unsupported music format.");
            return;
        }

        if (!isMidi || (m_configAudio.Synthesizer == Synth.OPL3 && EnsurePatchSetLoaded()))
        {
            // MP3, OGG, MOD, XM, IT, etc. -- use ZMusic.
            // No need for the "play thread" wrapper here because the player spins up its own thread/task internally
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

    private bool EnsurePatchSetLoaded()
    {
        if (m_genMidiPatchLoaded)
        {
            return true;
        }

        byte[]? patchSet = m_archiveCollection.Entries.FindByName("GENMIDI")?.ReadData() ?? null;
        if (patchSet != null)
        {
            // The original OPL patch set distributed by ID software has an 8-byte header.
            // Other patch sets may have longer or shorter headers: https://doomwiki.org/wiki/GENMIDI
            // "The header is followed by 175 36-byte records of instrument data." ...
            // "Following the instrument data is 175 32-byte ASCII fields containing the names of the standard General MIDI instruments."
            const int patchSetSize = (175 * 36) + (175 * 32);
            int patchStart = patchSet.Length - patchSetSize;

            if (patchStart < 0)
            {
                Log.Warn("Invalid OPL patch set.");
                return false;
            }

            m_zMusicPlayer.SetOPLPatchSet(patchSet[patchStart..]);
            m_genMidiPatchLoaded = true;
        }

        return m_genMidiPatchLoaded;
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
