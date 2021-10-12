namespace Helion.Render.OpenGL.Shaders.Uniforms;

[Uniform]
public abstract class Uniform
{
    internal const int NoLocation = -1;

    public int Location { get; internal set; } = NoLocation;
}
