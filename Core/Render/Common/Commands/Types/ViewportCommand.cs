using Helion.Geometry;
using Helion.Geometry.Vectors;
using System.Runtime.InteropServices;

namespace Helion.Render.OpenGL.Commands.Types;

[StructLayout(LayoutKind.Sequential)]
public struct ViewportCommand
{
    public readonly Dimension Dimension;
    public readonly Vec2I Offset;

    public ViewportCommand(Dimension dimension, Vec2I offset)
    {
        Dimension = dimension;
        Offset = offset;
    }
}
