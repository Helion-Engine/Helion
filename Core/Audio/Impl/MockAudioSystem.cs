using System;
using System.Collections.Generic;

namespace Helion.Audio.Impl
{
    public class MockAudioSystem : IAudioSystem
    {
        public IMusicPlayer Music => new MockMusicPlayer();

        public event EventHandler? DeviceChanging;

        public IAudioSourceManager CreateContext() => new MockAudioSourceManager();

        public void Dispose()
        {

        }

        public string GetDeviceName()
        {
            return string.Empty;
        }

        public IEnumerable<string> GetDeviceNames()
        {
            return Array.Empty<string>();
        }

        public bool SetDevice(string deviceName)
        {
            return true;
        }

        public void SetVolume(double volume)
        {

        }

        public void ThrowIfErrorCheckFails()
        {

        }
    }
}
