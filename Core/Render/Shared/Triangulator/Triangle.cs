using System.Numerics;

namespace Helion.Render.Shared.Triangulator
{
    /// <summary>
    /// A simple triangle that is the result of a triangulation of some world
    /// component. The vertices are to be in counter-clockwise order.
    /// </summary>
    public struct Triangle
    {
        public Vector3 First;
        public Vector3 Second;
        public Vector3 Third;

        public Triangle(Vector3 first, Vector3 second, Vector3 third)
        {
            First = first;
            Second = second;
            Third = third;
        }
    }
}
