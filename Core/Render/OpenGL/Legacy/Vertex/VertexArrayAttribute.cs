using Helion.Render.OpenGL.Legacy.Context;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Legacy.Vertex
{
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

        public abstract void Enable(IGLFunctions gl, int stride, int offset);
    }
}