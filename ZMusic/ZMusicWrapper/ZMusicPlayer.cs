namespace ZMusicWrapper;

using global::ZMusicWrapper.Generated;
using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;

/// <summary>
/// Provides basic playback functionality for formats that ZMusic supports
/// </summary>
public class ZMusicPlayer : IDisposable
{
    private const int DefaultSampleRate = 44_100;
    private const int DefaultSampleRate_OPL = 49_716; // source: https://en.wikipedia.org/wiki/Yamaha_OPL
    private const int DefaultChannels = 2;

    private EMidiDevice_ m_midiDevice;
    private IOutputStreamFactory m_streamFactory;
    private bool m_patchesLoaded;
    private bool m_soundFontLoaded;
    private float m_sourceVolume;
    private string m_soundFontPath;

    private Func<short[], bool>? m_fillBlockAction;
    private Action? m_stoppedAction;
    private IOutputStream? m_activeStream;
    private IntPtr m_zMusicSong;
    private Task? m_playStartTask;
    private bool m_loop;
    private float[]? m_floatSampleData;
    private int m_channels;
    private int m_samplerate;

    private bool m_disposed;

    public bool IsPlaying => IsPlayingImpl();

    /// <summary>
    /// Gets or sets the volume.  Note that this is done on the output stream, not internal to ZMusic.
    /// </summary>
    public float Volume
    {
        get => m_sourceVolume;
        set
        {
            m_sourceVolume = value;
            m_activeStream?.SetVolume(value);
        }
    }

    public MidiDevice PreferredDevice
    {
        get
        {
            return m_midiDevice == EMidiDevice_.MDEV_OPL
                ? MidiDevice.OPL3
                : MidiDevice.FluidSynth;
        }
        set
        {
            m_midiDevice = value == MidiDevice.OPL3
                ? EMidiDevice_.MDEV_OPL
                : EMidiDevice_.MDEV_FLUIDSYNTH;
        }
    }


    /// <summary>
    /// Stop playback on the current output stream and discard it.
    /// If music was playing at the time, this is resumable.
    /// </summary>
    public void OnDeviceChanging()
    {
        if (m_activeStream != null)
        {
            m_activeStream.Stop();
            m_activeStream.Dispose();
        }
    }

    /// <summary>
    /// Resume playback on a new output stream
    /// </summary>
    public void OnDeviceChanged()
    {
        if (IsPlayingImpl() && m_fillBlockAction != null)
        {
            m_activeStream = m_streamFactory.GetOutputStream(m_samplerate, m_channels);
            m_activeStream.SetVolume(m_sourceVolume);
            m_activeStream.Play(m_fillBlockAction);
        }
    }

    /// <summary>
    /// Gets the last error encountered when starting or stopping playback.
    /// This will be null if we have not encountered any errors.
    /// </summary>
    public Exception? LastErr { get; private set; }

    /// <summary>
    /// Creates a new music player
    /// </summary>
    /// <param name="outputStreamFactory">Output stream factory to use for playback</param>
    /// <param name="preferredDevice">Preferred device for playing MIDI</param>
    /// <param name="soundFontPath">Path to a SoundFont (.sf2, .sf3) file for use when playing MIDI</param>
    /// <param name="oplPatchSet">Patches to use for emulated OPL playback</param>
    /// <param name="sourceVolume">Initial volume for this source</param>
    public ZMusicPlayer(IOutputStreamFactory outputStreamFactory, MidiDevice preferredDevice, string soundFontPath, byte[]? oplPatchSet, float sourceVolume = 1.0f)
    {
        m_streamFactory = outputStreamFactory;
        m_soundFontPath = soundFontPath;
        m_sourceVolume = sourceVolume;

        if (preferredDevice == MidiDevice.OPL3 && oplPatchSet != null)
        {
            SetOPLPatchSet(oplPatchSet);
            m_midiDevice = EMidiDevice_.MDEV_OPL;
        }
        else
        {
            m_midiDevice = EMidiDevice_.MDEV_FLUIDSYNTH;
        }
    }

    /// <summary>
    /// Sets the patch set data to use when OPL emulation is selected.
    /// The caller is responsible for stripping any headers specific to the file/lump format.
    /// </summary>
    /// <param name="patchData">OPL patch data, typically from the GENMIDI lump in an IWAD, with headers stripped.</param>
    public unsafe void SetOPLPatchSet(byte[] patchData)
    {
        fixed (byte* genMidiBytes = patchData)
        {
            ZMusic.ZMusic_SetGenMidi(genMidiBytes);
            m_patchesLoaded = true;
        }
    }

