using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using Helion.Input;
using Helion.Util.ConfigsNew.Components;
using Helion.Util.ConfigsNew.Values;
using Helion.Util.Extensions;
using IniParser;
using IniParser.Model;
using NLog;

namespace Helion.Util.ConfigsNew
{
    public record ConfigComponent(string Path, ConfigInfoAttribute Attribute, IConfigValue Value);

    /// <summary>
    /// A configuration file that contains various settings, which are also
    /// accessible for enumeration.
    /// </summary>
    public class ConfigNew : IEnumerable<ConfigComponent>
    {
        private const string EngineSectionName = "engine";
        private const string KeysSectionName = "keys";
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        // Public fields in this class recursed upon by reflection to find config values.
        public readonly ConfigAudio Audio = new();
        public readonly ConfigCompat Compatibility = new();
        public readonly ConfigConsole Console = new();
        public readonly ConfigDeveloper Developer = new();
        public readonly ConfigFiles Files = new();
        public readonly ConfigGame Game = new();
        public readonly ConfigHud Hud = new();
        public readonly ConfigMouse Mouse = new();
        public readonly ConfigPlayer Player = new();
        public readonly ConfigRender Render = new();
        public readonly ConfigWindow Window = new();
        
        // Keys are handled specially since they are a multi-bi-directional mapping.
        public readonly ConfigKeyMapping Keys = new();

        // To be compatible with other ports, this is a list of all the variables
        // with other known names.
        public readonly ConfigVariableAliasMapping VariableAliasMapping; 

        private readonly Dictionary<string, ConfigComponent> m_components = new(StringComparer.OrdinalIgnoreCase);
        private bool m_noFileExistedWhenRead;
        
        public bool Changed => Keys.Changed || m_components.Values.Any(c => c.Value.Changed);
        
        public ConfigNew()
        {
            PopulateComponentsRecursively(this, "");
            VariableAliasMapping = new ConfigVariableAliasMapping(this);
        }

        public ConfigNew(string filePath) : this()
        {
            Log.Info($"Reading config from {filePath}");
            ReadConfigFrom(filePath);

            // If we read things in, we're going to very likely change things from
            // their default state. This is okay and should not be considered as
            // "changed".
            UnsetChangedFlag();
        }

        private void UnsetChangedFlag()
        {
            foreach (ConfigComponent configComponent in m_components.Values)
                configComponent.Value.Changed = false;

            Keys.Changed = false;
        }

        public void ApplyQueuedChanges(ConfigSetFlags setFlags)
        {
            Log.Trace("Applying queued config changes for {Flags}", setFlags);
            
            foreach (ConfigComponent component in m_components.Values)
                component.Value.ApplyQueuedChange(setFlags);
        }

        public bool Write(string filePath, bool alwaysWrite = false)
        {
            // If a file existed, and it didn't change after we loaded it, then
            // we don't want to force a pointless write. However if desired, this
            // can be forcefully overridden.
            if (!m_noFileExistedWhenRead && !Changed && !alwaysWrite)
                return true;
            
            try
            {
                FileIniDataParser parser = new();
                IniData iniData = new();

                bool success = true;
                success &= AddEngineFields(iniData);
                success &= AddKeyFields(iniData);

                if (success)
                    parser.WriteFile(filePath, iniData);

                Log.Info($"Wrote config file to {filePath}");
                return success;
            }
            catch (Exception e)
            {
                Log.Error($"Unable to write config file to {filePath}");
                Log.Debug($"Config write failure reason: {e.Message}");
                return false;
            }

            bool AddEngineFields(IniData data)
            {
                if (!data.Sections.AddSection(EngineSectionName))
                {
                    Log.Error("Failed to add engine section header when writing config");
                    return false;
                }

                KeyDataCollection section = data[EngineSectionName];
                foreach ((string entryPath, _, IConfigValue configValue) in m_components.Values)
                    section[entryPath] = configValue.ToString();

                return true;
            }
            
            bool AddKeyFields(IniData data)
            {
                if (!data.Sections.AddSection(KeysSectionName))
                {
                    Log.Error("Failed to add key section header when writing config");
                    return false;
                }

                KeyDataCollection section = data[KeysSectionName];
                foreach ((Key key, IEnumerable<string> commands) in Keys)
                    section[key.ToString()] = commands.Select(cmd => $"\"{cmd}\"").Join(", ");

                return true;
            }
        }

        private void PopulateComponentsRecursively(object obj, string path)
        {
            foreach (FieldInfo fieldInfo in obj.GetType().GetFields())
            {
                if (!fieldInfo.IsPublic)
                    continue;

                object? childObj = fieldInfo.GetValue(obj);
                if (childObj == null)
                    throw new Exception($"Missing config object instantiation {fieldInfo.Name} at '{path}'");

                string newPath = (path != "" ? $"{path}." : "") + fieldInfo.Name.ToLower();

                if (childObj is IConfigValue configValue)
                {
                    ConfigInfoAttribute? attribute = fieldInfo.GetCustomAttribute<ConfigInfoAttribute>();
                    if (attribute == null)
                        throw new Exception($"Config field at '{newPath}' is missing attribute {nameof(ConfigInfoAttribute)}");
                    
                    m_components[newPath] = new ConfigComponent(newPath, attribute, configValue);
                    continue;
                }

                PopulateComponentsRecursively(childObj, newPath);
            }
        }

        private void ReadConfigFrom(string path)
        {
            Log.Debug("Reading config file from {Path}", path);
            
            if (!File.Exists(path))
            {
                Log.Info($"Config file not found, will generate new config file at {path}");
                m_noFileExistedWhenRead = true;
                return;
            }

            try
            {
                FileIniDataParser parser = new();
                IniData iniData = parser.ReadFile(path);
                foreach (KeyData keyData in iniData.Sections[EngineSectionName])
                {
                    string identifier = keyData.KeyName.ToLower();

                    if (!m_components.TryGetValue(identifier, out ConfigComponent? configComponent))
                    {
                        Log.Warn($"Unable to find config mapping for {identifier}, value will be lost on saving");
                        continue;
                    }

                    if (!configComponent.Attribute.Save)
                    {
                        Log.Warn($"Config mapping {identifier} is transient (and ignored), value will be excluded on saving");
                        continue;
                    }

                    configComponent.Value.Set(keyData.Value);
                }
            }
            catch (Exception e)
            {
                Log.Error($"Unable to parse config file: {e.Message}");
            }
        }

        public IEnumerator<ConfigComponent> GetEnumerator() => m_components.Values.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
