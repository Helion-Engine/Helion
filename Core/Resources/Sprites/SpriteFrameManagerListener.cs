namespace Helion.Resources.Sprites
{
    /// <summary>
    /// Describes objects that are able to register for events with a sprite 
    /// manager and receive notifications for updates.
    /// </summary>
    public interface SpriteFrameManagerListener
    {
        /// <summary>
        /// Invoked when an event has occurred in the sprite manager the 
        /// implementor has registered to.
        /// </summary>
        /// <param name="frameEvent">The event from the sprite manager.</param>
        void HandleSpriteEvent(SpriteFrameManagerEvent frameEvent);
    }
}
