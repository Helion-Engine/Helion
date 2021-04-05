// THIS FILE WAS AUTO-GENERATED.
// CHANGES WILL NOT BE PROPAGATED.
// ----------------------------------------------------------------------------

using System;

namespace Helion.Util.Extensions
{
    public static class PrimitiveExtensions
    {
        public static double Clamp(this byte value, byte low, byte high) => value < low ? low : (value > high ? high : value);

        public static (byte min, byte max) MinMax(byte first, byte second) => (first.Min(second), first.Max(second));

        public static byte Min(this byte self, byte other)
        {
            return Math.Min(self, other);
        }

        public static byte Max(this byte self, byte other)
        {
            return Math.Max(self, other);
        }

        public static bool DifferentSign(this short first, short second) => (first ^ second) < 0;

        public static double Clamp(this short value, short low, short high) => value < low ? low : (value > high ? high : value);

        public static (short min, short max) MinMax(short first, short second) => (first.Min(second), first.Max(second));

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

        public static double Clamp(this ushort value, ushort low, ushort high) => value < low ? low : (value > high ? high : value);

        public static (ushort min, ushort max) MinMax(ushort first, ushort second) => (first.Min(second), first.Max(second));

        public static ushort Min(this ushort self, ushort other)
        {
            return Math.Min(self, other);
        }

        public static ushort Max(this ushort self, ushort other)
        {
            return Math.Max(self, other);
        }

        public static bool DifferentSign(this int first, int second) => (first ^ second) < 0;

        public static double Clamp(this int value, int low, int high) => value < low ? low : (value > high ? high : value);

        public static (int min, int max) MinMax(int first, int second) => (first.Min(second), first.Max(second));

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

        public static double Clamp(this uint value, uint low, uint high) => value < low ? low : (value > high ? high : value);

        public static (uint min, uint max) MinMax(uint first, uint second) => (first.Min(second), first.Max(second));

        public static uint Min(this uint self, uint other)
        {
            return Math.Min(self, other);
        }

        public static uint Max(this uint self, uint other)
        {
            return Math.Max(self, other);
        }

        public static bool DifferentSign(this long first, long second) => (first ^ second) < 0;

        public static double Clamp(this long value, long low, long high) => value < low ? low : (value > high ? high : value);

        public static (long min, long max) MinMax(long first, long second) => (first.Min(second), first.Max(second));

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

        public static double Clamp(this ulong value, ulong low, ulong high) => value < low ? low : (value > high ? high : value);

        public static (ulong min, ulong max) MinMax(ulong first, ulong second) => (first.Min(second), first.Max(second));

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

        public static bool InNormalRange(this float value) => value >= 0 && value <= 1;

        public static double Clamp(this float value, float low, float high) => value < low ? low : (value > high ? high : value);

        public static (float min, float max) MinMax(float first, float second) => (first.Min(second), first.Max(second));

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

        public static bool InNormalRange(this double value) => value >= 0 && value <= 1;

        public static double Clamp(this double value, double low, double high) => value < low ? low : (value > high ? high : value);

        public static (double min, double max) MinMax(double first, double second) => (first.Min(second), first.Max(second));

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
