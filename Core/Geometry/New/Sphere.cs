using System;

namespace Helion.Geometry.New;

public readonly record struct SphereF(Vec3f Center, float Radius)
{
    public float Volume => 4.0f / 3.0f * MathF.PI * Radius * Radius * Radius;

    public SphereF(Vec3f center) : this(center, 1.0f)
    {
    }

    public static SphereF operator +(SphereF circle, Vec3f delta) => new(circle.Center + delta, circle.Radius);
    public static SphereF operator -(SphereF circle, Vec3f delta) => new(circle.Center - delta, circle.Radius);

    public static Vec3f Unit(float angle, float pitch)
    {
        float sinAngle = MathF.Sin(angle);
        float cosAngle = MathF.Cos(angle);
        float sinPitch = MathF.Sin(pitch);
        float cosPitch = MathF.Cos(pitch);
        return new(cosAngle * cosPitch, sinAngle * cosPitch, sinPitch);
    }

    public bool Inside(Vec3f point) => (Center - point).LengthSquared < Radius * Radius;
    public bool Outside(Vec3f point) => !Inside(point);
}

public readonly record struct SphereD(Vec3d Center, double Radius)
{
    public double Volume => 4.0 / 3.0 * Math.PI * Radius * Radius * Radius;

    public SphereD(Vec3d center) : this(center, 1.0)
    {
    }

    public static SphereD operator +(SphereD circle, Vec3d delta) => new(circle.Center + delta, circle.Radius);
    public static SphereD operator -(SphereD circle, Vec3d delta) => new(circle.Center - delta, circle.Radius);

    public static Vec3d Unit(double angle, double pitch)
    {
        double sinAngle = Math.Sin(angle);
        double cosAngle = Math.Cos(angle);
        double sinPitch = Math.Sin(pitch);
        double cosPitch = Math.Cos(pitch);
        return new(cosAngle * cosPitch, sinAngle * cosPitch, sinPitch);
    }

    public bool Inside(Vec3d point) => (Center - point).LengthSquared < Radius * Radius;
    public bool Outside(Vec3d point) => !Inside(point);
}
