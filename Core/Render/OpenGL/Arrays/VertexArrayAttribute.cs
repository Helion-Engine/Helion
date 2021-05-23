using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using GlmSharp;
using Helion.Geometry.Vectors;
using Helion.Render.OpenGL.Primitives;
using OpenTK.Graphics.OpenGL4;

namespace Helion.Render.OpenGL.Arrays
{
    public class VertexArrayAttribute
    {
        public readonly int Index;
        public readonly int Size;
        public readonly bool Normalized;
        public readonly int Offset;
        public int Stride { get; internal set; }
        public readonly VertexAttribPointerType? Type;
        public readonly VertexAttribIntegerType? IntType;
        public readonly VertexAttribDoubleType? DoubleType;

        private VertexArrayAttribute(int index, int size, bool normalized, int offset, int stride, 
            VertexAttribPointerType? type, VertexAttribIntegerType? intType, VertexAttribDoubleType? doubleType)
        {
            Index = index;
            Size = size;
            Normalized = normalized;
            Offset = offset;
            Stride = stride;
            Type = type;
            IntType = intType;
            DoubleType = doubleType;
        }

        public static VertexArrayAttribute From(VertexAttribPointerType type, int index, int size, bool normalized, int offset, int stride)
        {
            return new(index, size, normalized, offset, stride, type, null, null);
        }
        
        public static VertexArrayAttribute From(VertexAttribIntegerType type, int index, int size, int offset, int stride)
        {
            return new(index, size, false, offset, stride, null, type, null);
        }
        
        public static VertexArrayAttribute From(VertexAttribDoubleType type, int index, int size, int offset, int stride)
        {
            return new(index, size, false, offset, stride, null, null, type);
        }

        public void EnableAttribute()
        {
            if (DoubleType != null)
                GL.VertexAttribLPointer(Index, Size, DoubleType.Value, Stride, new IntPtr(Offset));
            else if (IntType != null)
                GL.VertexAttribIPointer(Index, Size, IntType.Value, Stride, new IntPtr(Offset));
            else if (Type != null)
                GL.VertexAttribPointer(Index, Size, Type.Value, Normalized, Stride, new IntPtr(Offset));
            else
                throw new Exception("Unexpected failure of applying vertex attribute, no known type");
            
            GL.EnableVertexAttribArray(Index);
        }

        private static bool HasNormalizedAttribute(FieldInfo fieldInfo)
        {
            return fieldInfo.GetCustomAttributes().OfType<NormalizedAttribute>().Any();
        }
        
        public static List<VertexArrayAttribute> FindAttributes<TVertex>() where TVertex : struct
        {
            List<VertexArrayAttribute> vaoAttributes = new();

            int index = 0;
            int offset = 0;
            foreach (FieldInfo field in typeof(TVertex).GetFields())
            {
                Type fieldType = field.FieldType;
                bool normalized = HasNormalizedAttribute(field);
        
                if (fieldType == typeof(byte))
                    HandleIntAttribute(1, VertexAttribIntegerType.UnsignedByte, sizeof(byte));
                else if (fieldType == typeof(int))
                    HandleIntAttribute(1, VertexAttribIntegerType.Int, sizeof(int));
                else if (fieldType == typeof(uint))
                    HandleIntAttribute(1, VertexAttribIntegerType.UnsignedInt, sizeof(uint));
                else if (fieldType == typeof(float))
                    HandleAttribute(1, VertexAttribPointerType.Float, normalized, sizeof(float));
                else if (fieldType == typeof(double))
                    HandleDoubleAttribute(1, VertexAttribDoubleType.Double, sizeof(double));
                else if (fieldType == typeof(vec2) || fieldType == typeof(Vec2F))
                    HandleAttribute(2, VertexAttribPointerType.Float, normalized, sizeof(float));
                else if (fieldType == typeof(vec3) || fieldType == typeof(Vec3F))
                    HandleAttribute(3, VertexAttribPointerType.Float, normalized, sizeof(float));
                else if (fieldType == typeof(vec4) || fieldType == typeof(Vec4F))
                    HandleAttribute(4, VertexAttribPointerType.Float, normalized, sizeof(float));
                else if (fieldType == typeof(ByteColor))
                    HandleAttribute(4, VertexAttribPointerType.UnsignedByte, normalized, sizeof(byte));
                else if (fieldType == typeof(BindlessHandle))
                    HandleIntAttribute(2, VertexAttribIntegerType.UnsignedInt, sizeof(uint));
                else
                    throw new Exception($"Unexpected field in vertex attribute {fieldType.Name} when getting attributes");
        
                index++;
            }
            
            // The stride is the total offset we have moved.
            foreach (VertexArrayAttribute attr in vaoAttributes)
                attr.Stride = offset;

            return vaoAttributes;
            
            void HandleAttribute(int size, VertexAttribPointerType type, bool isNormalized, int primitiveSize)
            {
                VertexArrayAttribute attr = From(type, index, size, isNormalized, offset, 0);
                vaoAttributes.Add(attr);
                offset += primitiveSize * size;
            }
            
            void HandleIntAttribute(int size, VertexAttribIntegerType type, int primitiveSize)
            {
                VertexArrayAttribute attr = From(type, index, size, offset, 0);
                vaoAttributes.Add(attr);
                offset += primitiveSize * size;
            }
            
            void HandleDoubleAttribute(int size, VertexAttribDoubleType type, int primitiveSize)
            {
                VertexArrayAttribute attr = From(type, index, size, offset, 0);
                vaoAttributes.Add(attr);
                offset += primitiveSize * size;
            }
        }
    }
}
