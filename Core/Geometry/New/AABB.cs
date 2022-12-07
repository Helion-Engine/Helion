namespace Helion.Geometry.New;

// Extent's are the radius, or the distance from the center to the edge of the box.
public readonly record struct AABB2d(Vec2d Pos, Vec2d Extent)
{
    public Vec2d Min => Pos - Extent;
    public Vec2d Max => Pos + Extent;
    public Box2d Box => new(Min, Max);

    public AABB2d(Vec2d center, double radius) : this(center, (radius, radius))
    {
    }

    public AABB2d(Seg2d seg) : this(SegToAABBCenter(seg, out var extent), extent)
    {
    }

    public static AABB2d operator +(AABB2d self, Vec2d other) => new(self.Pos + other, self.Extent);
    public static AABB2d operator -(AABB2d self, Vec2d other) => new(self.Pos - other, self.Extent);

    private static Vec2d SegToAABBCenter(Seg2d seg, out Vec2d extent)
    {
        AABB2d aabb = new Box2d(seg).AABB;
        extent = aabb.Extent;
        return aabb.Pos;
    }
}

public readonly record struct AABB3d(Vec3d Pos, Vec3d Extent)
{
    public Vec3d Min => Pos - Extent;
    public Vec3d Max => Pos + Extent;
    public Box3d Box => new(Min, Max);

    public AABB3d(Seg3d seg) : this(SegToAABBCenter(seg, out var extent), extent)
    {
    }

    public static AABB3d operator +(AABB3d self, Vec3d other) => new(self.Pos + other, self.Extent);
    public static AABB3d operator -(AABB3d self, Vec3d other) => new(self.Pos - other, self.Extent);

    private static Vec3d SegToAABBCenter(Seg3d seg, out Vec3d extent)
    {
        AABB3d aabb = new Box3d(seg).AABB;
        extent = aabb.Extent;
        return aabb.Pos;
    }
}
