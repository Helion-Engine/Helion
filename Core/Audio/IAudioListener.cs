using System.Numerics;

namespace Helion.Audio
{
    /// <summary>
    /// The listener's position. All other sounds are to be played relative to
    /// this.
    /// </summary>
    public interface IAudioListener
    {
        /// <summary>
        /// The location of the listener.
        /// </summary>
        Vector3 Position { get; set; }
        
        /// <summary>
        /// The velocity of the listener.
        /// </summary>
        Vector3 Velocity { get; set; }
    }
}