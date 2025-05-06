using System;
using Helion.Util.Extensions;

namespace Helion.Resources.Definitions.ZDoom
{
    public struct BrightmapLookupCacheKey(string textureName, ResourceNamespace textureNamespace) : IEquatable<BrightmapLookupCacheKey>
    {
        public string TextureName = textureName;
        public ResourceNamespace TextureNamespace = textureNamespace;

        public override readonly int GetHashCode() => HashCode.Combine(TextureName, TextureNamespace);
        public readonly bool Equals(BrightmapLookupCacheKey other)
            => TextureNamespace == other.TextureNamespace && TextureName.EqualsIgnoreCase(other.TextureName);
        public readonly override bool Equals(object? obj) => obj is not null && obj is BrightmapLookupCacheKey v && Equals(v);
        public static bool operator ==(BrightmapLookupCacheKey left, BrightmapLookupCacheKey right) => left.Equals(right);
        public static bool operator !=(BrightmapLookupCacheKey left, BrightmapLookupCacheKey right) => !(left == right);
    }
}