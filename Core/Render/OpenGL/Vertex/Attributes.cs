using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Buffer.Array.Vertex;
using Helion.Render.OpenGL.Shader;
using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Helion.Render.OpenGL.Vertex;

public static class Attributes
{
    private static readonly Dictionary<Type, VaoAttribute[]> TypeToData = new();
    private static readonly HashSet<Type> IsValid = new(); // If present, there's no undefined attributes, and is safe to use.

    private static readonly HashSet<int> IndexUsed = new();

    public static VaoAttribute[] ReadStructAttributes<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TVertex>() where TVertex : struct
    {
        Type type = typeof(TVertex);
        if (TypeToData.TryGetValue(type, out var result))
            return result;

        int offset = 0;
        int stride = 0;
        int nextAvailableIndex = 0;

        var fields = type.GetFields();

        var attributes = new List<VaoAttribute>();

        foreach (FieldInfo info in fields)
        {
            int size = 0;
            int primitiveSize = sizeof(float);
            VertexAttribPointerType pointerType = VertexAttribPointerType.Float;
            VertexAttribIntegerType integerType = VertexAttribIntegerType.Int;

            if (info.FieldType == typeof(byte))
            {
                primitiveSize = 1;
                size = 1;
                pointerType = VertexAttribPointerType.UnsignedByte;
                integerType = VertexAttribIntegerType.UnsignedByte;
            }
            else if (info.FieldType == typeof(sbyte))
            {
                primitiveSize = 1;
                size = 1;
                pointerType = VertexAttribPointerType.Byte;
                integerType = VertexAttribIntegerType.Byte;
            }
            else if (info.FieldType == typeof(int))
            {
                primitiveSize = 4;
                size = 1;
                pointerType = VertexAttribPointerType.Int;
                integerType = VertexAttribIntegerType.Int;
            }
            else if (info.FieldType == typeof(short))
            {
                primitiveSize = 2;
                size = 1;
                pointerType = VertexAttribPointerType.Short;
                integerType = VertexAttribIntegerType.Short;
            }
            else if (info.FieldType == typeof(ushort))
            {
                primitiveSize = 2;
                size = 1;
                pointerType = VertexAttribPointerType.UnsignedShort;
                integerType = VertexAttribIntegerType.UnsignedShort;
            }
            else if (info.FieldType == typeof(uint))
            {
                primitiveSize = 4;
                size = 1;
                pointerType = VertexAttribPointerType.UnsignedByte;
                integerType = VertexAttribIntegerType.UnsignedInt;
            }
            else if (info.FieldType == typeof(float))
            {
                size = 1;
            }
            else if (info.FieldType == typeof(Vec2F) || info.FieldType == typeof(vec2))
            {
                size = 2;
            }
            else if (info.FieldType == typeof(Vec3F) || info.FieldType == typeof(vec3))
            {
                size = 3;
            }
            else if (info.FieldType == typeof(Vec4F) || info.FieldType == typeof(vec4))
            {
                size = 4;
            }
            else
            {
                throw new($"Unsupported attribute type in {nameof(TVertex)}: {info.FieldType.FullName}");
            }

            VertexAttributeAttribute? codeAttr = info.GetCustomAttribute<VertexAttributeAttribute>();
            if (codeAttr == null)
                continue;

            string? name = codeAttr.InferName ? info.Name : codeAttr.Name;
            size = codeAttr.InferSize ? size : codeAttr.Size;
            int index = GetNextIndex(codeAttr, IndexUsed, ref nextAvailableIndex);
            IndexUsed.Add(index);

            if (name == null)
                throw new Exception("Failed to read attrubute name");

            VaoAttribute attr = codeAttr.IsIntegral ?
                new(name, index, size, integerType, offset, stride, codeAttr.Required) :
                new(name, index, size, pointerType, offset, codeAttr.Normalized, stride, codeAttr.Required, codeAttr.Divisor);
            
            attributes.Add(attr);

            int sizeBytes = size * primitiveSize;
            offset += sizeBytes;
            stride += sizeBytes;
        }

        // Doing two passes over everything is extra code, so we'll update at the end
        // with the correct stride value after accumulating one thing from a single pass.
        foreach (VaoAttribute attr in attributes) 
            attr.Stride = stride;

        IndexUsed.Clear();
        var attributesArray = attributes.ToArray();
        TypeToData[type] = attributesArray;
        return attributesArray;
    }

    private static int GetNextIndex(VertexAttributeAttribute codeAttribute, HashSet<int> indexUsed, ref int nextAvailableIndex)
    {
        if (!codeAttribute.InferIndex)
            return codeAttribute.Index;

        while (indexUsed.Contains(nextAvailableIndex))
            nextAvailableIndex++;

        return nextAvailableIndex;
    }

