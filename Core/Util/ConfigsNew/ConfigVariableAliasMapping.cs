using System;
using System.Collections;
using System.Collections.Generic;
using Helion.Util.ConfigsNew.Values;

namespace Helion.Util.ConfigsNew
{
    public class ConfigVariableAliasMapping : IEnumerable<(string name, IConfigValue value)>
    {
        private readonly Dictionary<string, IConfigValue> m_nameToComponent;
        
        public ConfigVariableAliasMapping(ConfigNew config)
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

        public IEnumerator<(string name, IConfigValue value)> GetEnumerator()
        {
            foreach ((string pathName, IConfigValue configValue) in m_nameToComponent)
                yield return (pathName, configValue);
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
