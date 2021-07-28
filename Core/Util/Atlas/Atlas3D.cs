using System;
using System.Collections.Generic;
using Helion.Geometry;

namespace Helion.Util.Atlas
{
    /// <summary>
    /// A three dimensional atlas. This is best viewed as a list of 2D atlases.
    /// </summary>
    public class Atlas3D
    {
        public readonly Dimension Dimension;
        public readonly int Depth;
        private readonly List<Atlas2D> m_atlases = new();

        public Atlas3D(Dimension dimension, int depth)
        {
            Dimension = dimension;
            Depth = Math.Max(depth, 1);
            
            for (int d = 0; d < depth; d++)
                m_atlases.Add(new Atlas2D(dimension));
        }
        
        /// <summary>
        /// Tries to reserve space. If successful, returns the handle for it,
        /// otherwise it returns null if no such space could be allocated
        /// anywhere.
        /// </summary>
        /// <param name="area">The area to request.</param>
        /// <returns>The handle for the reserved area, or null if no space is
        /// available.</returns>
        public AtlasHandle? Add(Dimension area)
        {
            if (area.Width > Dimension.Width || area.Height > Dimension.Height)
                return null;

            for (int i = 0; i < Depth; i++)
            {
                AtlasHandle? handle = m_atlases[i].Add(area);
                if (handle != null)
                    return handle;
            }

            return null;
        }
    }
}
