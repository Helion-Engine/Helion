namespace Helion.Util.Configs.Values
{
    public interface IConfigValue<out T>
    {
        /// <summary>
        /// The backing value.
        /// </summary>
        T Value { get; }

        /// <summary>
        /// If this value changed since loading.
        /// </summary>
        bool Changed { get; }

        /// <summary>
        /// Gets the stored value.
        /// </summary>
        /// <returns>The stored value.</returns>
        object Get();

        /// <summary>
        /// Sets the value to the object provided. This allows anything to be
        /// passed in and be configured as needed.
        /// </summary>
        /// <param name="obj">The object to try to set.</param>
        /// <returns>True on success, false if the object could not be turned
        /// into something that could be set. This will end up with a default
        /// value being set if false is returned.</returns>
        bool Set(object obj);
    }
}
