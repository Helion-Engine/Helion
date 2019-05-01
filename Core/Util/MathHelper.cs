using Helion.Util.Geometry;
using System;

namespace Helion.Util
{
    public static class MathHelper
    {
        public static bool IsZero(int i) => i == 0;
        public static bool IsZero(Fixed fixedPoint) => fixedPoint == 0;
        public static bool IsZero(float f) => Math.Abs(f) < 0.00001f;
        public static bool IsZero(double d) => Math.Abs(d) < 0.000001;

        public static bool AreEqual(int first, int second) => first == second;
        public static bool AreEqual(Fixed first, Fixed second) => first == second;
        public static bool AreEqual(float first, float second) => Math.Abs(first - second) < 0.00001f;
        public static bool AreEqual(double first, double second) => Math.Abs(first - second) < 0.000001;
    }
}
