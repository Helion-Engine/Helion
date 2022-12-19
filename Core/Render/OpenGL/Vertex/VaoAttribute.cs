using OneOf;
using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Vertex;

public class VaoAttribute
{
    public readonly string Name;
    public readonly int Index;
    public readonly int Size;
    public readonly OneOf<VertexAttribPointerType, VertexAttribIntegerType> PointerType;
    public readonly int Offset;
    public readonly bool Normalized;
    public int Stride;

    private string PointerTypeToString => PointerType.Match(f => f.ToString(), i => i.ToString());

    public VaoAttribute(string name, int index, int size, VertexAttribPointerType type, int offset, bool normalized, int stride)
    {
        Name = name;
        Index = index;
        Size = size;
        PointerType = type;
        Offset = offset;
        Normalized = normalized;
        Stride = stride;
    }
    
    public VaoAttribute(string name, int index, int size, VertexAttribIntegerType type, int offset, int stride)
    {
        Name = name;
        Index = index;
        Size = size;
        PointerType = type;
        Offset = offset;
        Stride = stride;
    }
    
    public override string ToString() => $"[{Index}] {Name} (size: {Size}, offset: {Offset}, type: {PointerTypeToString}, normalized: {Normalized}, stride: {Stride})";
}
