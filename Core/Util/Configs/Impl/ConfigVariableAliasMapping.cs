using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Helion.Util.Configs.Values;

namespace Helion.Util.Configs.Impl
{
    public class ConfigVariableAliasMapping : IConfigVariableAliasMapping
    {
        private readonly Dictionary<string, IConfigValue> m_nameToComponent;
        
        public ConfigVariableAliasMapping(IConfig config)
        {
            m_nameToComponent = new Dictionary<string, IConfigValue>(StringComparer.OrdinalIgnoreCase)
            {
                ["m_pitch"] = config.Mouse.Pitch,
                ["m_sensitivity"] = config.Mouse.Sensitivity,
                ["m_yaw"] = config.Mouse.Yaw,
                ["mouse_sensitivity"] = config.Mouse.Sensitivity,
                ["sv_nomonsters"] = config.Game.NoMonsters
            };
        }

        public bool TryGet(string name, [NotNullWhen(true)] out IConfigValue? configValue)
        {
            return m_nameToComponent.TryGetValue(name, out configValue);
        }

        public IEnumerator<(string name, IConfigValue value)> GetEnumerator()
        {
            foreach ((string pathName, IConfigValue configValue) in m_nameToComponent)
                yield return (pathName, configValue);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
