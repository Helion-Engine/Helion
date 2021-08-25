using System;

namespace Helion.Util.ConfigsNew.Values
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
        /// If true, saves to the config. If false, never saves.
        /// </summary>
        /// <remarks>
        /// If this is false, it means it is a transient field whereby it can
        /// be toggled via the console, but will never be saved. Upon loading
        /// the game again, it will always have the default definition.
        /// </remarks>
        public readonly bool Save;

        public ConfigInfoAttribute(string description, bool save = true)
        {
            Description = description;
            Save = save;
        }
    }
}
