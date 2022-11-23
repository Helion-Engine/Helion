using System;

namespace Helion.Render.OpenGL.Context.Types;

[Flags]
public enum ClearType
{
    DepthBufferBit = 256,
    StencilBufferBit = 1024,
    ColorBufferBit = 16384,
}
