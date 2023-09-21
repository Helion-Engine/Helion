using System.Runtime.InteropServices;
using Helion.Geometry.Vectors;

namespace Helion.Render.OpenGL.Renderers.Legacy.World.Geometry.Portals.FloodFill.Optimized;

[StructLayout(LayoutKind.Sequential, Pack = 1)]
public readonly record struct FloodFillVertex(Vec3F Pos);