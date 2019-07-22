using Helion.Util.Extensions;
using IniParser;
using IniParser.Model;
using MoreLinq;
using NLog;
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using static Helion.Util.Assertion.Assert;

namespace Helion.Util.Configuration
{
    public static class ConfigReflectionReader
    {
        private static readonly Logger log = LogManager.GetCurrentClassLogger();

        internal static bool HasConfigSectionAttribute(FieldInfo fieldInfo)
        {
            return fieldInfo.FieldType.IsDefined(typeof(ConfigSection), true);
        }
        
        internal static bool HasConfigComponentAttribute(FieldInfo fieldInfo)
        {
            return fieldInfo.FieldType.IsDefined(typeof(ConfigComponent), true);
        }
        
        internal static bool HasConfigValueAttribute(FieldInfo fieldInfo)
        {
            return fieldInfo.FieldType.IsDefined(typeof(ConfigValueComponent), true);
        }

        private static object? RecursivelyFindField(object node, string[] lowerTokens, int tokenIndex = 0)
        {
            Precondition(tokenIndex < lowerTokens.Length, "Recursion for config key field out of range");
            
            if (tokenIndex == lowerTokens.Length - 1)
            {
                return (from field in node.GetType().GetFields()
                        where HasConfigValueAttribute(field)
                        let lowerFieldName = field.Name.ToLower()
                        where lowerFieldName == lowerTokens[tokenIndex]
                        select field.GetValue(node))
                        .FirstOrDefault();
            }
            
            return (from field in node.GetType().GetFields() 
                    where HasConfigComponentAttribute(field) 
                    let lowerFieldName = field.Name.ToLower() 
                    where lowerFieldName == lowerTokens[tokenIndex] 
                    select RecursivelyFindField(field.GetValue(node), lowerTokens, tokenIndex + 1))
                    .FirstOrDefault();
        }

        private static bool IsEnumConfigValue(object configValueNode, out Type? enumType)
        {
            Type nodeType = configValueNode.GetType();
            if (nodeType.GenericTypeArguments.Length > 0)
            {
                enumType = nodeType.GenericTypeArguments[0];
                return enumType.IsEnum;
            }

            enumType = null;
            return false;
        }
        
        private static void SetConfigFieldWithEnum(object configValueNode, string lowerKeyName, string value, Type? enumType)
        {
            if (enumType == null || !enumType.IsEnum)
            {
                Fail($"Unexpected config field properties (field: {lowerKeyName}, type: {enumType})");
                return;
            }

            string[] enumNames = Enum.GetNames(enumType);
            Array enumValues = Enum.GetValues(enumType);
            Invariant(enumNames.Length == enumValues.Length, "C# says there are mismatching name/value enum lengths via reflection");

            for (int i = 0; i < enumNames.Length; i++)
            {
                if (!string.Equals(enumNames[i], value, StringComparison.OrdinalIgnoreCase)) 
                    continue;

                foreach (MethodInfo methodInfo in configValueNode.GetType().GetMethods())
                {
                    if (methodInfo.Name != "Set") 
                        continue;
                    
                    methodInfo.Invoke(configValueNode, new[] {enumValues.GetValue(i)});
                    return;
                }

                Fail("Unable to find .Set() method on ConfigValue<Enum>");
            }
            
            log.Error($"Unable to find enumeration '{value}' type for field {lowerKeyName}, setting to default value '{configValueNode}'");
        }
        
        private static void SetConfigFieldWithValue(object configValueNode, string lowerKeyName, string value)
        {
            bool fail = false;

            switch (configValueNode)
            {
            case ConfigValue<bool> boolNode:
                if (bool.TryParse(value, out bool boolValue))
                    boolNode.Set(boolValue);
                else
                    fail = true;
                break;

            case ConfigValue<double> doubleNode:
                if (double.TryParse(value, out double doubleValue))
                    doubleNode.Set(doubleValue);
                else
                    fail = true;
                break;

            case ConfigValue<int> intNode:
                if (int.TryParse(value, out int intValue))
                    intNode.Set(intValue);
                else
                    fail = true;
                break;

            case ConfigValue<string> stringNode:
                if (!string.IsNullOrEmpty(value))
                    stringNode.Set(value);
                else
                    fail = true;
                break;

            default:
                log.Warn("Unknown config field type: '{0}'", lowerKeyName);
                break;
            }

            if (fail)
                log.Warn($"Unable to set {lowerKeyName} to value '{value}', setting to default value '{configValueNode}'");
        }

        private static void ReadKeyValueIntoConfigNode(object element, string lowerKeyName, string value)
        {
            if (string.IsNullOrEmpty(lowerKeyName))
            {
                log.Warn("Malformed config key/value pair detected (empty key)");
                return;
            }
            
            // The Config object is a directed acyclic n-ary tree, where the
            // leaves are all 'ConfigValue<Type>' objects. The following will
            // get these leaf objects for the name by traversing it based on
            // the name.
            //
            // For example if the field is `render.anisotropy.value`, then it
            // will look for a `render` object, then inside that object an
            // `anisotropy` object, and then it reaches `value` that should be
            // a ConfigNode<>.
            object? configValueNode = RecursivelyFindField(element, lowerKeyName.Split("."));
            if (configValueNode == null)
            {
                log.Warn("Unable to find config entry '{0}', value will be ignored", lowerKeyName);
                return;
            }

            if (IsEnumConfigValue(configValueNode, out Type? enumType))
                SetConfigFieldWithEnum(configValueNode, lowerKeyName, value, enumType);
            else
                SetConfigFieldWithValue(configValueNode, lowerKeyName, value);
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
                        ReadKeyValueIntoConfigNode(pair.fieldValue, keyValue.KeyName, keyValue.Value);
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
    }
}
