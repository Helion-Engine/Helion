using System;

namespace Helion.Util.Configs.Values;

/// <summary>
/// Metadata for a config component or value.
/// </summary>
[AttributeUsage(AttributeTargets.Field)]
public class ConfigInfoAttribute : Attribute
{
    public const string MapRestartRequiredMessage = "Map restart required for this change to take effect.";
    public const string AppRestartRequiredMessage = "App restart required for this change to take effect.";

    /// <summary>
    /// A high level description of the attribute.
    /// </summary>
    public readonly string Description;

    /// <summary>
    /// If true, saves to the config. If false, never saves.
    /// </summary>
    /// <remarks>
    /// If this is false, it means it is a transient field whereby it can
    /// be toggled via the console, but will never be saved. Upon loading
    /// the game again, it will always have the default definition.
    /// </remarks>
    public readonly bool Save;

    /// <summary>
    /// If true, serializes to the world state (save games).
    /// </summary>
    public readonly bool Serialize;

    // If true this option is serialized for demos.
    public readonly bool Demo;

    // If the map needs to be restarted to take effect.
    public readonly bool MapRestartRequired;

    // If the application needs to be restarted to take effect.
    public readonly bool RestartRequired;

    // If the computer needs to be restarted to take effect.
    public readonly bool ComputerRestartRequired;

    public readonly bool Legacy;

    public ConfigInfoAttribute(string description, bool save = true, bool serialize = false, bool demo = false, bool mapRestartRequired = false, 
        bool restartRequired = false, bool legacy = false, bool computerRestartRequired = false)
    {
        Description = description;
        Save = save;
        Serialize = serialize;
        Demo = demo;
        MapRestartRequired = mapRestartRequired;
        RestartRequired = restartRequired;
        Legacy = legacy;
        ComputerRestartRequired = computerRestartRequired;
    }

    public bool GetSetWarningString(out string message)
    {
        message = string.Empty;
        if (MapRestartRequired)
            message = MapRestartRequiredMessage;
        if (RestartRequired)
            message = AppRestartRequiredMessage;
        return message.Length > 0;
    }
}
