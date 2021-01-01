using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Helion.Input;
using Helion.Util.Configs.Tree;
using Helion.Util.Extensions;
using IniParser;
using IniParser.Model;

namespace Helion.Util.Configs
{
    public partial class Config
    {
        private void ReadConfig(string path)
        {
            if (!File.Exists(path))
            {
                Log.Info("Config file not found, generating new config file and using default values");
                m_newConfig = true;
                return;
            }

            try
            {
                FileIniDataParser parser = new();
                IniData data = parser.ReadFile(path);
                ReadEngineAssignments(data.Sections[EngineSectionName]);
                ReadKeys(data.Sections[KeysSectionName]);
            }
            catch (Exception e)
            {
                Log.Error($"Unable to parse config file: {e.Message}");
            }
        }

        private void ReadEngineAssignments(KeyDataCollection engineData)
        {
            foreach (KeyData data in engineData)
            {
                dynamic? configValue = FindConfigValueObject(this, data.KeyName);
                if (configValue == null)
                {
                    Log.Warn("Unable to find config key name '{0}', ignoring value", data.KeyName);
                    continue;
                }

                try
                {
                    configValue.Set(data.Value);
                }
                catch
                {
                    Log.Error("Unable to set config path '{0}' with value '{1}'", data.KeyName, data.Value);
                }
            }
        }

        private object? FindConfigValueObject(object component, string path)
        {
            const BindingFlags bindingFlags = BindingFlags.Public | BindingFlags.Instance | BindingFlags.IgnoreCase;

            List<string> tokens = path.Split(ConfigTree.Separator)
                                      .Where(s => !s.Empty())
                                      .Select(s => s.ToLower())
                                      .ToList();
            if (tokens.Empty())
                return null;

            // Keep going through each field. For example, if we want to find
            // "render.showfps", then look for the field named "render" in the
            // config, and then look for "showfps" under the 'render' object we
            // got from above. Eventually in a well formed config, we find the
            // object of interest.
            for (int i = 0; i < tokens.Count; i++)
            {
                FieldInfo? fieldInfo = component.GetType().GetField(tokens[i], bindingFlags);
                if (fieldInfo == null)
                    return null;

                if (i == tokens.Count - 1)
                {
                    // Now that we've reached the final piece in the path (as
                    // in, "render.showfps" would result in the field object
                    // named 'showfps' under the 'render' object), we can grab
                    // the object. It has to be of the proper type as well for
                    // one final sanity check.
                    object? targetObj = fieldInfo.GetValue(component);
                    return targetObj != null && IsConfigValueType(targetObj.GetType()) ? targetObj : null;
                }

                // Since we're not at a leaf, our path still has 2+ elements. This
                // means we must have found a parent node. If not, something went
                // very wrong (a developer forgot to apply an attribute to a field
                // or the user happened to enter a field name).
                if (!HasConfigAttribute(fieldInfo))
                    return null;

                object? childComponent = fieldInfo.GetValue(component);
                if (childComponent == null)
                    return null;

                component = childComponent;
            }

            // If this is ever reached, it means the config has a path to a
            // parent node.
            return null;
        }

        private void ReadKeys(KeyDataCollection keyData)
        {
            // We want to map the lower case name to the enumeration value.
            Dictionary<string, InputKey> keys = new();
            foreach (object enumValue in Enum.GetValues(typeof(InputKey)))
            {
                InputKey inputKey = (InputKey)enumValue;
                keys[inputKey.ToString().ToLower()] = inputKey;
            }

            // Now take all the keys in their lower named form, and assign them
            // to our known keys (provided it is not the `Unknown` enum value).
            foreach (KeyData data in keyData)
                if (keys.TryGetValue(data.KeyName.ToLower(), out InputKey key) && key != InputKey.Unknown)
                    Keys[key] = data.Value;
        }
    }
}
