using System;
using Helion.Util.Configs.Values;
using IniParser;
using IniParser.Model;

namespace Helion.Util.Configs
{
    public partial class Config
    {
        private void WriteConfig()
        {
            try
            {
                FileIniDataParser parser = new();
                IniData data = new();
                WriteEngineFields(data);

                parser.WriteFile(m_path, data);
            }
            catch (Exception e)
            {
                Log.Error($"Unable to write config file: {e.Message}");
            }
        }

        private bool IsChanged()
        {
            // The generic type doesn't matter here, we just need any type.
            const string ChangedProperty = nameof(ConfigValue<int>.Changed);

            foreach (object configValue in m_pathToConfigValue.Values)
            {
                object? value = configValue.GetType().GetProperty(ChangedProperty)?.GetValue(configValue);
                bool changed = (bool)(value ?? false);
                if (changed)
                    return true;
            }

            return false;
        }

        private void WriteEngineFields(IniData data)
        {
            data.Sections.AddSection(EngineSectionName);
            RecursivelyWriteEngineData(this, data[EngineSectionName]);
        }

        private void RecursivelyWriteEngineData(object component, KeyDataCollection keyData, string path = "")
        {
            foreach (var (child, _, newPath, isValue) in GetRelevantComponentFields(component, path))
            {
                if (isValue)
                    keyData[newPath] = child.ToString();
                else
                    RecursivelyWriteEngineData(child, keyData, newPath);
            }
        }
    }
}
