namespace Helion.Render.OpenGL.Shaders.Attributes
{
    /// <summary>
    /// A shader attribute that is sourced from a program.
    /// </summary>
    public record VertexShaderAttribute(int Location, string Name)
    {
        public const int NoLocation = -1;
    }
}
