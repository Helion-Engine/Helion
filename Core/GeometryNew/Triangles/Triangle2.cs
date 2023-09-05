using System;
using Helion.GeometryNew.Vectors;

namespace Helion.GeometryNew.Triangles;

public readonly record struct Triangle2(Vec2 First, Vec2 Second, Vec2 Third)
{
    public static implicit operator Triangle2(ValueTuple<Vec2, Vec2, Vec2> tuple)
    {
        return new(tuple.Item1, tuple.Item2, tuple.Item3);
    }

    public void Deconstruct(out Vec2 first, out Vec2 second, out Vec2 third)
    {
        first = First;
        second = Second;
        third = Third;
    }
}