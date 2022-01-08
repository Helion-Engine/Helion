using System.Drawing;
using System.Runtime.InteropServices;
using Helion.Graphics.Geometry;

namespace Helion.Render.Legacy.Commands.Types;

[StructLayout(LayoutKind.Sequential)]
public struct DrawShapeCommand
{
    public readonly ImageBox2I Rectangle;
    public readonly Color Color;
    public readonly float Alpha;

    public DrawShapeCommand(ImageBox2I rectangle, Color color, float alpha)
    {
        Rectangle = rectangle;
        Color = color;
        Alpha = alpha;
    }
}
