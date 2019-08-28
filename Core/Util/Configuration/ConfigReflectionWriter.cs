using System;
using System.Reflection;
using IniParser;
using IniParser.Model;
using NLog;

namespace Helion.Util.Configuration
{
    public static class ConfigReflectionWriter
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public static void WriteFieldsRecursively(object config, string path)
        {
            try
            {
                FileIniDataParser parser = new FileIniDataParser();
                IniData data = new IniData();
                PopulateIniSections(config, data);
                parser.WriteFile(path, data);
            }
            catch
            {
                Log.Error("Failure to write config file to: {0}", path);
            }
        }

        private static string ConfigValueToString(object configValue, Type fieldType)
        {
            if (configValue is ConfigValue<double> node)
            {
                double value = node;
                
                // Doubles don't have a trailing zero if they are close to zero
                // so we want to add that for anyone who is editing the ini as
                // they won't know what's floating point and what's not.
                if (MathHelper.AreEqual(Math.Round(value), value))
                    return $"{value}.0";
            }

            return configValue.ToString()?.ToLower() ?? "?";
        }

        private static void PopulateIniKeysRecursively(object configNode, KeyDataCollection keyData, string keySoFar = "")
        {
            foreach (FieldInfo field in configNode.GetType().GetFields())
            {
                string lowerName = field.Name.ToLower();
                string newKeyName = (string.IsNullOrEmpty(keySoFar) ? lowerName : $"{keySoFar}.{lowerName}");

                if (ConfigReflectionReader.HasConfigValueAttribute(field))
                {
                    keyData[newKeyName] = ConfigValueToString(field.GetValue(configNode), field.FieldType);
                    continue;
                }

                if (ConfigReflectionReader.HasConfigComponentAttribute(field)) 
                    PopulateIniKeysRecursively(field.GetValue(configNode), keyData, newKeyName);
            }
        }

        private static void PopulateIniSections(object config, IniData data)
        {
            foreach (FieldInfo field in config.GetType().GetFields())
            {
                if (!ConfigReflectionReader.HasConfigSectionAttribute(field)) 
                    continue;
                
                string lowerName = field.Name.ToLower();
                data.Sections.AddSection(lowerName);
                PopulateIniKeysRecursively(field.GetValue(config), data[lowerName]);
            }
        }
    }
}