using System;
using System.IO;
using System.Linq;
using Commons.Music.Midi;
using Helion.Audio;
using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Client.Music
{
    public class MidiDotNetMusicPlayer : IMusicPlayer
    {
        private readonly IMidiOutput m_output;
        private MidiPlayer? m_player;
        private bool m_disposed;
        private string m_lastDataHash = "";
        
        public MidiDotNetMusicPlayer()
        {
            IMidiAccess access = MidiAccessManager.Default;
            m_output = access.OpenOutputAsync(access.Outputs.Last().Id).Result;
        }

        ~MidiDotNetMusicPlayer()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        public void SetVolume(float volume)
        {
            // TODO: This doesn't work with the player.
        }

        private MidiPlayer CreatePlayer(byte[] data)
        {
            MidiMusic music = MidiMusic.Read(new MemoryStream(data));
            return new MidiPlayer(music, m_output);
        }
        
        public bool Play(byte[] data, bool loop = true, bool ignoreAlreadyPlaying = true)
        {
            string? hash = null;
            if (ignoreAlreadyPlaying)
            {
                hash = data.CalculateCrc32();
                if (hash == m_lastDataHash)
                    return true;
            }
            
            m_player?.Stop();
            m_player?.Dispose();

            m_lastDataHash = hash ?? data.CalculateCrc32();
            m_player = CreatePlayer(data);
            m_player.Play();

            // This is a horrible hack because the library won't loop and the
            // methods that should make it do so, don't.
            if (loop)
                m_player.PlaybackCompletedToEnd += () => Play(data, true, false);

            return true;
        }

        public void Stop()
        {
            m_player?.Stop();
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
            
            m_player?.Dispose();
            m_disposed = true;
        }
    }
}
