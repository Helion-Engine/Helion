using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;
using Helion.Util.Configs.Components;
using Helion.Util.Configs.Values;
using Helion.Util.Extensions;
using Helion.Window.Input;
using IniParser;
using IniParser.Model;
using NLog;

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

                list = new();
                list.Add(item.Command);
                commandMapping[item.Key] = list;
            }

            foreach ((Key key, IEnumerable<string> commands) in commandMapping)
                section[key.ToString()] = $"[{commands.Where(x => x.Trim().Length > 0).Select(cmd => $"\"{cmd}\"").Join(", ")}]";

            return true;
        }
    }

    private void ReadConfigFrom(string path, bool addDefaultsIfNew)
    {
        Log.Info($"Reading config from {path}");

        if (!File.Exists(path))
        {
            Log.Info($"Config file not found, will generate new config file at {path}");
            m_noFileExistedWhenRead = true;

            KeyMapping.AddDefaultsIfMissing();
            return;
        }

        try
        {
            FileIniDataParser parser = new();
            IniData iniData = parser.ReadFile(path);

            ReadEngineValues(iniData);
            ReadKeyValues(iniData);
            KeyMapping.AddDefaultsIfMissing();
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

        void ReadKeyValues(IniData iniData)
        {
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

                List<string>? commandArray = JsonSerializer.Deserialize<List<string>>(keyData.Value).Where(x => x.Trim().Length > 0).ToList();
                if (commandArray == null)
                {
                    Log.Warn($"Unable to parse parse config key line: {keyData.KeyName} = {keyData.Value}");
                    continue;
                }

                // If the user clears a key bind entirely then the command array will be empty.
                // Keep this so that the defaults are not added.
                if (commandArray.Count == 0)
                {
                    Keys.AddEmpty(key);
                    continue;
                }

                foreach (var command in commandArray)
                    Keys.Add(key, command);
            }
        }
    }
}
