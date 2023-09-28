using System;
using System.Collections.Generic;
using System.Reflection;
using System.Runtime.InteropServices;

namespace Helion.Render.OpenGLNew.Attributes;

public record VertexAttributeInfo(string Name, int Index, int Size, int DataType, bool IsIntegral, bool Normalized, int Stride, int Offset);

public class VertexAttributeTracker
{
    private static readonly Dictionary<Type, List<VertexAttributeInfo>> TypeToData = new();

    public static List<VertexAttributeInfo> GetAttributes<TVertex>() where TVertex : struct
    {
        Type vertexType = typeof(TVertex);
        if (TypeToData.TryGetValue(vertexType, out List<VertexAttributeInfo>? attrList))
            return attrList;

        List<VertexAttributeInfo> attributes = new();
        int stride = Marshal.SizeOf<TVertex>();
        int offset = 0;

        foreach (FieldInfo info in vertexType.GetFields())
        {
            // TODO
        }

        TypeToData[vertexType] = attributes;
        return attributes;
    }
}