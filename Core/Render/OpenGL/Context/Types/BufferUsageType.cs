using Helion;
using Helion.Render;
using Helion.Render.OpenGL.Context.Types;

namespace Helion.Render.OpenGL.Context.Types;

public enum BufferUsageType
{
    StreamDraw = 35040,
    StreamRead = 35041,
    StreamCopy = 35042,
    StaticDraw = 35044,
    StaticRead = 35045,
    StaticCopy = 35046,
    DynamicDraw = 35048,
    DynamicRead = 35049,
    DynamicCopy = 35050,
}
