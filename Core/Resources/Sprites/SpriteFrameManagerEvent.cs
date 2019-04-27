using Helion.Util;

namespace Helion.Resources.Sprites
{
    /// <summary>
    /// The event type that signifies what kind of action the event is.
    /// </summary>
    public enum SpriteFrameManagerEventType
    {
        CreateOrUpdate,
        Delete
    }

    /// <summary>
    /// An event that contains data about something that occurred in an image
    /// manager.
    /// </summary>
    public class SpriteFrameManagerEvent
    {
        /// <summary>
        /// The type of event this is.
        /// </summary>
        public SpriteFrameManagerEventType Type { get; }

        /// <summary>
        /// The base 5 letters of the frame. This means if your sprite is 
        /// POSSA2A8 then the frame base is "POSSA".
        /// </summary>
        public UpperString FrameBase { get; }

        /// <summary>
        /// The rotations for the frame.
        /// </summary>
        public SpriteRotations Rotations { get; }

        public SpriteFrameManagerEvent(SpriteFrameManagerEventType type, UpperString frameBase,
            SpriteRotations rotations)
        {
            Type = type;
            FrameBase = frameBase;
            Rotations = rotations;
        }

        /// <summary>
        /// A shortcut to for making a creation event.
        /// </summary>
        /// <param name="frameBase">The first 5 letters of the frame.</param>
        /// <param name="rotations">The rotations for this frame.</param>
        /// <returns>The event for this data.</returns>
        public static SpriteFrameManagerEvent Create(UpperString frameBase, SpriteRotations rotations)
        {
            return new SpriteFrameManagerEvent(SpriteFrameManagerEventType.CreateOrUpdate, frameBase, rotations);
        }
    }
}
