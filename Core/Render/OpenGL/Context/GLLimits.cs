using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Context;

public static class GLLimits
{
    public static readonly float MaxAnisotropy;

    static GLLimits()
    {
        MaxAnisotropy = GL.GetFloat(GetPName.MaxTextureMaxAnisotropy);
    }
}
