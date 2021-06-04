using Helion.Render.OpenGL.Capabilities;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL
{
    public static class GLUtil
    {
        public const int GLTrue = 1;

        /// <summary>
        /// Will properly select the binding function based on the version of
        /// of OpenGL available.
        /// </summary>
        /// <param name="fbo">The framebuffer object index.</param>
        /// <param name="target">The framebuffer target.</param>
        /// <returns>True if it could bind, false if it could not due to a low
        /// opengl version.</returns>
        public static bool BindFramebuffer(int fbo, FramebufferTarget target = FramebufferTarget.Framebuffer)
        {
            if (GLCapabilities.Extensions.Framebuffers.HasNativeSupport)
                GL.BindFramebuffer(target, fbo);
            else if (GLCapabilities.Extensions.Framebuffers.HasExtSupport)
                GL.Ext.BindFramebuffer(target, fbo);
            else
                return false;

            return true;
        }
        
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
