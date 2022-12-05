using System;

namespace Helion.GeometryNew;

public readonly record struct CircleF(Vec2f Center, float Radius)
{
    public float Area => MathF.PI * Radius * Radius;

    public CircleF(Vec2f center) : this(center, 1.0f)
    {
    }

    public static CircleF operator +(CircleF circle, Vec2f delta) => new(circle.Center + delta, circle.Radius);
    public static CircleF operator -(CircleF circle, Vec2f delta) => new(circle.Center - delta, circle.Radius);

    public static Vec2f Unit(float radians) => new(MathF.Cos(radians), MathF.Sin(radians));

    public float Angle(Vec2f other) => MathF.Atan2(other.Y - Center.Y, other.X - Center.X);
    public bool Inside(Vec2f point) => (Center - point).LengthSquared < Radius * Radius;
    public bool Outside(Vec2f point) => !Inside(point);
}

public readonly record struct CircleD(Vec2d Center, double Radius)
{
    public double Area => Math.PI * Radius * Radius;

    public CircleD(Vec2d center) : this(center, 1.0)
    {
    }

    public static CircleD operator +(CircleD circle, Vec2d delta) => new(circle.Center + delta, circle.Radius);
    public static CircleD operator -(CircleD circle, Vec2d delta) => new(circle.Center - delta, circle.Radius);

    public static Vec2d Unit(double radians) => new(Math.Cos(radians), Math.Sin(radians));

    public double Angle(Vec2d other) => Math.Atan2(other.Y - Center.Y, other.X - Center.X);
    public bool Inside(Vec2d point) => (Center - point).LengthSquared < Radius * Radius;
    public bool Outside(Vec2d point) => !Inside(point);
}
