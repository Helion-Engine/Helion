using System.Numerics;
using Helion.Audio;

namespace Helion.Client.OpenAL
{
    public class OpenALAudioListener : IAudioListener
    {
        public Vector3 Position { get; set; } = Vector3.Zero;
        public Vector3 Velocity { get; set; } = Vector3.Zero;
    }
}