using System;
using System.IO;
using System.Linq;
using Helion.Audio;
using Helion.Util.Bytes;
using Helion.Util.Configs;
using Melanchall.DryWetMidi.Devices;
using Melanchall.DryWetMidi.Smf;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.Client.Music
{
    /// <summary>
    /// A simple music player for MIDI files.
    /// </summary>
    public class MidiMusicPlayer : IMusicPlayer
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private readonly Config m_config;
        private OutputDevice? m_outputDevice;
        private Playback? m_playback;
        private bool m_isDisposed;
        private string m_md5 = string.Empty;

        /// <summary>
        /// Creates a music player using the default MIDI device.
        /// </summary>
        public MidiMusicPlayer(Config config)
        {
            m_config = config;
            m_outputDevice = OutputDevice.GetAll().FirstOrDefault();

            if (m_outputDevice != null)
                Log.Info("Using MIDI device: {0}", m_outputDevice.Name);
            else
                Log.Warn("Unable to find MIDI device to play music with");
        }

        ~MidiMusicPlayer()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        public void SetVolume(float volume)
        {
            if (m_outputDevice == null)
                return;

            volume = Math.Clamp(volume, 0.0f, 1.0f);
            ushort volumeValue = (ushort)(volume * ushort.MaxValue);
            m_outputDevice.Volume = new Volume(volumeValue, volumeValue);
        }

        private float GetVolumeFromConfig()
        {
            float volume = (float)(m_config.Audio.Volume * m_config.Audio.MusicVolume);
            return Math.Clamp(volume, 0.0f, 1.0f);
        }

        public bool Play(byte[] data, bool loop = true, bool ignoreAlreadyPlaying = true)
        {
            if (m_isDisposed)
                return false;

            using MemoryStream ms = new MemoryStream(data);
            string md5 = Files.CalculateMD5(ms);
            if (ignoreAlreadyPlaying && md5 == m_md5)
                return true;

            m_md5 = md5;

            if (m_outputDevice == null)
            {
                Log.Warn("Cannot play music, no MIDI device found (at startup)");
                return false;
            }

            try
            {
                Stop();

                float volume = GetVolumeFromConfig();
                if (volume <= 0.0f)
                    return true;

                MemoryStream stream = new(data);
                MidiFile midi = MidiFile.Read(stream);
                Playback newPlayback = midi.GetPlayback(m_outputDevice);

                m_playback = newPlayback;
                newPlayback.InterruptNotesOnStop = true;
                newPlayback.Loop = loop;
                newPlayback.Start();

                // This sucks, but this has to come here because the MIDI APIs
                // apparently aren't fully initialized after grabbing a device,
                // so we need to defer volume setting until we load something.
                SetVolume(volume);

                return true;
            }
            catch
            {
                return false;
            }
        }

        public void Stop()
        {
            if (m_isDisposed)
                return;

            m_playback?.Stop();
            m_playback?.Dispose();
            m_playback = null;
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            PerformDispose();
        }

        private void PerformDispose()
        {
            if (m_isDisposed)
                return;

            m_isDisposed = true;

            m_playback?.Dispose();
            m_playback = null;

            m_outputDevice?.Dispose();
            m_outputDevice = null;
        }
    }
}
