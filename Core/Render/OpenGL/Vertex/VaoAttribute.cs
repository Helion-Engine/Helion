namespace Helion.Render.OpenGL.Vertex;

public class VaoAttribute
{
    public readonly string Name;
    public readonly int Index;
    public readonly int Size;
    public readonly int Offset;
    public readonly bool Normalized;
    public int Stride;

    public VaoAttribute(string name, int index, int size, int offset, bool normalized, int stride)
    {
        Name = name;
        Index = index;
        Size = size;
        Offset = offset;
        Normalized = normalized;
        Stride = stride;
    }

    public override string ToString() => $"[{Index}] {Name} (size: {Size}, offset: {Offset}, normalized: {Normalized}, stride: {Stride})";
}
