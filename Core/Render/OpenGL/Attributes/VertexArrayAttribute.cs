using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Attributes
{
    public record VertexAttributeArrayElement(string Name, int Size, bool Normalized, int Offset, 
        int Stride, VertexAttribPointerType Type)
    {
    }
}
