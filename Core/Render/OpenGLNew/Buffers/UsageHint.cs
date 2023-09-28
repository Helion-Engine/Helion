using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGLNew.Buffers;

public enum UsageHint
{
    Static,
    Dynamic,
    Stream
}

public static class UsageHintHelper
{
    public static BufferUsageHint ToBufferUsageHint(this UsageHint usageHint)
    {
        return usageHint switch
        {
            UsageHint.Static => BufferUsageHint.StaticDraw,
            UsageHint.Dynamic => BufferUsageHint.DynamicDraw,
            UsageHint.Stream => BufferUsageHint.StreamDraw,
        };
    }
}