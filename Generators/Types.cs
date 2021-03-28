using System;

namespace Generators
{
    public enum Types
    {
        Int,
        Float,
        Double,
        Fixed
    }

    public static class TypesExtensions
    {
        public static bool IsFloatingPoint(this Types type)
        {
            return IsFloatingPoint(type) || type == Types.Fixed;
        }
        
        public static bool IsFloatingPointPrimitive(this Types type)
        {
            return type == Types.Float || type == Types.Double;
        }

        public static string PrimitiveType(this Types type)
        {
            return type switch
            {
                Types.Int => "int",
                Types.Float => "float",
                Types.Double => "double",
                Types.Fixed => "Fixed",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }

        public static string GetShorthand(this Types type)
        {
            return type switch
            {
                Types.Int => "I",
                Types.Float => "F",
                Types.Double => "D",
                Types.Fixed => "Fixed",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }
}
