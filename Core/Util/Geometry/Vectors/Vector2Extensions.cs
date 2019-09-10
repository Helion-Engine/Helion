using System;
using System.Numerics;

namespace Helion.Util.Geometry.Vectors
{
    // TODO: Can we invoke intrinsic functions?
    public static class Vector2Extensions
    {
        public static float U(this Vector2 vec) => vec.X;
        public static float V(this Vector2 vec) => vec.Y;
        public static Vector2 Abs(this Vector2 vec) => new Vector2(Math.Abs(vec.X), Math.Abs(vec.Y));
        public static float Dot(this Vector2 vec, Vector2 other) => (vec.X * other.X) + (vec.Y * other.Y);
        public static Vector2 Unit(this Vector2 vec) => vec / vec.Length();
        public static float LengthSquared(this Vector2 vec) => (vec.X * vec.X) + (vec.Y * vec.Y);
        public static float Length(this Vector2 vec) => (float)Math.Sqrt((vec.X * vec.X) + (vec.Y * vec.Y));
        public static float DistanceSquared(this Vector2 vec, Vector2 other) => (vec - other).LengthSquared();
        public static float Distance(this Vector2 vec, Vector2 other) => (vec - other).Length();
        public static float Component(this Vector2 vec, Vector2 onto) => vec.Dot(onto) / onto.Length();
        public static Vector2 Projection(this Vector2 vec, Vector2 onto) => vec.Dot(onto) / onto.LengthSquared() * onto;
        public static Vector2 Interpolate(this Vector2 start, Vector2 end, float t) => start + (t * (end - start));
        public static Vec2I ToInt(this Vector2 vec) => new Vec2I((int)vec.X, (int)vec.Y);
        public static Vec2Fixed ToFixed(this Vector2 vec) => new Vec2Fixed(new Fixed(vec.X), new Fixed(vec.Y));
        public static Vec2D ToDouble(this Vector2 vec) => new Vec2D(vec.X, vec.Y);
        public static Vector2 OriginRightRotate90(this Vector2 vec) => new Vector2(vec.Y, -vec.X);
        public static Vector2 OriginLeftRotate90(this Vector2 vec) => new Vector2(-vec.Y, vec.X);

        public static bool EqualTo(this Vector2 vec, Vector2 other, float epsilon = 0.00001f)
        {
            return MathHelper.AreEqual(vec.X, other.X, epsilon) && MathHelper.AreEqual(vec.Y, other.Y, epsilon);
        }

        public static void Deconstruct(this Vector2 vec, out float x, out float y)
        {
            x = vec.X;
            y = vec.Y;
        }
    }
}