using Helion.Graphics;
using System.Runtime.InteropServices;

namespace Helion.Render.OpenGL.Commands.Types;

[StructLayout(LayoutKind.Sequential)]
public struct ClearRenderCommand
{
    public static readonly Color DefaultClearColor = (0, 0, 0, 0);

    public readonly bool Color;
    public readonly bool Depth;
    public readonly bool Stencil;
    public readonly Color ClearColor;

    public ClearRenderCommand(bool color, bool depth, bool stencil, Color clearColor)
    {
        Color = color;
        Depth = depth;
        Stencil = stencil;
        ClearColor = clearColor;
    }

    public static ClearRenderCommand All() => All((16, 16, 16));

    public static ClearRenderCommand All(Color clearColor)
    {
        return new ClearRenderCommand(true, true, true, clearColor);
    }

    public static ClearRenderCommand DepthOnly()
    {
        return new ClearRenderCommand(false, true, false, DefaultClearColor);
    }
}
