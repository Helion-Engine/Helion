using System.Runtime.InteropServices;

namespace Helion.Geometry.New;

public readonly record struct Quad<T>(T TopLeft, T TopRight, T BottomLeft, T BottomRight);

[StructLayout(LayoutKind.Sequential, Pack = 4)]
public readonly record struct QuadF(Vec3 TopLeft, Vec3 TopRight, Vec3 BottomLeft, Vec3 BottomRight);
