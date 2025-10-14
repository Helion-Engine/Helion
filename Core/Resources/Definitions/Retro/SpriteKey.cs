using Helion.Util.Extensions;
using System;

namespace Helion.Resources.Definitions.Retro;

readonly struct SpriteKey(string spriteName, string brightMapName) : IEquatable<SpriteKey>
{
    public readonly string SpriteName = spriteName;
    public readonly string BrightMapName = brightMapName;

    public bool Equals(SpriteKey other) => SpriteName == other.SpriteName && BrightMapName.EqualsIgnoreCase(other.BrightMapName);
    public override int GetHashCode() => HashCode.Combine(StringComparer.OrdinalIgnoreCase.GetHashCode(SpriteName), StringComparer.OrdinalIgnoreCase.GetHashCode(BrightMapName));
    public override bool Equals(object? obj) => obj is SpriteKey key && Equals(key);
}
