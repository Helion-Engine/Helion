using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using Helion.Render.OpenGL.Util;
using OpenTK.Graphics.OpenGL;
using static Helion.Util.Assert;

namespace Helion.Render.OpenGL.Buffers
{
    public class VertexArrayAttributes : IEnumerable<VertexArrayAttribute>
    {
        private List<VertexArrayAttribute> attributes = new List<VertexArrayAttribute>();

        public int Count => attributes.Count();
        
        public VertexArrayAttributes(params VertexArrayAttribute[] vaoAttributes)
        {
            Precondition(vaoAttributes.Length > 0, "Cannot have a VAO with no attributes");
            
            attributes.AddRange(vaoAttributes);
        }

        public IEnumerator<VertexArrayAttribute> GetEnumerator() => attributes.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
    
    public abstract class VertexArrayAttribute
    {
        public readonly string Name;
        public readonly int Index;
        public readonly int Size;

        protected VertexArrayAttribute(string name, int index, int size)
        {
            Precondition(name.Length > 0, "Cannot have an empty VAO attribute name");
            Precondition(index >= 0, "VAO attribute index must be positive");
            Precondition(size > 0, "Cannot have a VAO attribute with no size");

            Name = name;
            Index = index;
            Size = size;
        }
        
        public abstract int ByteLength();

        public abstract void Enable(int stride, int offset);
    }
    
    public class VertexFloatAttribute : VertexArrayAttribute
    {
        public readonly VertexAttribPointerType Type;
        public readonly bool Normalized;

        public VertexFloatAttribute(string name, int index, int size, VertexAttribPointerType type = VertexAttribPointerType.Float, bool normalized = false) :
            base(name, index, size)
        {
            Type = type;
            Normalized = normalized;
        }

        public override int ByteLength() => GLHelper.ToByteLength(Type) * Size;

        public override void Enable(int stride, int offset)
        {
            Precondition(stride >= ByteLength(), "Stride is smalle than the length of the VAO element");
            Precondition(offset >= 0 && offset < stride, $"Offset relative to stride is wrong: offset={offset}, stride={stride}");

            GL.VertexAttribPointer(Index, Size, Type, Normalized, stride, offset);
            GL.EnableVertexAttribArray(Index);
        }
    }
    
    public class VertexIntAttribute : VertexArrayAttribute
    {
        public readonly VertexAttribIntegerType Type;

        public VertexIntAttribute(string name, int index, int size, VertexAttribIntegerType type = VertexAttribIntegerType.Int) :
            base(name, index, size)
        {
            Type = type;
        }

        public override int ByteLength() => GLHelper.ToByteLength(Type) * Size;

        public override void Enable(int stride, int offset)
        {
            Precondition(stride >= ByteLength(), "Stride is smalle than the length of the VAO element");
            Precondition(offset >= 0 && offset < stride, $"Offset relative to stride is wrong: offset={offset}, stride={stride}");

            GL.VertexAttribIPointer(Index, Size, Type, stride, IntPtr.Zero);
            GL.EnableVertexAttribArray(Index);
        }
    }
}