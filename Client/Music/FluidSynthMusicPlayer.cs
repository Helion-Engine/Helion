using System;
using System.IO;
using System.Threading;
using Helion.Audio;
using Helion.Util.Extensions;
using NFluidsynth;
using static Helion.Util.Assertion.Assert;

namespace Helion.Client.Music
{
    public class FluidSynthMusicPlayer : IMusicPlayer
    {
        private bool m_disposed;
        private string m_lastDataHash = string.Empty;

        private readonly Synth m_synth;
        private readonly Settings m_settings;

        private Player? m_player;
        private Thread? m_thread;

        private class PlayParams
        {
            public string File { get; set; } = string.Empty;
            public bool Loop { get; set; }
        }
        
        public FluidSynthMusicPlayer(string soundFontFile)
        {
            m_settings = new Settings();
            m_settings[ConfigurationKeys.SynthAudioChannels].IntValue = 2;

            m_synth = new Synth(m_settings);
            m_synth.LoadSoundFont(soundFontFile, true);
            for (int i = 0; i < 16; i++)
                m_synth.SoundFontSelect(i, 0);
        }

        ~FluidSynthMusicPlayer()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        public void SetVolume(float volume)
        {
            var setting = m_settings[ConfigurationKeys.SynthGain];
            setting.DoubleValue = volume * setting.DoubleDefault;
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

            m_lastDataHash = hash ?? data.CalculateCrc32();

            Stop();

            const string file = "temp.mid";
            File.WriteAllBytes(file, data);
            m_thread = new Thread(new ParameterizedThreadStart(PlayThread));
            m_thread.Start(new PlayParams() { File = file, Loop = loop });

            return true;
        }

        private void PlayThread(object? param)
        {
            if (param is not PlayParams playParams)
                return;

            using (m_player = new Player(m_synth))
            {
                using (var adriver = new AudioDriver(m_synth.Settings, m_synth))
                {
                    if (playParams.Loop)
                        m_player.SetLoop(-1);
                    m_player.Add(playParams.File);           
                    m_player.Play();
                    m_player.Join();
                }
            }

            m_player = null;
        }


        public void Stop()
        {
            m_player?.Stop();
            m_thread?.Join();
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
            m_synth.Dispose();
            m_settings.Dispose();
            m_disposed = true;
        }
    }
}
