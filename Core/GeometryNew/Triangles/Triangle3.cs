using System;
using Helion.GeometryNew.Vectors;

namespace Helion.GeometryNew.Triangles;

public readonly record struct Triangle3(Vec3 First, Vec3 Second, Vec3 Third)
{
    public static implicit operator Triangle3(ValueTuple<Vec3, Vec3, Vec3> tuple)
    {
        return new(tuple.Item1, tuple.Item2, tuple.Item3);
    }

    public void Deconstruct(out Vec3 first, out Vec3 second, out Vec3 third)
    {
        first = First;
        second = Second;
        third = Third;
    }
}