﻿using Helion.Util.Geometry;
using System;

namespace Helion.Util
{
    public static class MathHelper
    {
        public const float QuarterPi = (float)(Math.PI / 4);
        public const float HalfPi = (float)(Math.PI / 2);
        public const float TwoPi = (float)(2 * Math.PI);

        public static bool IsZero(int i) => i == 0;
        public static bool IsZero(Fixed fixedPoint) => fixedPoint.Bits == 0;
        public static bool IsZero(Fixed fixedPoint, Fixed epsilon) => fixedPoint.Abs() < epsilon;
        public static bool IsZero(float f, float epsilon = 0.00001f) => Math.Abs(f) < epsilon;
        public static bool IsZero(double d, double epsilon = 0.000001) => Math.Abs(d) < epsilon;

        public static bool AreEqual(int first, int second) => first == second;
        public static bool AreEqual(Fixed first, Fixed second) => first == second;
        public static bool AreEqual(Fixed first, Fixed second, Fixed epsilon) => (first - second).Abs() < epsilon;
        public static bool AreEqual(float first, float second, float epsilon = 0.00001f) => Math.Abs(first - second) < epsilon;
        public static bool AreEqual(double first, double second, double epsilon = 0.000001) => Math.Abs(first - second) < epsilon;

        public static bool DifferentSign(int first, int second) => (first ^ second) < 0;
        public static bool DifferentSign(Fixed first, Fixed second) => (first.Bits ^ second.Bits) < 0;
        public static bool DifferentSign(float first, float second) => (first * second) < 0;
        public static bool DifferentSign(double first, double second) => (first * second) < 0;

        public static bool InNormalRange(float value) => value >= 0 && value <= 1;
        public static bool InNormalRange(double value) => value >= 0 && value <= 1;

        public static Fixed Min(Fixed first, Fixed second) => first.Bits < second.Bits ? first : second;
        public static Fixed Max(Fixed first, Fixed second) => first.Bits < second.Bits ? second : first;
    }
}
