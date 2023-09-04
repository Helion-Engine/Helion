using Helion.GeometryNew.Vectors;

namespace Helion.GeometryNew.Boxes;

public struct Box2i
{
    public Vec2i Origin;
    public Vec2i Sides;
    
    public float Width => Sides.Width;
    public float Height => Sides.Height;

    public Box2i(Vec2i origin, Vec2i sides)
    {
        Origin = origin;
        Sides = sides;
    }
}