    /// <summary>
    /// Determine whether a given byte array looks like MIDI or MUS data
    /// </summary>
    /// <param name="soundFileData">byte array representing the raw data of a music file</param>
    /// <returns>True if the input data appears to be MIDI or MUS, False otherwise</returns>
    public unsafe bool IsMIDI(byte[] soundFileData, out string? err)
    {
        err = null;

        try
        {
            fixed (byte* dataBytes = soundFileData)
            {
                _ZMusic_MusicStream_Struct* song = null;

                nuint length = (nuint)soundFileData.Length;
                song = ZMusic.ZMusic_OpenSongMem(dataBytes, length, EMidiDevice_.MDEV_FLUIDSYNTH, null);
                m_zMusicSong = (IntPtr)song;

                bool result = ZMusic.ZMusic_IsMIDI(song) != 0;

                ZMusic.ZMusic_Close(song);
                m_zMusicSong = IntPtr.Zero;
                return result;
            }
        }
        catch (Exception ex)
        {
            // Whatever just happened, let's assume it wasn't MIDI
            err = ex.ToString();
            return false;
        }
    }

    /// <summary>
    /// Begin playback, on another thread, of the specified buffer
    /// </summary>
    /// <param name="soundFileData">A byte array containing the raw data from a music file in a supported format</param>
    /// <param name="loop">If true, loop playback at end of track</param>
    /// <param name="stopped">Optional: A method that will be called when the underlying ZMusic playback has stoppe.</param>
    public unsafe void Play(byte[] soundFileData, bool loop, Action? stopped = null)
    {
        ObjectDisposedException.ThrowIf(m_disposed, this);
        Stop();

        m_stoppedAction = stopped;
        m_playStartTask = new Task(() => PlayImpl(soundFileData, loop));
        m_playStartTask.Start();
    }

    private unsafe void PlayImpl(byte[] soundFileData, bool loop)
    {
        _ZMusic_MusicStream_Struct* song = null;

        try
        {
            SoundStreamInfo_ info;

            fixed (byte* dataBytes = soundFileData)
            {
                nuint length = (nuint)soundFileData.Length;
                song = ZMusic.ZMusic_OpenSongMem(dataBytes, length, m_midiDevice, null);
                m_zMusicSong = (IntPtr)song;
            }

            ZMusic.ZMusic_GetStreamInfo(song, &info);

            if (ZMusic.ZMusic_IsMIDI(song) == 0)
            {
                PlayStream(info.mSampleRate, info.mNumChannels, loop);
            }
            else
            {
                if (m_midiDevice == EMidiDevice_.MDEV_OPL)
                {
                    if (!m_patchesLoaded)
                    {
                        throw new Exception("Cannot use OPL synth without patches loaded.");
                    }

                    ZMusic.ChangeMusicSettingInt(EIntConfigKey_.zmusic_opl_numchips, song, 8, null);
                    // OPL cores:
                    // 0 YM3812
                    // 1 DBOPL
                    // 2 JavaOPL
                    // 3 NukedOPL3
                    ZMusic.ChangeMusicSettingInt(EIntConfigKey_.zmusic_opl_core, song, 3, null);
                    PlayStream(DefaultSampleRate_OPL, 2, loop);
                }
                else
                {
                    if (!m_soundFontLoaded)
                        SetSoundFont(song, m_soundFontPath);

                    PlayStream(DefaultSampleRate, DefaultChannels, loop);
                }
            }
        }
        catch (Exception e)
        {
            LastErr = e;

            if (song != null)
            {
                // We've failed at some point in initialization; bail out and clean up
                Stop();
            }
        }
    }

    public unsafe void ChangeSoundFont(string newPath)
    {
        if (m_soundFontPath == newPath)
            return;

        m_soundFontPath = newPath;
        m_soundFontLoaded = false;

        if (IsPlaying)
        {
            _ZMusic_MusicStream_Struct* song = (_ZMusic_MusicStream_Struct*)m_zMusicSong;

            SetSoundFont(song, m_soundFontPath);
            m_playStartTask = new(() =>
            {
                ZMusic.ZMusic_Stop(song);
                ZMusic.ZMusic_Start(song, 0, Convert.ToByte(m_loop));
            });
            m_playStartTask.Start();
        }
    }

    private unsafe void SetSoundFont(_ZMusic_MusicStream_Struct* song, string soundFontPath)
    {
        if (!File.Exists(soundFontPath))
        {
            throw new FileNotFoundException("Invalid SoundFont file path");
        }

        byte[] fluidSynthPathBytes = Encoding.UTF8.GetBytes(soundFontPath);
        fixed (byte* path = fluidSynthPathBytes)
        {
            ZMusic.ChangeMusicSettingInt(EIntConfigKey_.zmusic_fluid_samplerate, song, DefaultSampleRate, null);
            ZMusic.ChangeMusicSetting(EStringConfigKey_.zmusic_fluid_patchset, song, (sbyte*)path);
            m_soundFontLoaded = true;
        }
    }

