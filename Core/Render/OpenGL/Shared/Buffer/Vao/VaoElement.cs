using OpenTK.Graphics.OpenGL;
using System;
using static Helion.Util.Assert;

namespace Helion.Render.OpenGL.Shared.Buffer.Vao
{
    public abstract class VaoAttribute
    {
        public readonly string Name;
        public readonly int Index;
        public readonly int Size;

        protected VaoAttribute(string name, int index, int size)
        {
            Precondition(name.Length > 0, "Cannot have an empty VAO attribute name");
            Precondition(index >= 0, "VAO attribute index must be positive");
            Precondition(size > 0, "Cannot have a VAO attribute with no size");

            Name = name;
            Index = index;
            Size = size;
        }

        public void BindShaderLocation(int programId)
        {
            Precondition(programId >= 0, "Invalid program ID, cannot bind VAO attribute to shader location");

            GL.BindAttribLocation(programId, Index, Name);
        }

        public abstract int ByteLength();

        public abstract void Enable(int stride, int offset);
    }

    public class VaoAttributeF : VaoAttribute
    {
        public readonly VertexAttribPointerType Type;
        public readonly bool Normalized;

        public VaoAttributeF(string name, int index, int size, VertexAttribPointerType type, bool normalized = false) :
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

    public class VaoAttributeI : VaoAttribute
    {
        public readonly VertexAttribIntegerType Type;

        public VaoAttributeI(string name, int index, int size, VertexAttribIntegerType type) :
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
