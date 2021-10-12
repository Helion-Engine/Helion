using System;
using System.Collections.Generic;
using Helion.Geometry.Boxes;
using Helion.Geometry.Vectors;
using Helion.Util.Extensions;

namespace Helion.Geometry.Triangles;

public struct Triangle3D
{
    public Vec3D First;
    public Vec3D Second;
    public Vec3D Third;

    public Box3D Box => MakeBox();
    public IEnumerable<Vec3D> Vertices => GetVertices();

    public Triangle3D(Vec3D first, Vec3D second, Vec3D third)
    {
        First = first;
        Second = second;
        Third = third;
    }

    public static Triangle3D operator +(Triangle3D self, Vec3D other) => new(self.First + other, self.Second + other, self.Third + other);
    public static Triangle3D operator +(Triangle3D self, Vector3D other) => new(self.First + other, self.Second + other, self.Third + other);
    public static Triangle3D operator -(Triangle3D self, Vec3D other) => new(self.First - other, self.Second - other, self.Third - other);
    public static Triangle3D operator -(Triangle3D self, Vector3D other) => new(self.First - other, self.Second - other, self.Third - other);
    public static bool operator ==(Triangle3D self, Triangle3D other) => self.First == other.First && self.Second == other.Second && self.Third == other.Third;
    public static bool operator !=(Triangle3D self, Triangle3D other) => !(self == other);

    public override string ToString() => $"({First}), ({Second}), ({Third})";
    public override bool Equals(object? obj) => obj is Triangle3D tri && First == tri.First && Second == tri.Second && Third == tri.Third;
    public override int GetHashCode() => HashCode.Combine(First.GetHashCode(), Second.GetHashCode(), Third.GetHashCode());

    private Box3D MakeBox()
    {
        double minX = First.X.Min(Second.X).Min(Third.X);
        double minY = First.Y.Min(Second.Y).Min(Third.Y);
        double minZ = First.Z.Min(Second.Z).Min(Third.Z);
        double maxX = First.X.Max(Second.X).Max(Third.X);
        double maxY = First.Y.Max(Second.Y).Max(Third.Y);
        double maxZ = First.Z.Max(Second.Z).Max(Third.Z);
        return new Box3D((minX, minY, minZ), (maxX, maxY, maxZ));
    }

    private IEnumerable<Vec3D> GetVertices()
    {
        yield return First;
        yield return Second;
        yield return Third;
    }
}
