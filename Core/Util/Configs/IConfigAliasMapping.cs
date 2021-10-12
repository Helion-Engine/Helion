using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using Helion.Util.Configs.Values;

namespace Helion.Util.Configs;

/// <summary>
/// A mapping of aliases to a config value.
/// </summary>
public interface IConfigAliasMapping : IEnumerable<(string name, IConfigValue value)>
{
    /// <summary>
    /// Tries to look up an existing config value that maps to the alias name.
    /// </summary>
    /// <param name="name">The alias name, which is not case sensitive.</param>
    /// <param name="configValue">The value found, if returning true.</param>
    /// <returns>True if an alias mapping was found, false otherwise.</returns>
    bool TryGet(string name, [NotNullWhen(true)] out IConfigValue? configValue);
}

