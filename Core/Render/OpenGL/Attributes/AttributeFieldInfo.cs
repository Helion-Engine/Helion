using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Attributes
{
    public record AttributeFieldInfo(string Name, int Size, int BytesPerSize, bool Normalized, int Offset,
        VertexAttribPointerType AttribType);
}
