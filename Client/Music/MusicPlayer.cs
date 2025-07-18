namespace Helion.Client.Music;

using Helion.Audio;
using Helion.Resources.Archives.Collection;
using Helion.Util;
using Helion.Util.Configs.Components;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;
using ZMusicWrapper;

public class MusicPlayer : IMusicPlayer
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private UInt128 m_lastDataHash;
    private bool m_disposed;

    private readonly PathsManager m_pathsManager;
    private readonly ConfigAudio m_configAudio;
    private readonly ArchiveCollection m_archiveCollection;
    private readonly ConcurrentQueue<PlayParams> m_playQueue = [];
    private readonly Dictionary<UInt128, byte[]> m_convertedMus = [];
    private readonly CancellationTokenSource m_cancelPlayQueue = new();
    private readonly Task m_playQueueTask;
    private readonly AudioStreamFactory m_audioStreamFactory = new();
    private ZMusicPlayer m_zMusicPlayer;
    private readonly MD5 m_md5 = MD5.Create();
    private bool m_genMidiPatchLoaded;
    private PlayParams? m_currentTrack;
    private bool m_isMidi;
    private bool m_enabled = true;
    private const string DefaultSoundFont = "SoundFonts/Default.sf2";

    public MusicPlayer(PathsManager pathsManager, ConfigAudio configAudio, ArchiveCollection archiveCollection)
    {
        m_pathsManager = pathsManager;
        m_configAudio = configAudio;
        m_archiveCollection = archiveCollection;
        m_playQueueTask = Task.Factory.StartNew(PlayQueueTask, m_cancelPlayQueue.Token,
                TaskCreationOptions.LongRunning, TaskScheduler.Default);

        // Hook up event handlers
        m_configAudio.SoundFontFile.OnChanged += SoundFontFile_OnChanged;
        m_configAudio.EnableChorus.OnChanged += EnableChorus_OnChanged;
        m_configAudio.EnableReverb.OnChanged += EnableReverb_OnChanged;
        m_configAudio.Synthesizer.OnChanged += Synthesizer_OnChanged;
        string soundFontPath = GetFullSoundFontPathOrFallback(configAudio.SoundFontFile);

        m_zMusicPlayer = CreateZMusicPlayer(configAudio, m_audioStreamFactory, soundFontPath);
    }

    private ZMusicPlayer CreateZMusicPlayer(ConfigAudio configAudio, AudioStreamFactory streamFactory, string soundFontPath)
    {
        var player = new ZMusicPlayer(
            streamFactory,
            configAudio.Synthesizer == Synth.OPL3 ? MidiDevice.OPL3 : MidiDevice.FluidSynth,
            soundFontPath,
            null,
            (float)configAudio.MusicVolume.Value);
        SetSynthesizer(player);
        return player;
    }

    private string GetFullSoundFontPathOrFallback(string soundFontPath)
    {
        foreach (var folder in m_pathsManager.SoundFontsFolders)
        {
            string fullPath = Path.Combine(folder, soundFontPath);
            if (Path.Exists(fullPath))
                return fullPath;
        }
        // if not found, get the fallback in one of the search folders
        foreach (var folder in m_pathsManager.SoundFontsFolders)
        {
            string fullPath = Path.Combine(folder, DefaultSoundFont);
            if (Path.Exists(fullPath))
                return fullPath;
        }
        return DefaultSoundFont;
    }

    private void Synthesizer_OnChanged(object? sender, Synth e) => SetSynthesizer(m_zMusicPlayer);
    private void EnableReverb_OnChanged(object? sender, bool e) => SetChorusAndReverb();
    private void EnableChorus_OnChanged(object? sender, bool e) => SetChorusAndReverb();
    private void SoundFontFile_OnChanged(object? sender, string e) => ChangeSoundFont();

    public void OutputChanging()
    {
        m_zMusicPlayer.OnDeviceChanging();
    }

    public void OutputChanged()
    {
        m_zMusicPlayer.OnDeviceChanged();
    }

    private readonly struct PlayParams(byte[] data, MusicPlayerOptions options)
    {
        public readonly byte[] Data = data;
        public readonly MusicPlayerOptions Options = options;
    }

    public bool Play(byte[] data, MusicPlayerOptions options)
    {
        if (m_disposed || !m_enabled)
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

        string soundFontPath = GetFullSoundFontPathOrFallback(m_configAudio.SoundFontFile);
        m_zMusicPlayer.ChangeSoundFont(soundFontPath);

        var playing = m_zMusicPlayer.IsPlaying;
        if (playing)
            m_zMusicPlayer.Stop();

        m_zMusicPlayer.Dispose();
        m_zMusicPlayer = CreateZMusicPlayer(m_configAudio, m_audioStreamFactory, soundFontPath);

        if (playing)
            RestartZMusicPlayer();
    }

    private void RestartZMusicPlayer()
    {
        m_zMusicPlayer.Stop();
        if (m_currentTrack.HasValue)
            PlayMusic(m_currentTrack.Value, m_currentTrack.Value.Data);
    }

    public void SetSynthesizer(ZMusicPlayer player)
    {
        if (m_disposed)
        {
            return;
        }

        MidiDevice currentDevice = player.PreferredDevice;
        MidiDevice newDevice = m_configAudio.Synthesizer == Synth.OPL3
            ? MidiDevice.OPL3
            : MidiDevice.FluidSynth;

        if (currentDevice != newDevice)
        {
            player.PreferredDevice = newDevice;
            if (m_currentTrack?.Data != null)
            {
                var newOptions = (m_currentTrack?.Options ?? MusicPlayerOptions.None) & ~MusicPlayerOptions.IgnoreAlreadyPlaying;
                Play(m_currentTrack?.Data!, newOptions);
            }
        }
    }

    public void SetChorusAndReverb()
    {
        var options = FluidMidiOptions.None;
        if (m_configAudio.EnableChorus)
            options |= FluidMidiOptions.Chorus;
        if (m_configAudio.EnableReverb)
            options |= FluidMidiOptions.Reverb;

        m_zMusicPlayer.SetFluidMidiOptions(options);
        if (m_zMusicPlayer.IsPlaying)
            RestartZMusicPlayer();
    }

    private void PlayQueueTask()
    {
        while (!m_disposed)
        {
            if (m_playQueue.TryDequeue(out var playParams))
                try
                {
                    CreateAndPlayMusic(playParams);
                }
                catch (Exception ex)
                {
                    Log.Warn($"Could not start music playback.");
                    Log.Info(ex);
                }

            if (m_cancelPlayQueue.IsCancellationRequested)
                break;

            Thread.Sleep(10);
        }
    }

    private void CreateAndPlayMusic(in PlayParams playParams)
    {
        if (!m_enabled)
            return;

        m_currentTrack = playParams;
        var data = playParams.Data;
        var options = playParams.Options;
        UInt128 hash = BitConverter.ToUInt128(m_md5.ComputeHash(data));

        if ((options & MusicPlayerOptions.IgnoreAlreadyPlaying) != 0)
        {
            if (hash == m_lastDataHash)
                return;
        }

        m_lastDataHash = hash;

        Stop();

        if (m_configAudio.Synthesizer == Synth.OPL3 && !EnsurePatchSetLoaded())
            return;

        m_isMidi = m_zMusicPlayer.IsMIDI(data, out _);
        PlayMusic(playParams, data);
    }

    private void PlayMusic(in PlayParams playParams, byte[] data)
    {
        SetVolume();
        m_zMusicPlayer.Play(data, (playParams.Options & MusicPlayerOptions.Loop) != 0);
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

        m_configAudio.SoundFontFile.OnChanged += SoundFontFile_OnChanged;
        m_configAudio.EnableChorus.OnChanged += EnableChorus_OnChanged;
        m_configAudio.EnableReverb.OnChanged += EnableReverb_OnChanged;
        m_configAudio.Synthesizer.OnChanged += Synthesizer_OnChanged;

        m_cancelPlayQueue.Cancel();
        m_playQueueTask.Wait(1000);

        m_zMusicPlayer.Dispose();
        m_md5.Dispose();
        m_disposed = true;
    }

    public void SetVolume()
    {
        float mod = m_isMidi ? 1f : 0.5f;
        m_zMusicPlayer.Volume = (float)(m_configAudio.ZMusicVolumeNormalized * mod);
    }

    public bool Enabled
    {
        get => m_enabled;
        set
        {
            m_enabled = value;

            if (!value)
            {
                Stop();
            }
        }
    }

    public void Stop()
    {
        if (m_disposed)
            return;

        m_zMusicPlayer.Stop();
    }
}
