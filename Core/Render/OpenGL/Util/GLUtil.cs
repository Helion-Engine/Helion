using Helion.Render.OpenGL.Capabilities;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Util
{
    public static class GLUtil
    {
        public const int GLTrue = 1;

        /// <summary>
        /// Attaches a label if the OpenGL version supports it.
        /// </summary>
        /// <param name="label">The label to attach.</param>
        /// <param name="type">The type of label.</param>
        /// <param name="name">The GL name.</param>
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
