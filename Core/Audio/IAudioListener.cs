using System.Numerics;

namespace Helion.Audio
{
    public interface IAudioListener
    {
        Vector3 Position { get; set; }
        Vector3 Velocity { get; set; }
    }
}