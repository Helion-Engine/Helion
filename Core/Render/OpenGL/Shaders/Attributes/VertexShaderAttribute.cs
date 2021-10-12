using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Shaders.Attributes;

/// <summary>
/// A shader attribute that is sourced from a program.
/// </summary>
public record VertexShaderAttribute(int Location, string Name, int Index, int Size, ActiveAttribType AttributeType)
{
    public const int NoLocation = -1;
}
