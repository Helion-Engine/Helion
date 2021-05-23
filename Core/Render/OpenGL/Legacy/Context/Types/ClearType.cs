using System;

namespace Helion.Render.OpenGL.Legacy.Context.Types
{
    [Flags]
    public enum ClearType
    {
        DepthBufferBit = 256,
        StencilBufferBit = 1024,
        ColorBufferBit = 16384,
    }
}