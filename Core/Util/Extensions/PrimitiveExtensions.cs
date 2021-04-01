// THIS FILE WAS AUTO-GENERATED.
// CHANGES WILL NOT BE PROPAGATED.
// ----------------------------------------------------------------------------

using System;

namespace Helion.Util.Extensions
{
    public static class PrimitiveExtensions
    {
        public static byte Min(this byte self, byte other)
        {
            return Math.Min(self, other);
        }

        public static byte Max(this byte self, byte other)
        {
            return Math.Max(self, other);
        }

        public static bool DifferentSign(this short first, short second) => (first ^ second) < 0;

        public static short Min(this short self, short other)
        {
            return Math.Min(self, other);
        }

        public static short Max(this short self, short other)
        {
            return Math.Max(self, other);
        }

        public static short Abs(this short self)
        {
            return Math.Abs(self);
        }

        public static ushort Min(this ushort self, ushort other)
        {
            return Math.Min(self, other);
        }

        public static ushort Max(this ushort self, ushort other)
        {
            return Math.Max(self, other);
        }

        public static bool DifferentSign(this int first, int second) => (first ^ second) < 0;

        public static int Min(this int self, int other)
        {
            return Math.Min(self, other);
        }

        public static int Max(this int self, int other)
        {
            return Math.Max(self, other);
        }

        public static int Abs(this int self)
        {
            return Math.Abs(self);
        }

        public static uint Min(this uint self, uint other)
        {
            return Math.Min(self, other);
        }

        public static uint Max(this uint self, uint other)
        {
            return Math.Max(self, other);
        }

        public static bool DifferentSign(this long first, long second) => (first ^ second) < 0;

        public static long Min(this long self, long other)
        {
            return Math.Min(self, other);
        }

        public static long Max(this long self, long other)
        {
            return Math.Max(self, other);
        }

        public static long Abs(this long self)
        {
            return Math.Abs(self);
        }

        public static ulong Min(this ulong self, ulong other)
        {
            return Math.Min(self, other);
        }

        public static ulong Max(this ulong self, ulong other)
        {
            return Math.Max(self, other);
        }

        public static bool ApproxEquals(this float value, float target, float epsilon = 0.0001f)
        {
            return value >= target - epsilon && value <= target + epsilon;
        }

        public static bool ApproxZero(this float value, float epsilon = 0.0001f)
        {
            return value.ApproxEquals(0, epsilon);
        }

        public static float Interpolate(this float start, float end, float t)
        {
            return start + (t * (end - start));
        }

        public static float Floor(this float self)
        {
            return MathF.Floor(self);
        }

        public static float Ceiling(this float self)
        {
            return MathF.Ceiling(self);
        }

        public static bool DifferentSign(this float first, float second) => (first * second) < 0;

        public static float Min(this float self, float other)
        {
            return Math.Min(self, other);
        }

        public static float Max(this float self, float other)
        {
            return Math.Max(self, other);
        }

        public static float Abs(this float self)
        {
            return Math.Abs(self);
        }

        public static bool ApproxEquals(this double value, double target, double epsilon = 0.00001)
        {
            return value >= target - epsilon && value <= target + epsilon;
        }

        public static bool ApproxZero(this double value, double epsilon = 0.00001)
        {
            return value.ApproxEquals(0, epsilon);
        }

        public static double Interpolate(this double start, double end, double t)
        {
            return start + (t * (end - start));
        }

        public static double Floor(this double self)
        {
            return Math.Floor(self);
        }

        public static double Ceiling(this double self)
        {
            return Math.Ceiling(self);
        }

        public static bool DifferentSign(this double first, double second) => (first * second) < 0;

        public static double Min(this double self, double other)
        {
            return Math.Min(self, other);
        }

        public static double Max(this double self, double other)
        {
            return Math.Max(self, other);
        }

        public static double Abs(this double self)
        {
            return Math.Abs(self);
        }

    }
}
