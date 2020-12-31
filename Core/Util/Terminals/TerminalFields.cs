using System;
using System.Collections.Generic;
using System.Reflection;
using Helion.Util.Configuration;
using Helion.Util.Configuration.Attributes;
using Helion.Util.Extensions;

namespace Helion.Util.Terminals
{
    public class TerminalFields
    {
        private readonly Config m_config;
        private readonly Dictionary<string, object> m_nameToConfigValue = new();

        public TerminalFields(Config config)
        {
            m_config = config;

            PopulateNameToConfigMap();
        }

        /// <summary>
        /// Gets a list of all the variables that match the path provided. This
        /// takes into account the period. For example, "renderer." would have
        /// fields like "renderer.window" as part of the list. It matches based
        /// on prefix.
        /// </summary>
        /// <param name="path">The path to the config variable.</param>
        /// <returns>All the matches.</returns>
        public IEnumerable<string> GetAutocomplete(string path)
        {
            if (path.Empty())
                yield break;

            foreach (string pathKey in m_nameToConfigValue.Keys)
                if (pathKey.StartsWith(path, StringComparison.OrdinalIgnoreCase))
                    yield return pathKey;
        }

        /// <summary>
        /// Gets the config value at the path provided.
        /// </summary>
        /// <param name="path">The path (ex: "renderer.window.height").</param>
        /// <returns>The config value object, or null if no such value exists.
        /// </returns>
        public object? GetConfigValue(string path)
        {
            return m_nameToConfigValue.TryGetValue(path.ToLower(), out object? obj) ? obj : null;
        }

        private void PopulateNameToConfigMap()
        {
            Stack<(string, object)> pathToType = new();
            pathToType.Push(("", m_config.Engine));

            // The stack acts like recursion, going through and finding the
            // period separated path to every entry. For example, we would
            // have "renderer.window.height" being one path.
            while (!pathToType.Empty())
            {
                (string path, object obj) = pathToType.Pop();

                FieldInfo[] fields = obj.GetType().GetFields(BindingFlags.Public | BindingFlags.Instance);
                foreach (FieldInfo fieldInfo in fields)
                {
                    foreach (Attribute attribute in Attribute.GetCustomAttributes(fieldInfo.FieldType))
                    {
                        string lowerName = fieldInfo.Name.ToLower();
                        string newPath = path.Empty() ? lowerName : $"{path}.{lowerName}";

                        if (attribute.GetType() == typeof(ConfigComponentAttribute))
                        {
                            object fieldObj = fieldInfo.GetValue(obj) ?? throw new Exception("Should never fail to get the config object");
                            pathToType.Push((newPath, fieldObj));
                        }
                        else if (attribute.GetType() == typeof(ConfigValueComponentAttribute))
                        {
                            object fieldObj = fieldInfo.GetValue(obj) ?? throw new Exception("Should never fail to get the config object");
                            m_nameToConfigValue[newPath] = fieldObj;
                        }
                    }
                }
            }
        }
    }
}
