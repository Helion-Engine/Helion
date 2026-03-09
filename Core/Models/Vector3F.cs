using Helion.Geometry.Vectors;

namespace Helion.Models
{
    public struct Vector3F
    {
        public float X;
        public float Y;
        public float Z;

        public Vector3F()
        {

        }

        public Vector3F(float x, float y, float z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        public Vector3F(Vec3F v)
        {
            X = v.X;
            Y = v.Y;
            Z = v.Z;
        }
    }
}
