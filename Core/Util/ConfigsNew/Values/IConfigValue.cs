namespace Helion.Util.ConfigsNew.Values
{
    /// <summary>
    /// A value that can be read and written to in a config.
    /// </summary>
    public interface IConfigValue
    {
        /// <summary>
        /// The value that backs this config value.
        /// </summary>
        object Value { get; }
        
        /// <summary>
        /// True if this value has changed since loading.
        /// </summary>
        bool Changed { get; }
        
        /// <summary>
        /// The flags on when this value should be changed.
        /// </summary>
        ConfigSetFlags SetFlags { get; }

        /// <summary>
        /// Tries to set the config value with some object. Will attempt to do
        /// reasonable conversions (like passing a string "5" to an integer
        /// version will convert it to the integer 5 and then try to set it).
        /// </summary>
        /// <remarks>
        /// Some conversion information:
        ///     numeric  -> bool:     0 = false, anything else = true
        ///     bool     -> numeric:  false = 0, true = 1
        ///     string   -> bool:     "" = false, anything else = true
        ///     string   -> numeric:  attempts a TryParse
        ///     anything -> string:   Jsonification, or ToString()
        /// </remarks>
        /// <param name="newValue">The new value.</param>
        /// <returns>The set result.</returns>
        ConfigSetResult Set(object newValue);
        
        /// <summary>
        /// Applies the queued changes, if any.
        /// </summary>
        /// <param name="flagType">The mask which must be present. This requires all
        /// of the bits to be set to apply the change.</param>
        void ApplyQueuedChange(ConfigSetFlags flagType);
    }
}
