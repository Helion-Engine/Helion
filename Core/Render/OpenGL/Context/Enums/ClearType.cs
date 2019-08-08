using System;

namespace Helion.Render.OpenGL.Context.Enums
{
    [Flags]
    public enum ClearType
    {
        DepthBufferBit = 256,
        StencilBufferBit = 1024,
        ColorBufferBit = 16384,
    }
}