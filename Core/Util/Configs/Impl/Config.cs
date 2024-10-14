using Helion.Models;
using Helion.Util.Configs.Components;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using NLog;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;

namespace Helion.Util.Configs.Impl;

/// <summary>
/// A basic config file.
/// </summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)]
public class Config : ConfigElement<Config>, IConfig
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public ConfigAudio Audio { get; } = new();
    public ConfigCompat Compatibility { get; } = new();
    public ConfigConsole Console { get; } = new();
    public ConfigController Controller { get; } = new();
    public ConfigDeveloper Developer { get; } = new();
    public ConfigFiles Files { get; } = new();
    public ConfigGame Game { get; } = new();
    public ConfigHud Hud { get; } = new();
    public ConfigMouse Mouse { get; } = new();
    public ConfigPlayer Player { get; } = new();
    public ConfigRender Render { get; } = new();
    public ConfigWindow Window { get; } = new();
    public ConfigDemo Demo { get; } = new();
    public ConfigSlowTick SlowTick { get; } = new();
    public IConfigKeyMapping Keys => KeyMapping;
    public IConfigAliasMapping Aliases { get; }
    protected readonly ConfigKeyMapping KeyMapping = new();
    protected readonly Dictionary<string, ConfigComponent> Components = new(StringComparer.OrdinalIgnoreCase);

    public static string FindSplitValue(string value)
    {
        if (value.Contains(','))
            return ",";
        if (value.Contains('x'))
            return "x";
        if (value.Contains('X'))
            return "X";
        return " ";
    }

    public Config()
    {
        Aliases = new ConfigAliasMapping(this);
        PopulateTopLevelComponentsRecursively();
    }

    private void PopulateTopLevelComponentsRecursively()
    {
        foreach (PropertyInfo propertyInfo in typeof(Config).GetProperties())
        {
            MethodInfo? getMethod = propertyInfo.GetMethod;
            if (getMethod?.IsPublic == null)
                continue;

            if (!getMethod.ReturnType.Name.StartsWith("Config", StringComparison.OrdinalIgnoreCase))
                continue;

            object? obj = getMethod.Invoke(this, null);
            if (obj == null)
                continue;

            (obj as IConfigElement)?.PopulateComponentsRecursively(Components, propertyInfo.Name.ToLower(), 1);
        }
    }

    public bool TryGetComponent(string name, [NotNullWhen(true)] out ConfigComponent? component)
    {
        return Components.TryGetValue(name, out component);
    }

    public void ApplyQueuedChanges(ConfigSetFlags setFlags)
    {
        Log.Trace("Applying queued config changes for {Flags}", setFlags);

        foreach (ConfigComponent component in Components.Values)
            component.Value.ApplyQueuedChange(setFlags);
    }

    protected bool CheckAnyBindingsChanged()
    {
        return Keys.Changed || Components.Values.Any(c => c.Value.Changed && c.Attribute.Save && c.Value.WriteToConfig);
    }

    protected void UnsetChangedFlag()
    {
        foreach (ConfigComponent configComponent in Components.Values)
            configComponent.Value.Changed = false;
    }

    [Conditional("DEBUG")]
    protected void LogChangedValues()
    {
        foreach ((string key, ConfigComponent component) in Components)
            if (component.Value.Changed && component.Attribute.Save && component.Value.WriteToConfig)
                Log.Trace($"Writing changed value {key} = {component.Value.ObjectValue}");
    }

    public IEnumerator<(string, ConfigComponent)> GetEnumerator()
    {
        foreach ((string path, ConfigComponent component) in Components)
            yield return (path, component);
    }

    public Dictionary<string, ConfigComponent> GetComponents() => Components;

    public void ApplyConfiguration(IList<ConfigValueModel> configValues, bool writeToConfig = true)
    {
        foreach (var configModel in configValues)
        {
            if (!Components.TryGetValue(configModel.Key, out ConfigComponent? component))
            {
                Log.Error($"Invalid configuration path: {configModel.Key}");
                continue;
            }

            if (component.Value.Set(configModel.Value, writeToConfig) == ConfigSetResult.NotSetByBadConversion)
                Log.Error($"Bad configuration value '{configModel.Value}' for '{configModel.Key}'.");
        }
    }

    /// <summary>
    /// Gets information about all fields exposed by this configuration object
    /// </summary>
    public List<(IConfigValue, OptionMenuAttribute, ConfigInfoAttribute)> GetAllConfigFields()
    {
        List<(IConfigValue, OptionMenuAttribute, ConfigInfoAttribute)> fields = new();

        foreach (PropertyInfo propertyInfo in typeof(Config).GetProperties())
        {
            MethodInfo? getMethod = propertyInfo.GetMethod;
            if (getMethod?.IsPublic == null)
                continue;

            if (!getMethod.ReturnType.Name.StartsWith("Config", StringComparison.OrdinalIgnoreCase))
                continue;

            object? childObj = getMethod.Invoke(this, null);
            if (childObj == null)
                continue;

            (childObj as IConfigElement)?.RecursivelyGetConfigFieldsOrThrow(fields, depth: 1);
        }

        return fields;
    }
}
