namespace Helion.Geometry.New;

public readonly record struct Quad<T>(T TopLeft, T TopRight, T BottomLeft, T BottomRight);
public readonly record struct QuadF(Vec3f TopLeft, Vec3f TopRight, Vec3f BottomLeft, Vec3f BottomRight);
public readonly record struct QuadD(Vec3d TopLeft, Vec3d TopRight, Vec3d BottomLeft, Vec3d BottomRight);
