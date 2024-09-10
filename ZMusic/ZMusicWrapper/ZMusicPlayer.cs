namespace ZMusicWrapper
{
    using System;
    using System.Text;
    using System.Threading.Tasks;
    using global::ZMusicWrapper.Generated;

    /// <summary>
    /// Provides basic playback functionality for formats that ZMusic supports
    /// </summary>
    public class ZMusicPlayer : IDisposable
    {
        private const int DefaultSampleRate = 44100;
        private const int DefaultChannels = 2;

        private IOutputStreamFactory m_streamFactory;
        private string m_soundFontPath;

        private IOutputStream? m_activeStream;
        private Task? m_playStartTask;
        private IntPtr m_zMusicSong;
        private Action? m_stoppedAction;
        private float m_sourceVolume;
        private bool m_loop;
        private bool m_soundFontLoaded;

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

        /// <summary>
        /// Gets the last error encountered when starting or stopping playback.
        /// This will be null if we have not encountered any errors.
        /// </summary>
        public Exception? LastErr { get; private set; }

        /// <summary>
        /// Creates a new music player
        /// </summary>
        /// <param name="outputStreamFactory">Output stream factory to use for playback</param>
        /// <param name="soundFontPath">Path to a SoundFont (.sf2) file for use when playing MIDI</param>
        /// <param name="sourceVolume">Initial volume for this source</param>
        public ZMusicPlayer(IOutputStreamFactory outputStreamFactory, string soundFontPath, float sourceVolume = 1.0f)
        {
            m_streamFactory = outputStreamFactory;
            m_soundFontPath = soundFontPath;
            m_sourceVolume = sourceVolume;
        }

        /// <summary>
        /// Determine whether a given byte array looks like MIDI or MUS data
        /// </summary>
        /// <param name="soundFileData">byte array representing the raw data of a music file</param>
        /// <returns>True if the input data appears to be MIDI or MUS, False otherwise</returns>
        public unsafe bool IsMIDI(byte[] soundFileData)
        {
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
                    return result;
                }
            }
            catch
            {
                // Whatever just happened, let's assume it wasn't MIDI
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
                    song = ZMusic.ZMusic_OpenSongMem(dataBytes, length, EMidiDevice_.MDEV_FLUIDSYNTH, null);
                    m_zMusicSong = (IntPtr)song;
                }

                ZMusic.ZMusic_GetStreamInfo(song, &info);

                if (ZMusic.ZMusic_IsMIDI(song) == 0)
                {
                    this.PlayStream(song, info.mSampleRate, info.mNumChannels, loop, this.m_streamFactory);
                }
                else
                {
                    if (!m_soundFontLoaded)
                    {
                        this.SetSoundFont(song, this.m_soundFontPath);
                    }
                    this.PlayStream(song, DefaultSampleRate, DefaultChannels, loop, this.m_streamFactory);
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
                    ZMusic.ZMusic_Start(song, 0, Convert.ToByte(this.m_loop));
                });
                m_playStartTask.Start();
            }
        }

        private unsafe void SetSoundFont(_ZMusic_MusicStream_Struct* song, string soundFontPath)
        {
            byte[] fluidSynthPathBytes = Encoding.UTF8.GetBytes(soundFontPath);
            fixed (byte* path = fluidSynthPathBytes)
            {
                ZMusic.ChangeMusicSettingInt(EIntConfigKey_.zmusic_fluid_samplerate, song, DefaultSampleRate, null);
                ZMusic.ChangeMusicSetting(EStringConfigKey_.zmusic_fluid_patchset, song, (sbyte*)path);
                m_soundFontLoaded = true;
            }
        }

        private unsafe void PlayStream(_ZMusic_MusicStream_Struct* song, int sampleRate, int channels, bool loop, IOutputStreamFactory streamFactory)
        {
            if (channels == 0) // what
                throw new Exception("Cannot play stream with no channels");

            m_activeStream = streamFactory.GetOutputStream(sampleRate, Math.Abs(channels));
            m_activeStream.SetVolume(m_sourceVolume);

            m_loop = loop;
            _ = ZMusic.ZMusic_Start(song, 0, Convert.ToByte(loop));

            if (channels > 0)
            {
                float[] data = new float[m_activeStream.ChannelCount * m_activeStream.BlockLength];

                this.m_activeStream.Play(buffer =>
                {
                    if (this.IsPlayingImpl())
                    {
                        fixed (float* p = data)
                        {
                            _ = ZMusic.ZMusic_FillStream(song, p, sizeof(float) * data.Length);
                        }
                        for (int i = 0; i < buffer.Length; i++)
                        {
                            short sample = (short)Math.Clamp((int)(32768 * data[i]), short.MinValue, short.MaxValue);
                            buffer[i] = sample;
                        }
                    }
                    else
                    {
                        // We've reached end-of-track
                        this.m_stoppedAction?.Invoke();
                        this.m_stoppedAction = null;
                    }
                });
            }
            else if (channels < 0) // Samples should be handled as 16-bit ints
            {
                short[] data = new short[m_activeStream.ChannelCount * m_activeStream.BlockLength];

                this.m_activeStream.Play(buffer =>
                {
                    if (this.IsPlayingImpl())
                    {
                        fixed (short* b = buffer)
                        {
                            _ = ZMusic.ZMusic_FillStream(song, b, sizeof(short) * data.Length);
                        }
                    }
                    else
                    {
                        // We've reached end-of-track
                        this.m_stoppedAction?.Invoke();
                        this.m_stoppedAction = null;
                    }
                });
            }
        }

        private unsafe bool IsPlayingImpl()
        {
            ObjectDisposedException.ThrowIf(m_disposed, this);
            return this.m_zMusicSong != nint.Zero && ZMusic.ZMusic_IsPlaying((_ZMusic_MusicStream_Struct*)this.m_zMusicSong) != 0;
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
                m_activeStream?.Dispose();
                m_activeStream = null;

                // Ask ZMusic to close the stream
                if (m_zMusicSong != IntPtr.Zero)
                {
                    if (IsPlayingImpl())
                    {
                        ZMusic.ZMusic_Stop((_ZMusic_MusicStream_Struct*)this.m_zMusicSong);
                    }
                    ZMusic.ZMusic_Close((_ZMusic_MusicStream_Struct*)this.m_zMusicSong);
                    m_zMusicSong = IntPtr.Zero;
                }
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
                    this.Stop();
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
}
