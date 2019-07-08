using OpenTK.Graphics.OpenGL;

namespace Helion.Util.Extensions
{
    public static class TextureUnitExtensions
    {
        /// <summary>
        /// Converts the texture unit into a zero-based index.
        /// </summary>
        /// <param name="textureUnit">The texture unit index to get.</param>
        /// <returns></returns>
        public static int ToIndex(this TextureUnit textureUnit)
        {
            return (int)textureUnit - (int)TextureUnit.Texture0;
        }
    }
}