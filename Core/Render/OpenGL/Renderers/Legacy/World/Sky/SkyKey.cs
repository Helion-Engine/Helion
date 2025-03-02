using Helion.Geometry.Vectors;
using Helion.Resources.Definitions;
using System;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Sky;

readonly struct SkyKey(int id, SkyOptions options, Vec2F offset) : IEquatable<SkyKey>
{
    public readonly int Id = id;
    public readonly SkyOptions Options = options;
    public readonly Vec2F Offset = offset;

    public override int GetHashCode()
    {
        return HashCode.Combine(Id, (int)Options, Offset.X, Offset.Y);
    }

    public bool Equals(SkyKey other)
    {
        return Id == other.Id && Options == other.Options &&
            Offset.X == other.Offset.X && Offset.Y == other.Offset.Y;
    }

    public override bool Equals(object? obj)
    {
        return obj is SkyKey key && Equals(key);
    }
}
