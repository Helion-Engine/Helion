using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Capabilities
{
    public class GLInfo
    {
        public readonly string Vendor;
        public readonly string ShadingVersion;
        public readonly string Renderer;

        public GLInfo()
        {
            Renderer = GL.GetString(StringName.Renderer);
            ShadingVersion = GL.GetString(StringName.ShadingLanguageVersion);
            Vendor = GL.GetString(StringName.Vendor);
        }
    }
}