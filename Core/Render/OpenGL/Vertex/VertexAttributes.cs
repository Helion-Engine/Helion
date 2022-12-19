using OpenTK.Graphics.OpenGL;
using System;

namespace Helion.Render.OpenGL.Vertex;

public class VertexAttributeAttribute : Attribute
{
    public const int InferFromField = -1;

    public readonly string? Name;
    public readonly int Index;
    public readonly int Size;
    public readonly VertexAttribPointerType Type;
    public readonly bool IsIntegral;
    public readonly bool Normalized;

    public bool InferName => Name == null;
    public bool InferIndex => Index == InferFromField;
    public bool InferSize => Size == InferFromField;
    public bool InferType => Type == (VertexAttribPointerType)InferFromField;

    public VertexAttributeAttribute(string? name = null, int index = InferFromField, int size = InferFromField, bool isIntegral = false, bool normalized = false) :
        this((VertexAttribPointerType)InferFromField, name, index, size, isIntegral, normalized)
    {
    }

    public VertexAttributeAttribute(VertexAttribPointerType type, string? name = null, int index = InferFromField, int size = InferFromField, bool isIntegral = false, bool normalized = false)
    {
        Name = name;
        Index = index;
        Size = size;
        Type = type;
        IsIntegral = isIntegral;
        Normalized = normalized;
    }

    public override string ToString() => $"[{Index}] {Name} {Type} (size: {Size}, integral: {IsIntegral}, normalized: {Normalized})";
}
