using Helion.Render.Legacy.Context.Types;

namespace Helion.Render.Legacy.Context;

public class GLLimits
{
    public readonly float MaxAnisotropy;

    public GLLimits(IGLFunctions gl)
    {
        MaxAnisotropy = gl.GetFloat(GetFloatType.MaxTextureMaxAnisotropyExt);
        // TODO: GL_MAX_UNIFORM_BUFFER_BINDINGS
        // TODO: GL_MAX_SHADER_STORAGE_BUFFER_BINDINGS
    }
}

