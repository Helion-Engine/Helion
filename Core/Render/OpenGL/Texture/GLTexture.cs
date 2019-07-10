using System.Numerics;
using Helion.Util.Atlas;
using Helion.Util.Geometry;
using static Helion.Util.Assert;

namespace Helion.Render.OpenGL.Texture
{
    /// <summary>
    /// A wrapper around a texture managed by a texture manager.
    /// </summary>
    /// <remarks>
    /// Its primary intent is to both hold onto a handle to which this was
    /// allocated with in an atlas object (so it can be deleted easily and to
    /// make it easier for reclaiming space) while containing all the UV data
    /// used for drawing it in the shaders.
    /// </remarks>
    public class GLTexture
    {
        /// <summary>
        /// An index used for lookup. This is intended to be the offset into
        /// some buffer in a shader.
        /// </summary>
        public readonly int TextureInfoIndex;
        
        /// <summary>
        /// The locations of the texture in UV coordinates on the atlas.
        /// </summary>
        public readonly Box2F UVLocation;
        
        /// <summary>
        /// The inverse U/V coordinates for the
        /// </summary>
        public readonly Vector2 UVInverse;
        
        /// <summary>
        /// The width and height of this texture.
        /// </summary>
        public readonly Dimension Dimension;
        
        /// <summary>
        /// The handle on the atlas for which this was allocated.
        /// </summary>
        /// <remarks>
        /// This is required for cleaning up when deleting the texture.
        /// </remarks>
        internal readonly AtlasHandle AtlasHandle;

        /// <summary>
        /// Creates a new texture from the data provided. This doesn't allocate
        /// a texture on the GPU, but rather encapsulates the location info for
        /// drawing.
        /// </summary>
        /// <param name="textureInfoIndex">A number used to look up the handle
        /// information in the shader. See <see cref="TextureInfoIndex"/>.
        /// </param>
        /// <param name="atlasDimension">The full atlas dimension.</param>
        /// <param name="atlasHandle">The handle this was allocated with.
        /// </param>
        public GLTexture(int textureInfoIndex, Dimension atlasDimension, AtlasHandle atlasHandle)
        {
            Precondition(atlasDimension.Width > 0 && atlasDimension.Height > 0, "Bad atlas dimensions (cannot divide by zero)");
            Precondition(textureInfoIndex >= 0, "Negative texture info indices not allowed (it's an array offset in the shader)");

            TextureInfoIndex = textureInfoIndex;
            UVLocation = CalculateUVLocation(atlasDimension, atlasHandle);
            UVInverse = UVLocation.Sides;
            Dimension = atlasHandle.Location.Dimension;
            AtlasHandle = atlasHandle;
        }

        private Box2F CalculateUVLocation(Dimension atlasDimension, AtlasHandle atlasHandle)
        {
            Vector2 min = atlasHandle.Location.BottomLeft.ToFloat();
            Vector2 max = atlasHandle.Location.TopRight.ToFloat();
            Vector2 dimension = atlasDimension.ToVector().ToFloat();
            
            return new Box2F(min / dimension, max / dimension);
        }
    }
}