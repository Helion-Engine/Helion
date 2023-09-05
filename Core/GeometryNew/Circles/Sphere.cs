using System;
using Helion.GeometryNew.Vectors;

namespace Helion.GeometryNew.Circles;

public readonly record struct Sphere(Vec3 Origin, float Radius)
{
    public Sphere(float radius) : this((0, 0, 0), radius)
    {
    }
    
    public Sphere(Vec3 origin, Vec3 edge) : this(origin, origin.Distance(edge))
    {
    }
    
    public static Vec3 UnitPoint(float angle, float pitch)
    {
        float sinAngle = MathF.Sin(angle);
        float cosAngle = MathF.Cos(angle);
        float sinPitch = MathF.Sin(pitch);
        float cosPitch = MathF.Cos(pitch);
        return new(cosAngle * cosPitch, sinAngle * cosPitch, sinPitch);
    }
    
    public Vec3 Point(float angle, float pitch) => (UnitPoint(angle, pitch) * Radius) + Origin;
    public bool Contains(Vec3 point) => Origin.Distance(point) < Radius;

    public bool Intersects(Sphere sphere)
    {
        // Source: https://www.petercollingridge.co.uk/tutorials/computational-geometry/circle-circle-intersections/
        // Should be similar.
        float originDist = sphere.Radius + Radius;
        return (sphere.Origin - Origin).LengthSquared < originDist * originDist;
    }
}