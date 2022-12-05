using System;
using System.Collections.Generic;
using System.Numerics;

namespace Helion.GeometryNew;

public static class MathHelper
{
    public static T Abs<T>(this T value) where T : INumber<T>
    {
        return T.Abs(value);
    }

    public static T Min<T>(this T value, T other) where T : INumber<T>
    {
        return T.Min(value, other);
    }

    public static T Max<T>(this T value, T other) where T : INumber<T>
    {
        return T.Max(value, other);
    }

    public static (T Min, T Max) MinMax<T>(this T first, T second) where T : INumber<T>
    {
        return first < second ? (first, second) : (second, first);
    }

    public static T Clamp<T>(this T value, T low, T high) where T : INumber<T>
    {
        return T.Clamp(value, low, high);
    }


    public static bool IsZero<T>(this T value) where T : IFloatingPoint<T>
    {
        return IsZero(value, T.Epsilon);
    }

    public static bool IsZero<T>(this T value, T epsilon) where T : IFloatingPoint<T>
    {
        return T.Abs(value) < epsilon;
    }

    public static bool IsApprox<T>(this T first, T second) where T : IFloatingPoint<T>
    {
        return T.Abs(first - second) < T.Epsilon;
    }

    public static bool IsApprox<T>(this T first, T second, T epsilon) where T : IFloatingPoint<T>
    {
        return T.Abs(first - second) < epsilon;
    }

    public static bool DifferentSign<T>(this T first, T second) where T : IFloatingPoint<T>
    {
        return first * second < T.Zero;
    }

    public static T Lerp<T, F>(this T start, T end, F t)
        where T : IAdditionOperators<T, T, T>, ISubtractionOperators<T, T, T>, IMultiplyOperators<T, F, T>
        where F : IFloatingPoint<F>
    {
        return start + ((end - start) * t);
    }
}
