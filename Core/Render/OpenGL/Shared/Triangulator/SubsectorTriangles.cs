using System.Collections.Generic;

namespace Helion.Render.OpenGL.Shared.Triangulator
{
    /// <summary>
    /// A fan of triangles that make up the floor and ceiling polygon for a
    /// subsector.
    /// </summary>
    public class SubsectorTriangles
    {
        /// <summary>
        /// A counter-clockwise fan of triangles that make up the floor, with
        /// the assumption that the camera is above it on the Z axis.
        /// </summary>
        public List<Triangle> Floor = new List<Triangle>();

        /// <summary>
        /// A clockwise fan of triangles that make up the ceiling, with the
        /// assumption that the camera is underneath it on the Z axis.
        /// </summary>
        public List<Triangle> Ceiling = new List<Triangle>();
    }
}
