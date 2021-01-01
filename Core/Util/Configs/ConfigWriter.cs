using System;
using System.Reflection;
using Helion.Input;
using Helion.Util.Extensions;
using IniParser;
using IniParser.Model;

namespace Helion.Util.Configs
{
    public partial class Config
    {
        private void WriteConfig()
        {
            // TODO: AND if not changed, then return.
            if (!m_newConfig)
                return;

            try
            {
                FileIniDataParser parser = new();
                IniData data = new();
                WriteEngineFields(data);
                WriteKeys(data);

                parser.WriteFile(m_path, data);
            }
            catch (Exception e)
            {
                Log.Error($"Unable to write config file: {e.Message}");
            }
        }

        private void WriteEngineFields(IniData data)
        {
            data.Sections.AddSection(EngineSectionName);
            RecursivelyWriteEngineData(this, data[EngineSectionName]);
        }

        private void RecursivelyWriteEngineData(object component, KeyDataCollection keyData, string path = "")
        {
            foreach (FieldInfo fieldInfo in component.GetType().GetFields())
            {
                bool hasAttribute = HasConfigAttribute(fieldInfo);
                bool isValue = IsConfigValue(fieldInfo);

                if (!hasAttribute && !isValue)
                    continue;

                string lowerName = fieldInfo.Name.ToLower();
                string newPath = path.Empty() ? lowerName : $"{path}.{lowerName}";
                object childComponent = fieldInfo.GetValue(component) ?? throw new Exception($"Failed to get field at path '{path}'");

                if (isValue)
                    keyData[newPath] = childComponent.ToString();
                else
                    RecursivelyWriteEngineData(childComponent, keyData, newPath);
            }
        }

        private void WriteKeys(IniData data)
        {
            data.Sections.AddSection(KeysSectionName);
            KeyDataCollection keySection = data[KeysSectionName];

            foreach ((InputKey inputKey, string command) in Keys)
                if (inputKey != InputKey.Unknown)
                    keySection[inputKey.ToString()] = command;
        }
    }
}
