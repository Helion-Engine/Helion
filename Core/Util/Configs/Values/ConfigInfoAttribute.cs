using System;
using System.Reflection;

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

        /// <summary>
        /// Gets the description of the config value.
        /// </summary>
        /// <param name="field">The field for the config value.</param>
        /// <returns>The explanation value, or null if the attribute does not
        /// exist on the object.</returns>
        public static string? GetDescription(FieldInfo field)
        {

            Attribute? attribute = field.GetCustomAttribute(typeof(ConfigInfoAttribute));
            if (attribute is ConfigInfoAttribute configInfoAttribute)
                return configInfoAttribute.Description;
            return null;
        }
    }
}
