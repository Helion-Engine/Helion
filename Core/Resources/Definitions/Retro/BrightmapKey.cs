using Helion.Util.Extensions;
using System;

namespace Helion.Resources.Definitions.Retro;

public readonly struct BrightmapKey(ResourceNamespace ns, string name) : IEquatable<BrightmapKey>
{
    public readonly ResourceNamespace Namespace = ns;
    public readonly string Name = name;

    public bool Equals(BrightmapKey other) => Namespace == other.Namespace && Name.EqualsIgnoreCase(other.Name);
    public override int GetHashCode() => HashCode.Combine(Namespace, StringComparer.OrdinalIgnoreCase.GetHashCode(Name));
    public override bool Equals(object? obj) => obj is not null && obj is BrightmapKey key && Equals(key);
    public override string ToString() => $"{Namespace}:{Name}";
}
