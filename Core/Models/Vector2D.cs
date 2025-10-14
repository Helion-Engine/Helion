using Helion.Geometry.Vectors;

namespace Helion.Models;

public struct Vector2D
{
    public Vector2D()
    {

    }

    public Vector2D(double x, double y)
    {
        X = x;
        Y = y;
    }

    public Vector2D(Vec2D v)
    {
        X = v.X;
        Y = v.Y;
    }

    public double X { get; set; }
    public double Y { get; set; }
}