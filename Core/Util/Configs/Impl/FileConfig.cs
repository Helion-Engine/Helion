using Helion.Util.Configs.Components;
using Helion.Util.Configs.Values;
using Helion.Util.Extensions;
using Helion.Util.Loggers;
using Helion.Window.Input;
using IniParser;
using IniParser.Model;
using NLog;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Helion.Util.Configs.Impl;

/// <summary>
/// A configuration file that contains various settings, which are also
/// accessible for enumeration.
/// </summary>
public class FileConfig : Config
{
    const string IniFile = "config.ini";
    public const string EngineSectionName = "engine";
    public const string KeysSectionName = "keys";
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    private readonly string m_filePath;
    private bool m_noFileExistedWhenRead;

    public FileConfig(string filePath, bool addDefaultsIfNew)
    {
        m_filePath = filePath;

        ReadConfigFrom(filePath, addDefaultsIfNew);
        MigrateValues();

        // If we read things in, we're going to very likely change things from
        // their default state. This is okay and should not be considered as
        // "changed".
        UnsetChangedFlag();
    }

    private void MigrateValues()
    {
        if (Hud.MoveBob.Value != 1.0)
        {
            Hud.ViewBob.Set(Hud.MoveBob.Value);
            Hud.WeaponBob.Set(Hud.MoveBob.Value);
            Hud.MoveBob.Set(1.0);
        }
    }

    public static string GetDefaultConfigPath()
    {
        if (File.Exists(IniFile))
            return IniFile;

        // On Linux, default to "$XDG_CONFIG_HOME/helion/config.ini"
        if (OperatingSystem.IsLinux())
        {
            var xdgConfigHome = Environment.GetEnvironmentVariable("XDG_CONFIG_HOME");

            if (!string.IsNullOrWhiteSpace(xdgConfigHome))
                return $"{xdgConfigHome}/helion/{IniFile}";

            // Fallback to "$HOME/.config/helion/config.ini"
            var home = Environment.GetEnvironmentVariable("HOME");

            if (!string.IsNullOrWhiteSpace(home))
                return $"{home}/.config/helion/{IniFile}";
        }

        return IniFile;
    }

