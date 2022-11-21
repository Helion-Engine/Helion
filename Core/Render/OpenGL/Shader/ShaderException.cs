using Helion;
using Helion.Render;
using Helion.Render.OpenGL.Shader;
using Helion.Util;

namespace Helion.Render.OpenGL.Shader;

public class ShaderException : HelionException
{
    public ShaderException(string message) : base(message)
    {
    }
}
