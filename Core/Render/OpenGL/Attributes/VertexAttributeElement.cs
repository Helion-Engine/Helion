using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Attributes
{
    public record VertexAttributeElement(int Location, string Name, int Size, bool Normalized, int Offset, 
        int Stride, VertexAttribPointerType Type);
}
