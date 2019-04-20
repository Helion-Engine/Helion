namespace Helion.Resources.Images
{
    /// <summary>
    /// Describes objects that are able to register for events with an image 
    /// manager and receive notifications for updates.
    /// </summary>
    public interface ImageManagerListener
    {
        /// <summary>
        /// Invoked when an event has occurred in the image manager the 
        /// implementor has registered to.
        /// </summary>
        /// <param name="imageEvent">The event from the image manager.</param>
        void HandleImageEvent(ImageManagerEvent imageEvent);
    }
}
