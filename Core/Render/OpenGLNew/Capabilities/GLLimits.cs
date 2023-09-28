using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGLNew.Capabilities;

public static class GLLimits
{
    public static readonly float MaxAnisotropy;
    public static readonly int MaxTextureBufferSize;

    static GLLimits()
    {
        MaxAnisotropy = GL.GetFloat(GetPName.MaxTextureMaxAnisotropy);
        MaxTextureBufferSize = GL.GetInteger(GetPName.MaxTextureBufferSize);
    }
}
