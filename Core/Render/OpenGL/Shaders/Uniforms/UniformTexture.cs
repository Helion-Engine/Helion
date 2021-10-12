using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Shaders.Uniforms;

public class UniformTexture : UniformElement<TextureUnit>
{
    /// <summary>
    /// Since we use an integer value as a sampler, this is a helper function
    /// to help make it easier on us.
    /// </summary>
    /// <param name="textureUnit">The unit to set into the uniform.</param>
    public override void Set(TextureUnit textureUnit)
    {
        Precondition(Location != NoLocation, "Uniform (texture unit) int value did not have the location set");
        Precondition(textureUnit >= TextureUnit.Texture0, "Trying to use an invalid texture index");

        GL.Uniform1(Location, (int)textureUnit - (int)TextureUnit.Texture0);
    }
}

