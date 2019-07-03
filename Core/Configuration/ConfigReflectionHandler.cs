using System;
using System.IO;
using System.Linq;
using System.Reflection;
using IniParser;
using IniParser.Model;
using MoreLinq;
using NLog;
using static Helion.Util.Assert;

namespace Helion.Configuration
{
    public static class ConfigReflectionHandler
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        private static bool HasConfigSectionAttribute(FieldInfo fieldInfo)
        {
            return fieldInfo.IsPublic && fieldInfo.FieldType.IsDefined(typeof(ConfigSection), false);
        }
        
        private static bool HasConfigComponentAttribute(FieldInfo fieldInfo)
        {
            return fieldInfo.IsPublic && fieldInfo.FieldType.IsDefined(typeof(ConfigComponent), false);
        }

        private static void SetConfigValue(object element, string lowerFieldName, string value)
        {
            foreach (FieldInfo f in element.GetType().GetFields())
            {
                if (f.FieldType == typeof(ConfigValue<>) && f.Name.ToLower() == lowerFieldName)
                {
                    if (f.FieldType.GenericTypeArguments.Length != 1)
                    {
                        Fail("Generic type for ConfigValue<> has 0 or 2+ types when it should have 1");
                        continue;
                    }

                    Type type = f.FieldType.GenericTypeArguments[0];
                    if (type == typeof(bool))
                    {
                        
                    }                        
                    else if (type == typeof(double))
                    {
                        
                    }
                    else if (type == typeof(int))
                    {
                        
                    }
                    else if (type == typeof(string))
                    {
                        
                    }
                }
            }
            // TODO
//            element.GetType().GetFields()
//                   .Where(f => f.FieldType == typeof(ConfigValue<>) && f.Name.ToLower() == lowerFieldName)
//                   .ForEach(System.Console.WriteLine);
        }
        
        private static void RecursivelyReadKeyValue(object element, string value, string[] lowerTokens, int tokenIndex = 0)
        {
            if (tokenIndex == lowerTokens.Length - 1)
            {
                SetConfigValue(element, lowerTokens[lowerTokens.Length - 1], value);
                return;
            }
            
            element.GetType().GetFields()
                   .Where(HasConfigComponentAttribute)
                   .Select(fieldInfo => (lowerFieldName: fieldInfo.Name.ToLower(), fieldValue: fieldInfo.GetValue(element)))
                   .Where(pair => pair.lowerFieldName == lowerTokens[tokenIndex])
                   .ForEach(pair => RecursivelyReadKeyValue(pair.fieldValue, value, lowerTokens, tokenIndex + 1));
        }

        private static void ReadSections(object root, IniData data)
        {
            root.GetType().GetFields()
                .Where(HasConfigSectionAttribute)
                .Select(fieldInfo => (lowerFieldName: fieldInfo.Name.ToLower(), fieldValue: fieldInfo.GetValue(root)))
                .ForEach(pair =>
                {
                    // Do a select here instead? We should find a way to inline
                    // this into the above linq query!
                    foreach (KeyData keyValue in data[pair.lowerFieldName])
                    {
                        string[] tokens = keyValue.KeyName.ToLower().Split('.');
                        RecursivelyReadKeyValue(pair.fieldValue, keyValue.Value, tokens);
                    }
                });
        }
        
        public static void ReadIntoFieldsRecursively(object root, string path)
        {
            if (!File.Exists(path))
            {
                log.Info("Config file not found, generating new config file");
                return;
            }
            
            try
            {
                FileIniDataParser parser = new FileIniDataParser();
                IniData data = parser.ReadFile(path);
                ReadSections(root, data);
            }
            catch
            {
                log.Error("Unable to read config at path: {0}", path);
            }
        }

        public static void WriteFieldsRecursively(object root, string path)
        {
            // TODO
        }
    }
}