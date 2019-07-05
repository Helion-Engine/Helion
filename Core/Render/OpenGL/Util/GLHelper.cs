using Helion.Util;
using OpenTK.Graphics.OpenGL;
using System.Diagnostics;

namespace Helion.Render.OpenGL.Util
{
    public static class GLHelper
    {
        /// <summary>
        /// Gets the length in bytes for the VAO element.
        /// </summary>
        /// <param name="vaoElement">The element to get the byte length of.
        /// </param>
        /// <returns>The bytes needed for the element.</returns>
        public static int ToByteLength(VertexAttribPointerType type)
        {
            switch (type)
            {
            case VertexAttribPointerType.Byte:
                return 1;
            case VertexAttribPointerType.UnsignedByte:
                return 1;
            case VertexAttribPointerType.Short:
                return 2;
            case VertexAttribPointerType.UnsignedShort:
                return 2;
            case VertexAttribPointerType.Int:
                return 4;
            case VertexAttribPointerType.UnsignedInt:
                return 4;
            case VertexAttribPointerType.Float:
                return 4;
            case VertexAttribPointerType.Double:
                return 8;
            case VertexAttribPointerType.HalfFloat:
                return 2;
            case VertexAttribPointerType.Fixed:
                return 4;
            case VertexAttribPointerType.UnsignedInt2101010Rev:
                return 4;
            case VertexAttribPointerType.UnsignedInt10F11F11FRev:
                return 4;
            case VertexAttribPointerType.Int2101010Rev:
                return 4;
            default:
                throw new HelionException("Unknown vertex attribute pointer type");
            }
        }

        /// <summary>
        /// Gets the length in bytes for the VAO element.
        /// </summary>
        /// <param name="vaoElement">The element to get the byte length of.
        /// </param>
        /// <returns>The bytes needed for the element.</returns>
        public static int ToByteLength(VertexAttribIntegerType type)
        {
            switch (type)
            {
            case VertexAttribIntegerType.Byte:
                return 1;
            case VertexAttribIntegerType.UnsignedByte:
                return 1;
            case VertexAttribIntegerType.Short:
                return 2;
            case VertexAttribIntegerType.UnsignedShort:
                return 2;
            case VertexAttribIntegerType.Int:
                return 4;
            case VertexAttribIntegerType.UnsignedInt:
                return 4;
            default:
                throw new HelionException("Unknown integer vertex attribute pointer type");
            }
        }

        /// <summary>
        /// Takes a texture filter for a minimum value and returns a maximum
        /// value that matches best with the filter provided.
        /// </summary>
        /// <param name="filter">The minimum filter value.</param>
        /// <returns>The appropriate maximum filter value.</returns>
        public static TextureMagFilter MinToMagFilter(TextureMinFilter filter) {
            switch (filter)
            {
            case TextureMinFilter.Nearest:
            case TextureMinFilter.NearestMipmapLinear:
            case TextureMinFilter.NearestMipmapNearest:
                return TextureMagFilter.Nearest;
            default:
                return TextureMagFilter.Linear;
            }
        }

        /// <summary>
        /// Attaches an object label for the provided GL object.
        /// </summary>
        /// <param name="id">The GL identifier.</param>
        /// <param name="glName">The integral GL object name.</param>
        /// <param name="name">The label to attach.</param>
        [Conditional("DEBUG")]
        public static void ObjectLabel(ObjectLabelIdentifier id, int glName, string name)
        {
            GL.ObjectLabel(ObjectLabelIdentifier.Texture, glName, name.Length, name);
        }
    }
}
