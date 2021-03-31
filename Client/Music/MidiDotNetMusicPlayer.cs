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
        private byte[]? m_lastPlayedData;
        private bool m_subscribed;
        
        public MidiDotNetMusicPlayer()
        {
            var access = MidiAccessManager.Default;
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

        private MidiPlayer? CreatePlayer(byte[] data)
        {
            try
            {
                MidiMusic music = MidiMusic.Read(new MemoryStream(data));
                return new MidiPlayer(music, m_output);
            }
            catch
            {
                // I am unsure if the library can throw, so this is my safety net.
                return null;
            }
        }
        
        public bool Play(byte[] data, bool loop = true, bool ignoreAlreadyPlaying = true)
        {
            if (m_disposed)
                return false;

            string? hash = null;
            if (ignoreAlreadyPlaying)
            {
                hash = data.CalculateCrc32();
                if (hash == m_lastDataHash)
                    return true;
            }
            
            if (m_subscribed)
                PerformUnsubscribe();
            
            m_player?.Stop();
            m_player?.Dispose();

            m_lastPlayedData = data;
            m_lastDataHash = hash ?? data.CalculateCrc32();
            
            m_player = CreatePlayer(data);
            if (m_player == null)
            {
                m_lastPlayedData = null;
                m_lastDataHash = "";
                return false;
            }
            
            m_player.Play();

            // This is a horrible hack because the library won't loop and the
            // methods that should make it do so, don't.
            if (loop)
            {
                m_subscribed = true;
                m_player.PlaybackCompletedToEnd += Replay;
            }

            return true;
        }

        private void Replay()
        {
            if (m_lastPlayedData != null) 
                Play(m_lastPlayedData, true, false);
        }

        private void PerformUnsubscribe()
        {
            if (m_player == null) 
                return;
            
            m_player.PlaybackCompletedToEnd -= Replay;
            m_subscribed = false;
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
            
            PerformUnsubscribe();
            
            m_player?.Dispose();
            m_disposed = true;
        }
    }
}
