using System;
using System.Runtime.InteropServices;

namespace Helion.Geometry.New;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly record struct Sphere(Vec3 Center, float Radius)
{
    public float Volume => 4.0f / 3.0f * MathF.PI * Radius * Radius * Radius;

    public Sphere(Vec3 center) : this(center, 1.0f)
    {
    }

    public static Sphere operator +(Sphere circle, Vec3 delta) => new(circle.Center + delta, circle.Radius);
    public static Sphere operator -(Sphere circle, Vec3 delta) => new(circle.Center - delta, circle.Radius);

    public static Vec3 Unit(float angle, float pitch)
    {
        float sinAngle = MathF.Sin(angle);
        float cosAngle = MathF.Cos(angle);
        float sinPitch = MathF.Sin(pitch);
        float cosPitch = MathF.Cos(pitch);
        return new(cosAngle * cosPitch, sinAngle * cosPitch, sinPitch);
    }

    public bool Inside(Vec3 point) => (Center - point).LengthSquared < Radius * Radius;
    public bool Outside(Vec3 point) => !Inside(point);
}
