using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Vertex;

public class VaoAttribute
{
    public readonly string Name;
    public readonly int Index;
    public readonly int Size;
    public readonly VertexAttribPointerType? PointerType;
    public readonly VertexAttribIntegerType? IntegerType;
    public readonly int Offset;
    public readonly bool Normalized;
    public readonly bool Required;
    public int Stride;

    private string PointerTypeToString => PointerType != null ? PointerType.ToString() : IntegerType?.ToString();

    public VaoAttribute(string name, int index, int size, VertexAttribPointerType type, int offset, bool normalized, int stride, bool required)
    {
        Name = name;
        Index = index;
        Size = size;
        PointerType = type;
        Offset = offset;
        Normalized = normalized;
        Stride = stride;
        Required = required;
    }
    
    public VaoAttribute(string name, int index, int size, VertexAttribIntegerType type, int offset, int stride, bool required)
    {
        Name = name;
        Index = index;
        Size = size;
        IntegerType = type;
        Offset = offset;
        Stride = stride;
        Required = required;
    }
    
    public override string ToString() => $"[{Index}] {Name} (size: {Size}, offset: {Offset}, type: {PointerTypeToString}, normalized: {Normalized}, stride: {Stride})";
}
