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
            return type is Types.Float or Types.Double;
        }

        public static int SizeOf(this Types type)
        {
            switch (type)
            {
            case Types.Byte:
                return 1;
            case Types.Short:
            case Types.UShort:
                return 2;
            case Types.Int:
            case Types.UInt:
            case Types.Float:
            case Types.Fixed:
                return 4;
            case Types.Long:
            case Types.ULong:
            case Types.Double:
                return 8;
            default:
                throw new ArgumentOutOfRangeException(nameof(type), type, null);
            }
        }
        
        public static bool IsIntegralPrimitive(this Types type)
        {
            switch (type)
            {
            case Types.Byte:
            case Types.Short:
            case Types.UShort:
            case Types.Int:
            case Types.UInt:
            case Types.Long:
            case Types.ULong:
                return true;
            default:
                return false;
            }
        }

        public static bool IsSigned(this Types type)
        {
            switch (type)
            {
            case Types.Short:
            case Types.Int:
            case Types.Long:
            case Types.Fixed:
            case Types.Float:
            case Types.Double:
                return true;
            default:
                return false;
            }
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
                Types.Byte => "byte",
                Types.Short => "short",
                Types.UShort => "ushort",
                Types.UInt => "uint",
                Types.Long => "long",
                Types.ULong => "ulong",
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
        
        public static string CommonName(this Types type)
        {
            return type switch
            {
                Types.Int => "Int",
                Types.Float => "Float",
                Types.Double => "Double",
                Types.Fixed => "FixedPoint",
                _ => throw new ArgumentOutOfRangeException(nameof(type), type, null)
            };
        }
    }
}