    private unsafe void PlayStream(int sampleRate, int channels, bool loop)
    {
        if (channels == 0 || Math.Abs(channels) > 2)
            throw new Exception("Unsupported audio format");

        m_samplerate = sampleRate;
        m_channels = Math.Abs(channels);

        // Only get a new stream if the sample rate or channel count differs from the previous track.
        if (m_activeStream?.SampleRate != m_samplerate || m_activeStream?.ChannelCount != m_channels)
        {
            m_activeStream?.Dispose();
            m_activeStream = m_streamFactory.GetOutputStream(m_samplerate, m_channels);
            m_activeStream.SetVolume(m_sourceVolume);
            m_floatSampleData = new float[m_activeStream.ChannelCount * m_activeStream.BlockLength];
        }

        m_loop = loop;
        _ = ZMusic.ZMusic_Start((_ZMusic_MusicStream_Struct*)m_zMusicSong, 0, Convert.ToByte(loop));

        // The samples returned from ZMusic will be in one of two formats, depending on the _sign_ of the channel count
        // it reported when opening the data.  In all cases, the _output_ must be a signed short.

        if (channels > 0)
        {
            // Positive:  The samples are 32-bit floats in the range [-1..1] and must be converted by multiplying by 32768.
            m_fillBlockAction = FillStreamFromFloatSource;
        }
        else if (channels < 0)
        {
            // Negative:  The samples are shorts and can be copied directly to an output stream.
            m_fillBlockAction = FillStreamBufferDirectly;
        }

        if (m_fillBlockAction != null)
        {
            m_activeStream.Play(m_fillBlockAction);
        }
    }

    private unsafe bool FillStreamFromFloatSource(short[] buffer)
    {
        if (IsPlayingImpl())
        {
            fixed (float* p = m_floatSampleData ?? throw new NullReferenceException())
            {
                _ = ZMusic.ZMusic_FillStream((_ZMusic_MusicStream_Struct*)m_zMusicSong, p, sizeof(float) * m_floatSampleData.Length);
            }
            for (int i = 0; i < buffer.Length; i++)
            {
                short sample = (short)Math.Clamp((int)(32768 * m_floatSampleData[i]), short.MinValue, short.MaxValue);
                buffer[i] = sample;
            }
            return true;
        }
        else
        {
            HandleEndOfTrack(buffer);
            return false;
        }
    }

    private unsafe bool FillStreamBufferDirectly(short[] buffer)
    {
        if (IsPlayingImpl())
        {
            fixed (short* b = buffer)
            {
                _ = ZMusic.ZMusic_FillStream((_ZMusic_MusicStream_Struct*)m_zMusicSong, b, sizeof(short) * buffer.Length);
            }
            return true;
        }
        else
        {
            HandleEndOfTrack(buffer);
            return false;
        }
    }

    private void HandleEndOfTrack(short[] buffer)
    {
        // Zero-fill the buffer in case the stream doesn't respond immediately to the end request
        for (int i = 0; i < buffer.Length; i++)
        {
            buffer[i] = 0;
        }

        m_stoppedAction?.Invoke();
        m_stoppedAction = null;
    }

    private unsafe bool IsPlayingImpl()
    {
        ObjectDisposedException.ThrowIf(m_disposed, this);
        return m_zMusicSong != nint.Zero && ZMusic.ZMusic_IsPlaying((_ZMusic_MusicStream_Struct*)m_zMusicSong) != 0;
    }

    /// <summary>
    /// Stops playback and closes any native resources in use
    /// </summary>
    public unsafe void Stop()
    {
        ObjectDisposedException.ThrowIf(m_disposed, this);

        try
        {
            // Ensure we are not currently _starting_ playback, as that would put us in a weird state
            m_playStartTask?.Wait();
            m_playStartTask?.Dispose();
            m_playStartTask = null;

            // Stop playing
            m_activeStream?.Stop();

            // Ask ZMusic to close the stream
            if (m_zMusicSong != IntPtr.Zero)
            {
                if (IsPlayingImpl())
                {
                    ZMusic.ZMusic_Stop((_ZMusic_MusicStream_Struct*)m_zMusicSong);
                }
                ZMusic.ZMusic_Close((_ZMusic_MusicStream_Struct*)m_zMusicSong);
                m_zMusicSong = IntPtr.Zero;
            }

            m_fillBlockAction = null;
        }
        catch (Exception e)
        {
            LastErr = e;
        }
    }

    protected virtual void Dispose(bool disposing)
    {
        if (!m_disposed)
        {
            if (disposing)
            {
                Stop();
                m_activeStream?.Dispose();
                m_activeStream = null;
            }

            m_disposed = true;
        }
    }

    public void Dispose()
    {
        Dispose(disposing: true);
        GC.SuppressFinalize(this);
    }
}
