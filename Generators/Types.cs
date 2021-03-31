using System;

namespace Generators
{
    public enum Types
    {
        Byte,
        Short,
        UShort,
        Int,
        UInt,
        Long,
        ULong,
        Float,
        Double,
        Fixed
    }

    public static class TypesExtensions
    {
        public static bool IsFloatingPointPrimitive(this Types type)
        {
            return type == Types.Float || type == Types.Double;
        }

        public static bool IsUnsigned(this Types type)
        {
            switch (type)
            {
            case Types.Byte:
            case Types.UShort:
            case Types.UInt:
            case Types.ULong:
                return true;
            default:
                return false;
            }
        }

        public static string PrimitiveType(this Types type)
        {
            return type switch
            {
                Types.Int => "int",
                Types.Float => "float",
                Types.Double => "double",
                Types.Fixed => "Fixed",
                Types.Byte => "Byte",
                Types.Short => "Short",
                Types.UShort => "UShort",
                Types.UInt => "UInt",
                Types.Long => "Long",
                Types.ULong => "ULong",
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
