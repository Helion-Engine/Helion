using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Shader;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Reflection;

namespace Helion.Render.OpenGL.Vertex.New;

class VaoAttribute
{
    public string Name;
    public int Index;
    public int Size;
    public int Offset;
    public int Stride;
    public bool Normalized;

    public VaoAttribute(string name, int index, int size, int offset, int stride, bool normalized)
    {
        Name = name;
        Index = index;
        Size = size;
        Offset = offset;
        Stride = stride;
        Normalized = normalized;
    }
}

public static class Attributes
{
    private static readonly Dictionary<Type, List<VaoAttribute>> TypeToData = new();

    private static List<VaoAttribute> ReadStructAttributes<TVertex>() where TVertex : struct
    {
        Type type = typeof(TVertex);
        if (TypeToData.TryGetValue(type, out var result))
            return result;

        List<VaoAttribute> attributes = new();
        TypeToData[type] = attributes;

        int offset = 0;
        int stride = 0;
        int nextAvailableIndex = 0;
        HashSet<int> indexUsed = new();

        foreach (FieldInfo info in type.GetFields())
        {
            int size = 0;
            int primitiveSize = sizeof(float);

            if (info.FieldType == typeof(float))
                size = 1;
            else if (info.FieldType == typeof(Vec2F))
                size = 2;
            else if (info.FieldType == typeof(Vec3F))
                size = 3;
            else if (info.FieldType == typeof(Vec4F))
                size = 4;
            else
                throw new($"Unsupported attribute type in {nameof(TVertex)}: {info.FieldType.FullName}");

            int sizeBytes = size * primitiveSize;
            offset += sizeBytes;
            stride += sizeBytes;

            VertexAttributeAttribute codeAttr = info.FieldType.GetCustomAttribute<VertexAttributeAttribute>();
            if (codeAttr == null)
                continue;

            string name = codeAttr.InferName ? info.Name : codeAttr.Name;
            int index = GetNextIndex(codeAttr);
            indexUsed.Add(index);

            VaoAttribute attr = new(name, index, size, offset, stride, codeAttr.Normalized);
            attributes.Add(attr);
        }

        // Doing two passes over everything is extra code, so we'll update at the end
        // with the correct stride value after accumulating one thing from a single pass.
        attributes.ForEach(a => a.Stride = stride);

        return attributes;

        int GetNextIndex(VertexAttributeAttribute codeAttribute)
        {
            if (!codeAttribute.InferIndex)
                return codeAttribute.Index;

            while (indexUsed.Contains(nextAvailableIndex))
                nextAvailableIndex++;

            return nextAvailableIndex;
        }
    }

    // Assumes the VBO and VAO have been bound.
    public static void Apply<TVertex>(ProgramAttributes shaderAttribs) where TVertex : struct
    {
        foreach (VaoAttribute attr in ReadStructAttributes<TVertex>())
        {
            GL.VertexAttribPointer(attr.Index, attr.Size, VertexAttribPointerType.Float, attr.Normalized, attr.Stride, attr.Offset);
            GL.EnableVertexAttribArray(attr.Index);
        }
    }

    public static void BindAndApply<TVertex>(VertexBufferObject<TVertex> vbo, VertexArrayObject vao, ProgramAttributes shaderAttribs) 
        where TVertex : struct
    {
        vao.Bind();
        vbo.Bind();

        Apply<TVertex>(shaderAttribs);

        vbo.Unbind();
        vao.Unbind();
    }
}
