namespace Helion.Util.Configs.Values
{
    /// <summary>
    /// A result where non-zero means failure.
    /// </summary>
    public enum ConfigSetResult
    {
        /// <summary>
        /// If the config value was set, replacing an old value with a new one
        /// that is different.
        /// </summary>
        Set = 0,
        
        /// <summary>
        /// If the value trying to be set is equivalent to what is already in
        /// place.
        /// </summary>
        Unchanged,
        
        /// <summary>
        /// If the change cannot be applied since it has to be queued. This
        /// means it passed the filtering, but was not directly set yet.
        /// </summary>
        Queued,
        
        /// <summary>
        /// If the provided value could not be converted.
        /// </summary>
        NotSetByBadConversion,
        
        /// <summary>
        /// If filtered out.
        /// </summary>
        NotSetByFilter
    }
}
