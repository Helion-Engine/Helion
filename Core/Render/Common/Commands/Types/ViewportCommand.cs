using Helion;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Render;
using Helion.Render.Common.Commands.Types;
using System.Runtime.InteropServices;

namespace Helion.Render.Common.Commands.Types;

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
