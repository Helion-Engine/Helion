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
            Depth = depth;
            
            for (int d = 0; d < depth; d++)
                m_atlases.Add(new Atlas2D(dimension));
        }
        
        // TODO
    }
}
