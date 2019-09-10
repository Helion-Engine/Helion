using System;
using System.Numerics;
using GlmSharp;

namespace Helion.Util.Geometry.Vectors
{
    public static class Vector3Extensions
    {
        public static bool EqualTo(this Vector3 self, Vector3 other, float epsilon = 0.00001f)
        {
            return MathHelper.AreEqual(self.X, other.X, epsilon) && 
                   MathHelper.AreEqual(self.Y, other.Y, epsilon) && 
                   MathHelper.AreEqual(self.Z, other.Z, epsilon);
        }

        public static Vector3 Abs(this Vector3 vec) => new Vector3(Math.Abs(vec.X), Math.Abs(vec.Y), Math.Abs(vec.Z));
        public static Vector3 Unit(this Vector3 vec) => vec / vec.Length();
        public static Vector3 Interpolate(this Vector3 start, Vector3 end, float t) => start + (t * (end - start));
        public static Vector2 To2D(this Vector3 vec) => new Vector2(vec.X, vec.Y);
        public static Vec3I ToInt(this Vector3 vec) => new Vec3I((int)vec.X, (int)vec.Y, (int)vec.Z);
        public static Vec3Fixed ToFixed(this Vector3 vec) => new Vec3Fixed(new Fixed(vec.X), new Fixed(vec.Y), new Fixed(vec.Z));
        public static Vec3D ToDouble(this Vector3 vec) => new Vec3D(vec.X, vec.Y, vec.Z);
        public static vec3 ToGlmVector(this Vector3 vec) => new vec3(vec.X, vec.Y, vec.Z);
    }
}
