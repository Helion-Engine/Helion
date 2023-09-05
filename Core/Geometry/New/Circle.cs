using System;
using System.Runtime.InteropServices;

namespace Helion.Geometry.New;

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly record struct Circle(Vec2 Center, float Radius)
{
    public float Area => MathF.PI * Radius * Radius;

    public Circle(Vec2 center) : this(center, 1.0f)
    {
    }

    public static Circle operator +(Circle circle, Vec2 delta) => new(circle.Center + delta, circle.Radius);
    public static Circle operator -(Circle circle, Vec2 delta) => new(circle.Center - delta, circle.Radius);

    public static Vec2 Unit(float radians) => new(MathF.Cos(radians), MathF.Sin(radians));

    public float Angle(Vec2 other) => MathF.Atan2(other.Y - Center.Y, other.X - Center.X);
    public bool Inside(Vec2 point) => (Center - point).LengthSquared < Radius * Radius;
    public bool Outside(Vec2 point) => !Inside(point);
}
