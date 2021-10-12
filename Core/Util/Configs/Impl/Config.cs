using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using Helion.Util.Configs.Components;
using Helion.Util.Configs.Values;
using NLog;

namespace Helion.Util.Configs.Impl;

/// <summary>
/// A basic config file.
/// </summary>
public class Config : IConfig
{
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

    public ConfigAudio Audio { get; } = new();
    public ConfigCompat Compatibility { get; } = new();
    public ConfigConsole Console { get; } = new();
    public ConfigDeveloper Developer { get; } = new();
    public ConfigFiles Files { get; } = new();
    public ConfigGame Game { get; } = new();
    public ConfigHud Hud { get; } = new();
    public ConfigMouse Mouse { get; } = new();
    public ConfigPlayer Player { get; } = new();
    public ConfigRender Render { get; } = new();
    public ConfigWindow Window { get; } = new();
    public IConfigKeyMapping Keys => KeyMapping;
    public IConfigAliasMapping Aliases { get; }
    protected readonly ConfigKeyMapping KeyMapping = new();
    protected readonly Dictionary<string, ConfigComponent> Components = new(StringComparer.OrdinalIgnoreCase);

    public Config()
    {
        Aliases = new ConfigAliasMapping(this);
        PopulateTopLevelComponentsRecursively();
    }

    private void PopulateTopLevelComponentsRecursively()
    {
        foreach (PropertyInfo propertyInfo in GetType().GetProperties())
        {
            MethodInfo? getMethod = propertyInfo.GetMethod;
            if (getMethod?.IsPublic == null)
                continue;

            if (!getMethod.ReturnType.Name.StartsWith("Config", StringComparison.OrdinalIgnoreCase))
                continue;

            object? obj = getMethod.Invoke(this, null);
            if (obj == null)
                continue;

            PopulateComponentsRecursively(obj, propertyInfo.Name.ToLower(), 1);
        }
    }

    private void PopulateComponentsRecursively(object obj, string path, int depth)
    {
        const int RecursiveOverflowLimit = 100;

        if (depth > RecursiveOverflowLimit)
            throw new Exception($"A public instance is missing the [ConfigComponentIgnore] attribute, possibly at: {path}");

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

                Components[newPath] = new ConfigComponent(newPath, attribute, configValue);
                continue;
            }

            PopulateComponentsRecursively(childObj, newPath, depth + 1);
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
        return Keys.Changed || Components.Values.Any(c => c.Value.Changed && c.Attribute.Save);
    }

    protected void UnsetChangedFlag()
    {
        foreach (ConfigComponent configComponent in Components.Values)
            configComponent.Value.Changed = false;

        KeyMapping.ClearChanged();
    }

    [Conditional("DEBUG")]
    protected void LogChangedValues()
    {
        foreach ((string key, ConfigComponent component) in Components)
            if (component.Value.Changed && component.Attribute.Save)
                Log.Trace($"Writing changed value {key} = {component.Value.ObjectValue}");
    }

    public IEnumerator<(string, ConfigComponent)> GetEnumerator()
    {
        foreach ((string path, ConfigComponent component) in Components)
            yield return (path, component);
    }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
}

