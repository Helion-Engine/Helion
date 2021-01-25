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

        /// <summary>
        /// If true, saves to the config. If false, never saves.
        /// </summary>
        /// <remarks>
        /// If this is false, it means it is a transient field whereby it can
        /// be toggled via the console, but will never be saved. Upon loading
        /// the game again, it will always have the default definition.
        /// </remarks>
        public readonly bool Save;

        public ConfigInfoAttribute(string description, string? extendedDescription = null, bool advanced = false,
            bool save = true)
        {
            Description = description;
            ExtendedDescription = extendedDescription;
            Advanced = advanced;
            Save = save;
        }

        public static string? GetDescription(FieldInfo field)
        {
            Attribute? attribute = field.GetCustomAttribute(typeof(ConfigInfoAttribute));
            if (attribute is ConfigInfoAttribute configInfoAttribute)
                return configInfoAttribute.Description;
            return null;
        }

        public static bool IsSaved(FieldInfo field)
        {
            Attribute? attribute = field.GetCustomAttribute(typeof(ConfigInfoAttribute));
            if (attribute is ConfigInfoAttribute configInfoAttribute)
                return configInfoAttribute.Save;
            return false;
        }
    }
}