    [Conditional("DEBUG")]
    private static void AssertCorrectMappingOrThrow<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TVertex>(ProgramAttributes shaderAttribs) where TVertex : struct
    {
        Type type = typeof(TVertex);
        if (IsValid.Contains(type))
            return;

        // We will evaluate struct layouts as well at the same time.
        if ((type.Attributes & TypeAttributes.SequentialLayout) != TypeAttributes.SequentialLayout)
            throw new($"Layout of {nameof(TVertex)} is not {LayoutKind.Sequential}");

        foreach (VaoAttribute attr in ReadStructAttributes<TVertex>())
        {
            ProgramAttribute? progAttr = FindShaderAttribute(shaderAttribs, attr.Name);
            //if (progAttr == null && attr.Required)
            //    throw new($"Cannot find shader attribute named {attr.Name}, was it optimized out?");

            // TODO: This doesn't work because Size = 1 for Vec2, but we supply Size = 2 for Float.
            // This means we need to be able to find out if different types are the same size, since
            // in the above they are, but the code below says they are not.
            //if (attr.Size != progAttr.Size)
            //    throw new($"Different attribute size in shader for {attr.Name}, expected {attr.Size}, got {progAttr.Size}");
        }

        IsValid.Add(type);
    }

    // Assumes the VBO and VAO have been bound.
    public static void Apply<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TVertex>(ProgramAttributes shaderAttribs) where TVertex : struct
    {
        AssertCorrectMappingOrThrow<TVertex>(shaderAttribs);

        foreach (VaoAttribute attr in ReadStructAttributes<TVertex>())
        {
            if (attr.PointerType.HasValue)
                GL.VertexAttribPointer(attr.Index, attr.Size, attr.PointerType.Value, attr.Normalized, attr.Stride, attr.Offset);
            else if (attr.IntegerType.HasValue)
                GL.VertexAttribIPointer(attr.Index, attr.Size, attr.IntegerType.Value, attr.Stride, new(attr.Offset));

            if (attr.Divisor > 0)
                GL.VertexAttribDivisor(attr.Index, attr.Divisor);

            GL.EnableVertexAttribArray(attr.Index);
        }
    }

    public static void ApplyModern<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TVertex>(VertexBufferObject<TVertex> vbo, VertexArrayObject vao, ProgramAttributes shaderAttribs)
        where TVertex : struct
    {
        AssertCorrectMappingOrThrow<TVertex>(shaderAttribs);

        foreach (var attr in ReadStructAttributes<TVertex>())
        {
            GL.EnableVertexArrayAttrib(vao.Handle, attr.Index);

            if (attr.PointerType.HasValue)
            {
                GL.VertexArrayAttribFormat(
                    vao.Handle,
                    attr.Index,
                    attr.Size,
                    (VertexAttribType)attr.PointerType.Value,
                    attr.Normalized,
                    attr.Offset
                );
            }
            else if (attr.IntegerType.HasValue)
            {
                GL.VertexArrayAttribIFormat(
                    vao.Handle,
                    attr.Index,
                    attr.Size,
                    (VertexAttribIType)attr.IntegerType.Value,
                    attr.Offset
                );
            }

            GL.VertexArrayAttribBinding(vao.Handle, attr.Index, attr.Index);
        }
    }

    public static void BindAndApply<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TVertex>(VertexBufferObject<TVertex> vbo, VertexArrayObject vao, ProgramAttributes shaderAttribs)
        where TVertex : struct
    {
        vao.Bind();
        vbo.Bind();

        Apply<TVertex>(shaderAttribs);

        vbo.Unbind();
        vao.Unbind();
    }

    public static void BindVertexBuffer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TVertex>(VertexBufferObject<TVertex> vbo, VaoAttribute[] attributes)
        where TVertex : struct
    {
        foreach (var attr in attributes)
            GL.BindVertexBuffer(attr.Index, vbo.BufferId, 0, attr.Stride);
    }

    public static void BindVertexBuffer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TVertex>(VertexBufferObject<TVertex> vbo)
        where TVertex : struct
    {
        foreach (var attr in ReadStructAttributes<TVertex>())
            GL.BindVertexBuffer(attr.Index, vbo.BufferId, 0, attr.Stride);
    }

    public static void BindVertexArrayBuffer<[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields)] TVertex>(VertexArrayObject vao, VertexBufferObject<TVertex> vbo)
        where TVertex : struct
    {
        foreach (var attr in ReadStructAttributes<TVertex>())
            GL.VertexArrayVertexBuffer(vao.Handle, attr.Index, vbo.BufferId, 0, attr.Stride);
    }

    private static ProgramAttribute? FindShaderAttribute(ProgramAttributes shaderAttribs, string name)
    {
        for (int i = 0; i < shaderAttribs.Count; i++)
        {
            if (shaderAttribs[i].Name.Equals(name, StringComparison.OrdinalIgnoreCase))
                return shaderAttribs[i];
        }

        return null;
    }
}
