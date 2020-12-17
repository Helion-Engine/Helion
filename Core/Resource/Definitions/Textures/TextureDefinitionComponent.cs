using Helion.Util;
using Helion.Util.Geometry.Vectors;

namespace Helion.Resource.Definitions.Textures
{
    /// <summary>
    /// A component of a texture definition, which is a sub-image in the image
    /// to be compiled.
    /// </summary>
    public class TextureDefinitionComponent
    {
        /// <summary>
        /// The name of the component.
        /// </summary>
        public CIString Name;

        /// <summary>
        /// The offset relative to the top left of the main texture definition.
        /// </summary>
        public Vec2I Offset;

        /// <summary>
        /// Creates a new texture definition component.
        /// </summary>
        /// <param name="name">The name of the component.</param>
        /// <param name="offset">The offsets relative to the top left of the
        /// texture definition image.</param>
        public TextureDefinitionComponent(CIString name, Vec2I offset)
        {
            Name = name;
            Offset = offset;
        }
    }
}