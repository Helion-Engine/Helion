using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Helion.Models;
using Helion.Util.Configs.Components;
using Helion.Util.Configs.Values;

namespace Helion.Util.Configs;

/// <summary>
/// A configuration file which contains dynamic runtime information.
/// </summary>
public interface IConfig
{
    ConfigAudio Audio { get; }
    ConfigCompat Compatibility { get; }
    ConfigConsole Console { get; }
    ConfigDeveloper Developer { get; }
    ConfigFiles Files { get; }
    ConfigGame Game { get; }
    ConfigHud Hud { get; }
    ConfigMouse Mouse { get; }
    ConfigPlayer Player { get; }
    ConfigRender Render { get; }
    ConfigWindow Window { get; }
    ConfigDemo Demo { get; }
    IConfigKeyMapping Keys { get; }
    IConfigAliasMapping Aliases { get; }

    /// <summary>
    /// Tries to get the config component for the lookup name.
    /// </summary>
    /// <param name="name">The lookup name, is not case sensitive. An example
    /// would be "mouse.pitch".</param>
    /// <param name="component">The component which maps to this if this function
    /// return true, or null if this function return false.</param>
    /// <returns>True on successful finding, false if not.</returns>
    bool TryGetComponent(string name, [NotNullWhen(true)] out ConfigComponent? component);

    /// <summary>
    /// Applies changes that have been queued up to all such fields.
    /// </summary>
    /// <param name="setFlags">The flags that signify what config values
    /// should be applied, provided they match the flag bits provided.</param>
    void ApplyQueuedChanges(ConfigSetFlags setFlags);

    Dictionary<string, ConfigComponent> GetComponents();
    void ApplyConfiguration(IList<ConfigValueModel> configValues);
}
