using System;
using Helion;
using Helion.Render;
using Helion.Render.OpenGL.Context.Types;

namespace Helion.Render.OpenGL.Context.Types;

[Flags]
public enum ClearType
{
    DepthBufferBit = 256,
    StencilBufferBit = 1024,
    ColorBufferBit = 16384,
}
