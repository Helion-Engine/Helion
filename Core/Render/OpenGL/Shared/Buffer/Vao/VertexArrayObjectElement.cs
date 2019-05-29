using OpenTK.Graphics.OpenGL;

namespace Helion.Render.OpenGL.Shared.Buffer.Vao
{
    public struct VertexArrayObjectElement
    {
        public readonly int Index;
        public readonly int Size;
        public readonly VertexAttribPointerType Type;
        public readonly bool Normalized;
        public readonly bool IsIntegral;

        private VertexArrayObjectElement(int index, int size, VertexAttribPointerType type, bool normalized, bool isIntegral)
        {
            Index = index;
            Size = size;
            Type = type;
            Normalized = normalized;
            IsIntegral = isIntegral;
        }

        public static VertexArrayObjectElement From(int index, int size, VertexAttribPointerType type)
        {
            return From(index, size, type, false);
        }

        public static VertexArrayObjectElement From(int index, int size, VertexAttribPointerType type, bool normalized)
        {
            return new VertexArrayObjectElement(index, size, type, normalized, false);
        }

        public static VertexArrayObjectElement FromIntegral(int index, int size, VertexAttribPointerType type)
        {
            return new VertexArrayObjectElement(index, size, type, false, true);
        }
    }
}
