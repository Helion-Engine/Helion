using System;

namespace Helion.Util.Configs.Values
{
    /// <summary>
    /// Metadata for a config component or value.
    /// </summary>
    public class ConfigInfoAttribute : Attribute
    {
        /// <summary>
        /// A high level description of the attribute.
        /// </summary>
        public readonly string Description;

        /// <summary>
        /// Additional descriptions that can be requested from the user if they
        /// wish to know more.
        /// </summary>
        public readonly string? ExtendedDescription;

        /// <summary>
        /// Whether this is an advanced setting or not. Advanced settings are
        /// intended for people who know what they are doing, and should be
        /// hidden from people unless they acknowledge they know what they are
        /// doing.
        /// </summary>
        public readonly bool Advanced;

        public ConfigInfoAttribute(string description, string? extendedDescription = null, bool advanced = false)
        {
            Description = description;
            ExtendedDescription = extendedDescription;
            Advanced = advanced;
        }
    }
}