    /// <summary>
    /// Will write the config to the path provided, but will refuse to write
    /// if and only if: alwaysWrite is false, and no changes happened, and
    /// it's a new file that didn't exist before.
    /// </summary>
    /// <param name="filePath">The path to write. If null, will use the path
    /// that was present when opening, but this allows the caller to use a
    /// different path if desired.</param>
    /// <param name="alwaysWrite">If true, will always write no matter what.
    /// </param>
    /// <returns>True on success (or no write due to no changes if it's an
    /// existing file and alwaysWrite is false), false on failure.</returns>
    public bool Write(string? filePath = null, bool alwaysWrite = false)
    {
        filePath ??= m_filePath;

        Log.Debug($"Writing config file to {filePath} (always write = {alwaysWrite})");

        // If a file existed, and it didn't change after we loaded it, then
        // we don't want to force a pointless write. However if desired, this
        // can be forcefully overridden.
        bool changed = CheckAnyBindingsChanged();
        if (!m_noFileExistedWhenRead && !changed && !alwaysWrite)
        {
            Log.Trace($"Not writing config to {filePath} since nothing changed");
            return true;
        }

        if (changed)
            LogChangedValues();

        try
        {
            FileIniDataParser parser = new();
            IniData iniData = new();

            bool success = true;
            success &= AddEngineFields(iniData);
            success &= AddKeyFields(iniData);

            if (success)
            {
                // Ensure that all directories in the config path exist
                var dirPath = Path.GetDirectoryName(Path.GetFullPath(filePath));
                if (dirPath != null)
                    Directory.CreateDirectory(dirPath);

                parser.WriteFile(filePath, iniData);
            }

            KeyMapping.ClearChanged();
            HelionLog.Info($"Wrote config file to {filePath}");
            return success;
        }
        catch (Exception e)
        {
            HelionLog.Error($"Unable to write config file to {filePath}");
            HelionLog.Debug($"Config write failure reason: {e.Message}");
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
            foreach ((string path, ConfigInfoAttribute attr, IConfigValue value) in Components.Values)
                if (attr.Save && !attr.Legacy && value.WriteToConfig)
                    section[path] = value.ToString();

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
            Dictionary<Key, List<string>> commandMapping = new();
            foreach (var item in Keys.GetKeyMapping())
            {
                if (commandMapping.TryGetValue(item.Key, out var list))
                {
                    list.Add(item.Command);
                    continue;
                }

                list = [item.Command];
                commandMapping[item.Key] = list;
            }

            foreach ((Key key, IEnumerable<string> commands) in commandMapping)
                section[key.ToString()] = $"[{commands.Where(x => x.Trim().Length > 0).Select(cmd => $"\"{cmd}\"").Join(", ")}]";

            return true;
        }
    }

    private void ReadConfigFrom(string path, bool addDefaultsIfNew)
    {
        HelionLog.Info($"Reading config from {path}");

        if (!File.Exists(path))
        {
            HelionLog.Info($"Config file not found, will generate new config file at {path}");
            m_noFileExistedWhenRead = true;

            KeyMapping.SetInitialDefaultKeyBindings();
            return;
        }

        try
        {
            FileIniDataParser parser = new();
            IniData iniData = parser.ReadFile(path);

            ReadEngineValues(iniData);
            bool keyValuesNeedSave = !ReadKeyValues(iniData);

            // Allow the user to un-bind as many keys as they want, but make sure they
            // at least have a way to get back into the menus if their bindings aren't working.
            KeyMapping.EnsureMenuKey();
            if (KeyMapping.Changed || keyValuesNeedSave)
            {
                Write(path, true);
            }
        }
        catch (Exception e)
        {
            Log.Error($"Unable to parse config file: {e.Message}");
        }

        void ReadEngineValues(IniData iniData)
        {
            foreach (KeyData keyData in iniData.Sections[EngineSectionName])
            {
                string identifier = keyData.KeyName.ToLower();

                if (!Components.TryGetValue(identifier, out ConfigComponent? configComponent))
                {
                    Log.Warn($"Unable to find config mapping for {identifier}, value will be lost on saving");
                    continue;
                }

                if (!configComponent.Attribute.Save)
                {
                    Log.Warn($"Config mapping {identifier} is transient (and ignored), value will be excluded on saving");
                    continue;
                }

                var status = configComponent.Value.Set(keyData.Value);
                if (status != ConfigSetResult.Set && status != ConfigSetResult.Unchanged)
                    Log.Error($"Unable to parse and set {identifier} with {keyData.Value} (reason: {status})");
            }
        }

        bool ReadKeyValues(IniData iniData)
        {
            bool noBadMappings = true;
            Dictionary<string, Key> nameToKey = new(StringComparer.OrdinalIgnoreCase);
            foreach (Key key in Enum.GetValues<Key>())
                nameToKey[key.ToString()] = key;

            foreach (KeyData keyData in iniData.Sections[KeysSectionName])
            {
                if (!nameToKey.TryGetValue(keyData.KeyName, out Key key))
                {
                    Log.Warn($"Unable to parse config key type: {keyData.KeyName} (assigned: with {keyData.Value})");
                    continue;
                }

                var commandArray = GetCommandArray(keyData);
                if (commandArray == null)
                {
                    Log.Warn($"Unable to parse parse config key line: {keyData.KeyName} = {keyData.Value}");
                    continue;
                }

                if (commandArray.Length == 0)
                {
                    Log.Warn($"Empty mappings for {keyData.KeyName}");
                    noBadMappings = false;
                }

                foreach (var command in commandArray)
                    noBadMappings &= Keys.Add(key, command);
            }

            KeyMapping.ClearChanged();
            // This might be false if we couldn't bind a key because it was "empty" or duplicated.
            return noBadMappings;
        }
    }

    private static string[]? GetCommandArray(KeyData keyData)
    {
        var deserialized = JsonSerializer.Deserialize<string[]>(keyData.Value);
        if (deserialized == null)
            return null;

        return deserialized.Where(x => x.Trim().Length > 0).ToArray();
    }
}
