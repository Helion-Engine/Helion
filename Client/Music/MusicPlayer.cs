namespace Helion.Client.Music;

using Helion.Audio;
using Helion.Resources.Archives.Collection;
using Helion.Resources.Archives.Entries;
using Helion.Util;
using Helion.Util.Configs.Components;
using NLog;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using ZMusicWrapper;

public class MusicPlayer : IMusicPlayer
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private string m_lastEntryPath = string.Empty;
    private bool m_disposed;

    private readonly PathsManager m_pathsManager;
    private readonly ConfigAudio m_configAudio;
    private readonly ArchiveCollection m_archiveCollection;
    private readonly ConcurrentQueue<PlayParams> m_playQueue = [];
    private readonly CancellationTokenSource m_cancelPlayQueue = new();
    private readonly Task m_playQueueTask;
    private readonly AudioStreamFactory m_audioStreamFactory = new();
    private readonly ZMusicPlayer m_zMusicPlayer;
    private readonly Dictionary<string, byte[]> m_musicLookup = [];
    private bool m_genMidiPatchLoaded;
    private PlayParams? m_currentTrack;
    private bool m_isMidi;
    private bool m_soundFontChanged;
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
        FluidMidiOptions midiOptions = FluidMidiOptions.None;
        if (configAudio.EnableChorus)
            midiOptions |= FluidMidiOptions.Chorus;
        if (configAudio.EnableReverb)
            midiOptions |= FluidMidiOptions.Reverb;

        var player = new ZMusicPlayer(
            streamFactory,
            configAudio.Synthesizer == Synth.OPL3 ? MidiDevice.OPL3 : MidiDevice.FluidSynth,
            soundFontPath,
            null,
            (float)configAudio.MusicVolume.Value,
            fluidMidiOptions: midiOptions);
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

    private readonly struct PlayParams(Entry entry, MusicPlayerOptions options)
    {
        public readonly Entry Entry = entry;
        public readonly MusicPlayerOptions Options = options;
    }

    public bool Play(Entry entry, MusicPlayerOptions options)
    {
        if (m_disposed || !m_enabled)
            return false;

        m_playQueue.Clear();
        m_playQueue.Enqueue(new PlayParams(entry, options));
        return true;
    }
    public void ChangeSoundFont()
    {
        if (m_disposed)
            return;

        // This requires a reset of ZMusic so only trigger the change when playing a midi through fluidsynth.
        if (!m_isMidi || m_configAudio.Synthesizer.Value != Synth.FluidSynth)
        {
            m_soundFontChanged = true;
            return;
        }

        var isPlaying = m_zMusicPlayer.IsPlaying;
        var soundFontPath = GetFullSoundFontPathOrFallback(m_configAudio.SoundFontFile);
        m_zMusicPlayer.ChangeSoundFont(soundFontPath);

        if (isPlaying && m_currentTrack.HasValue)
        {
            var track = m_currentTrack.Value;
            m_playQueue.Enqueue(new(track.Entry, track.Options | MusicPlayerOptions.Reload));
        }
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
            if (m_currentTrack?.Entry != null)
            {
                var newOptions = (m_currentTrack?.Options ?? MusicPlayerOptions.None) | MusicPlayerOptions.Reload;
                Play(m_currentTrack!.Value.Entry, newOptions);
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
    }

    public void ClearCachedData()
    {
        m_musicLookup.Clear();
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

        m_currentTrack = new(playParams.Entry, playParams.Options & ~MusicPlayerOptions.Reload);

        var options = playParams.Options;
        var fullPath = playParams.Entry.Path.FullPath;
        if ((options & MusicPlayerOptions.IgnoreAlreadyPlaying) != 0 && (options & MusicPlayerOptions.Reload) == 0 && fullPath == m_lastEntryPath)
            return;

        if (!m_musicLookup.TryGetValue(fullPath, out var data))
        {
            data = playParams.Entry.ReadData();
            m_musicLookup[fullPath] = data;
        }

        m_lastEntryPath = fullPath;

        Stop();

        if (m_configAudio.Synthesizer == Synth.OPL3 && !EnsurePatchSetLoaded())
            return;

        m_isMidi = m_zMusicPlayer.IsMIDI(data, out _);
        PlayMusic(playParams, data);
    }

    private void PlayMusic(in PlayParams playParams, byte[] data)
    {
        if (m_soundFontChanged)
        {
            m_soundFontChanged = false;
            ChangeSoundFont();
        }

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
        m_disposed = true;
    }

    public void SetVolume()
    {
        m_zMusicPlayer.Volume = CalcVolume();
    }

    private float CalcVolume()
    {
        if (m_isMidi && m_configAudio.Synthesizer.Value == Synth.FluidSynth)
            return (float)m_configAudio.FluidSynthVolumeNormalized;
        return (float)m_configAudio.DefaultMusicVolumeNormalized * 0.5f;
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
