using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Render.OpenGL.Capabilities;
using Helion.Util.Atlas;
using System;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Render.OpenGL.Textures.Types;
using Helion.Render.OpenGL.Util;

namespace Helion.Render.OpenGL.Textures.Legacy
{
    public class AtlasGLTexture : GLTexture2D
    {
        /// <summary>
        /// The maximum size of the atlas. This is to prevent two things. First
        /// is allocating a ton of space we'd never use since some GPUs have a
        /// maximum size at 32k. Another is to make mipmap generation possibly
        /// less painful.
        /// </summary>
        private const int MaxAtlasDimension = 4096;
        
        private readonly Atlas2D m_atlas;
        
        public AtlasGLTexture(string debugName, Dimension? dimension = null) : 
            base(debugName, ClampDimension(dimension))
        {
            m_atlas = new Atlas2D(Dimension);
        }

        /// <summary>
        /// Clamps the dimension based on the following: If it's null, then we
        /// pick the largest viable dimension. If it's not null, we use that
        /// but clamp it in the range of (1, Max).
        /// </summary>
        /// <param name="dimension">The dimension to use, or null if it should
        /// use the max reasonable dimensions.</param>
        /// <returns>The dimension to use.</returns>
        private static Dimension ClampDimension(Dimension? dimension)
        {
            int maxDim = Math.Min(GLCapabilities.Limits.MaxTexture2DSize, MaxAtlasDimension);
            int w = Math.Max(1, dimension?.Width ?? maxDim);
            int h = Math.Max(1, dimension?.Height ?? maxDim);
            return (w, h);
        }

        public bool TryUpload(Image image, out Box2I area, Mipmap mipmap, Binding bind)
        {
            area = default;
            
            AtlasHandle? atlasHandle = m_atlas.Add(image.Dimension);
            if (atlasHandle == null)
                return false;

            Vec2I origin = atlasHandle.Location.Min;
            Upload(origin, image, mipmap, bind);
            
            area = atlasHandle.Location;
            return true;
        }
    }
}
