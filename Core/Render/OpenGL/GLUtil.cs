using Helion.Render.OpenGL.Capabilities;
using OpenTK.Graphics.OpenGL4;

namespace Helion.Render.OpenGL
{
    public static class GLUtil
    {
        public const int GLTrue = 1;
        
        public static void Label(string label, ObjectLabelIdentifier type, int name)
        {
            if (GLCapabilities.SupportsObjectLabels)
                GL.ObjectLabel(type, name, label.Length, label);
        }

        public static bool IsStream(this BufferUsageHint hint)
        {
            return hint switch
            {
                BufferUsageHint.StreamDraw => true,
                BufferUsageHint.StreamRead => true,
                BufferUsageHint.StreamCopy => true,
                _ => false,
            };
        }
    }
}
