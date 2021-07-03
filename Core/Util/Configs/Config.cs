using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using Helion.Util.Configs.Components;
using Helion.Util.Configs.Tree;
using Helion.Util.Configs.Values;
using Helion.Util.Extensions;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.Util.Configs
{
    /// <summary>
    /// A container for all of the configuration data.
    /// </summary>
    public partial class Config : IDisposable
    {
        private const string EngineSectionName = "engine";
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        // If you want to add new serializable fields into some section, they
        // should be here. Order technically doesn't matter, but it is clearer
        // for developers adding new fields to add them here.
        public readonly ConfigAudio Audio = new();
        public readonly ConfigConsole Console = new();
        public readonly ConfigDeveloper Developer = new();
        public readonly ConfigFiles Files = new();
        public readonly ConfigGame Game = new();
        public readonly ConfigHud Hud = new();
        public readonly ConfigControls Controls = new();
        public readonly ConfigMouse Mouse = new();
        public readonly ConfigRender Render = new();
        public readonly ConfigWindow Window = new();
        public readonly ConfigPlayer Player = new();
        public readonly ConfigCompat Compatibility = new();

        // Anything below this is not serialized into the engine section of the
        // config file.
        public readonly ConfigTree Tree;
        private readonly string m_path;
        private readonly Dictionary<string, (object value, FieldInfo field)> m_pathToConfigValue = new();
        private bool m_disposed;

        public Config(string path = "config.ini")
        {
            m_path = path;
            ReadConfig(path);
            Tree = new(this);
            PopulatePathConfigMapEngineRecursive(this);
        }

        ~Config()
        {
            FailedToDispose(this);
            PerformDispose();
        }

        internal static bool HasConfigAttribute(FieldInfo fieldInfo) => HasConfigAttribute(fieldInfo.FieldType);

        internal static bool HasConfigAttribute(Type type)
        {
            return type.IsDefined(typeof(ConfigInfoAttribute), true);
        }

        internal static bool IsConfigValue(FieldInfo fieldInfo) => IsConfigValueType(fieldInfo.FieldType);

        internal static bool IsConfigValueType(Type type)
        {
            // While this is probably overkill since at the time of writing
            // there is only an immediate parent inheritance, walking the tree
            // to the parent is the most robust solution. We also have to do
            // some weird stuff because checking generic parents requires the
            // use of a different type method for checking inheritance.
            Type? t = type;
            while (t != null)
            {
                if (t.IsGenericType && t.GetGenericTypeDefinition() == typeof(ConfigValue<>))
                    return true;
                t = t.BaseType;
            }

            return false;
        }

        internal IEnumerable<(object childComponent, FieldInfo fieldInfo, string path, bool isValue)>
            GetRelevantComponentFields(object component, string path = "")
        {
            foreach (FieldInfo fieldInfo in component.GetType().GetFields())
            {
                bool hasAttribute = HasConfigAttribute(fieldInfo);
                bool isValue = IsConfigValue(fieldInfo);

                if (!hasAttribute && !isValue)
                    continue;

                string lowerName = fieldInfo.Name.ToLower();
                string newPath = path.Empty() ? lowerName : $"{path}.{lowerName}";
                object childComponent = fieldInfo.GetValue(component) ?? throw new Exception($"Failed to get field for path '{newPath}'");

                yield return (childComponent, fieldInfo, newPath, isValue);
            }
        }

        private void PopulatePathConfigMapEngineRecursive(object component, string path = "")
        {
            foreach (var (child, fieldInfo, newPath, isValue) in GetRelevantComponentFields(component, path))
            {
                if (isValue)
                    m_pathToConfigValue[newPath] = (child, fieldInfo);
                else
                    PopulatePathConfigMapEngineRecursive(child, newPath);
            }
        }

        /// <summary>
        /// Gets a specific config value from an absolute path.
        /// </summary>
        /// <param name="path">The path to the config value. This should be
        /// absolute, like "render.window.height".</param>
        /// <returns>The config value object, or null if no path matches any
        /// indexed object.</returns>
        public object? GetConfigValue(string path)
        {
            if (m_pathToConfigValue.TryGetValue(path.ToLower(), out var obj))
                return obj.value;
            return null;
        }

        /// <summary>
        /// Uses a wildcard glob to search for all the matching possibilities.
        /// This always treats the glob as being anchored to the beginning of
        /// the word (similar to a regex starting with "^").
        /// </summary>
        /// <param name="glob">The glob string. This only supports asterisks,
        /// so you can search things like "*fps*" or "render.*" or such. The
        /// stars match anything (including nothing). This is not a full regex
        /// since it only supports *'s being turned into ".*". This means if
        /// you pass in "render.*" that is the same regex as "render\..*".
        /// </param>
        /// <returns>The path and config value object pair for all matches.
        /// </returns>
        public IEnumerable<(string path, object configValue, FieldInfo fieldInfo)>
            GetConfigValueWildcard(string glob)
        {
            string regexText = glob.Replace(".", @"\.").Replace("*", ".*");
            Regex regex = new($"^{regexText}", RegexOptions.Compiled | RegexOptions.IgnoreCase);

            foreach ((string path, (object configValue, FieldInfo fieldInfo)) in m_pathToConfigValue)
                if (regex.IsMatch(path))
                    yield return (path, configValue, fieldInfo);
        }

        public void Dispose()
        {
            GC.SuppressFinalize(this);
            PerformDispose();
        }

        private void PerformDispose()
        {
            if (m_disposed)
                return;

            WriteConfig();
            m_disposed = true;
        }
    }
}
