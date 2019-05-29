using OpenTK.Graphics.OpenGL;
using System.Diagnostics;

namespace Helion.Render.OpenGL.Shared
{
    public static class GLHelper
    {
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
