namespace Helion.Util
{
    /// <summary>
    /// Indicates an object can be ticked, which means some kind of action is
    /// done with respect to many other things in some container.
    /// </summary>
    public interface ITickable
    {
        /// <summary>
        /// Performs the ticking of the object.
        /// </summary>
        void Tick();
    }
}