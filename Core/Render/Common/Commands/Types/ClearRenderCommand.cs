using System.Drawing;
using System.Runtime.InteropServices;
using Helion;
using Helion.Render;
using Helion.Render.Common.Commands.Types;
using SystemColor = System.Drawing.Color;

namespace Helion.Render.Common.Commands.Types;

[StructLayout(LayoutKind.Sequential)]
public struct ClearRenderCommand
{
    public static readonly SystemColor DefaultClearColor = SystemColor.Black;

    public readonly bool Color;
    public readonly bool Depth;
    public readonly bool Stencil;
    public readonly SystemColor ClearColor;

    public ClearRenderCommand(bool color, bool depth, bool stencil, SystemColor clearColor)
    {
        Color = color;
        Depth = depth;
        Stencil = stencil;
        ClearColor = clearColor;
    }

    public static ClearRenderCommand All() => All(SystemColor.FromArgb(16, 16, 16));

    public static ClearRenderCommand All(SystemColor clearColor)
    {
        return new ClearRenderCommand(true, true, true, clearColor);
    }

    public static ClearRenderCommand DepthOnly()
    {
        return new ClearRenderCommand(false, true, false, DefaultClearColor);
    }
}
