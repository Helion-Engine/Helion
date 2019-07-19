using System;
using Helion.Util.Geometry;

namespace Helion.Util
{
    /// <summary>
    /// A collection of helpers for mathematical functions and constants.
    /// </summary>
    public static class MathHelper
    {
        /// <summary>
        /// A constant value for pi / 4.
        /// </summary>
        public const double QuarterPi = Math.PI / 4;
        
        /// <summary>
        /// A constant value for pi / 2.
        /// </summary>
        public const double HalfPi = Math.PI / 2;
        
        /// <summary>
        /// A constant value for 2 * pi.
        /// </summary>
        public const double TwoPi = 2 * Math.PI;
        
        /// <summary>
        /// Checks if the value is equal to zero.
        /// </summary>
        /// <param name="fixedPoint">The value to check.</param>
        /// <returns>True if it does, false if not.</returns>
        public static bool IsZero(Fixed fixedPoint) => fixedPoint.Bits == 0;
        
        /// <summary>
        /// Checks if the value is equal to zero within some epsilon.
        /// </summary>
        /// <param name="fixedPoint">The value to check.</param>
        /// <param name="epsilon">The epsilon value.</param>
        /// <returns>True if it's within some epsilon, false otherwise.
        /// </returns>
        public static bool IsZero(Fixed fixedPoint, Fixed epsilon) => fixedPoint.Abs() < epsilon;
        
        /// <summary>
        /// Checks if the value is equal to zero within some epsilon.
        /// </summary>
        /// <param name="f">The value to check.</param>
        /// <param name="epsilon">The epsilon value.</param>
        /// <returns>True if it's within some epsilon, false otherwise.
        /// </returns>
        public static bool IsZero(float f, float epsilon = 0.00001f) => Math.Abs(f) < epsilon;
        
        /// <summary>
        /// Checks if the value is equal to zero within some epsilon.
        /// </summary>
        /// <param name="d">The value to check.</param>
        /// <param name="epsilon">The epsilon value.</param>
        /// <returns>True if it's within some epsilon, false otherwise.
        /// </returns>
        public static bool IsZero(double d, double epsilon = 0.00001) => Math.Abs(d) < epsilon;

        /// <summary>
        /// Checks if the values are equal to one another.
        /// </summary>
        /// <param name="first">The first value to compare.</param>
        /// <param name="second">The second value to compare.</param>
        /// <returns>True if they're equal, false if not.</returns>
        public static bool AreEqual(Fixed first, Fixed second) => first == second;

        /// <summary>
        /// Checks if the values are equal to one another within some epsilon.
        /// </summary>
        /// <param name="first">The first value to compare.</param>
        /// <param name="second">The second value to compare.</param>
        /// <param name="epsilon">The epsilon to check again.</param>
        /// <returns>True if they're equal within some epsilon, false if not.
        /// </returns>
        public static bool AreEqual(Fixed first, Fixed second, Fixed epsilon) => (first - second).Abs() < epsilon;
        
        /// <summary>
        /// Checks if the values are equal to one another within some epsilon.
        /// </summary>
        /// <param name="first">The first value to compare.</param>
        /// <param name="second">The second value to compare.</param>
        /// <param name="epsilon">The epsilon to check again.</param>
        /// <returns>True if they're equal within some epsilon, false if not.
        /// </returns>
        public static bool AreEqual(float first, float second, float epsilon = 0.00001f) => Math.Abs(first - second) < epsilon;
        
        /// <summary>
        /// Checks if the values are equal to one another within some epsilon.
        /// </summary>
        /// <param name="first">The first value to compare.</param>
        /// <param name="second">The second value to compare.</param>
        /// <param name="epsilon">The epsilon to check again.</param>
        /// <returns>True if they're equal within some epsilon, false if not.
        /// </returns>
        public static bool AreEqual(double first, double second, double epsilon = 0.000001) => Math.Abs(first - second) < epsilon;

        /// <summary>
        /// Checks if the signs are different.
        /// </summary>
        /// <param name="first">The first value to check.</param>
        /// <param name="second">The second value to check.</param>
        /// <returns>True if the signs are different, false if not.</returns>
        public static bool DifferentSign(int first, int second) => (first ^ second) < 0;
        
        /// <summary>
        /// Checks if the signs are different.
        /// </summary>
        /// <param name="first">The first value to check.</param>
        /// <param name="second">The second value to check.</param>
        /// <returns>True if the signs are different, false if not.</returns>
        public static bool DifferentSign(Fixed first, Fixed second) => (first.Bits ^ second.Bits) < 0;
        
        /// <summary>
        /// Checks if the signs are different.
        /// </summary>
        /// <param name="first">The first value to check.</param>
        /// <param name="second">The second value to check.</param>
        /// <returns>True if the signs are different, false if not.</returns>
        public static bool DifferentSign(float first, float second) => (first * second) < 0;
        
        /// <summary>
        /// Checks if the signs are different.
        /// </summary>
        /// <param name="first">The first value to check.</param>
        /// <param name="second">The second value to check.</param>
        /// <returns>True if the signs are different, false if not.</returns>
        public static bool DifferentSign(double first, double second) => (first * second) < 0;

        /// <summary>
        /// Checks if the value is in the [0.0, 1.0] range.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>True if it is, false if not.</returns>
        public static bool InNormalRange(float value) => value >= 0 && value <= 1;
        
        /// <summary>
        /// Checks if the value is in the [0.0, 1.0] range.
        /// </summary>
        /// <param name="value">The value to check.</param>
        /// <returns>True if it is, false if not.</returns>
        public static bool InNormalRange(double value) => value >= 0 && value <= 1;

        /// <summary>
        /// Gets the minimum value of two fixed point numbers.
        /// </summary>
        /// <param name="first">The first number.</param>
        /// <param name="second">The second number.</param>
        /// <returns>The minimum value of both.</returns>
        public static Fixed Min(Fixed first, Fixed second) => first.Bits < second.Bits ? first : second;
        
        /// <summary>
        /// Gets the maximum value of two fixed point numbers.
        /// </summary>
        /// <param name="first">The first number.</param>
        /// <param name="second">The second number.</param>
        /// <returns>The maximum value of both.</returns>
        public static Fixed Max(Fixed first, Fixed second) => first.Bits < second.Bits ? second : first;
    }
}
