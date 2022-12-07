namespace Helion.Geometry.New;

public readonly record struct Triangle2<T>(T First, T Second, T Third);
public readonly record struct Triangle2F(Vec2f First, Vec2f Second, Vec2f Third);
public readonly record struct Triangle2D(Vec2d First, Vec2d Second, Vec2d Third);
public readonly record struct Triangle3F(Vec3f First, Vec3f Second, Vec3f Third);
public readonly record struct Triangle3D(Vec3d First, Vec3d Second, Vec3d Third);
