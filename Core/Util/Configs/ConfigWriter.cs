using System;
using Helion.Input;
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
            foreach (var (child, newPath, isValue) in GetRelevantComponentFields(component, path))
            {
                if (isValue)
                    keyData[newPath] = child.ToString();
                else
                    RecursivelyWriteEngineData(child, keyData, newPath);
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
