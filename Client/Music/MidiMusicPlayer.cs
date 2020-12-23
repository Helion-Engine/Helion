using System;
using System.IO;
using System.Linq;
using Helion.Audio;
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

        private OutputDevice? m_outputDevice;
        private Playback? m_playback;
        private bool m_isDisposed;

        /// <summary>
        /// Creates a music player using the default MIDI device.
        /// </summary>
        public MidiMusicPlayer()
        {
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

        public bool Play(byte[] data)
        {
            if (m_isDisposed)
                return false;

            if (m_outputDevice == null)
            {
                Log.Warn("Cannot play music, no MIDI device found (at startup)");
                return false;
            }

            try
            {
                Stop();

                MemoryStream stream = new(data);
                MidiFile midi = MidiFile.Read(stream);
                Playback newPlayback = midi.GetPlayback(m_outputDevice);

                m_playback = newPlayback;
                newPlayback.InterruptNotesOnStop = true;
                newPlayback.Loop = true;
                newPlayback.Start();

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
