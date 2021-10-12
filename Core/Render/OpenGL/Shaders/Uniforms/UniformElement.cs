namespace Helion.Render.OpenGL.Shaders.Uniforms;

public abstract class UniformElement<T> : Uniform where T : struct
{
    public abstract void Set(T value);
}

