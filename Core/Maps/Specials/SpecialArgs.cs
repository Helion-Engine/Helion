using System;

namespace Helion.Maps.Specials
{
    /// <summary>
    /// A wrapper around five argument values that guarantees no out of range
    /// index errors.
    /// </summary>
    /// <remarks>
    /// This is a mutable data structure.
    /// </remarks>
    public struct SpecialArgs
    {
        public byte Arg0;
        public byte Arg1;
        public byte Arg2;
        public byte Arg3;
        public byte Arg4;

        public SpecialArgs(byte arg0 = 0, byte arg1 = 0, byte arg2 = 0, byte arg3 = 0, byte arg4 = 0)
        {
            Arg0 = arg0;
            Arg1 = arg1;
            Arg2 = arg2;
            Arg3 = arg3;
            Arg4 = arg4;
        }

        public SpecialArgs(SpecialArgs other)
        {
            Arg0 = other.Arg0;
            Arg1 = other.Arg1;
            Arg2 = other.Arg2;
            Arg3 = other.Arg3;
            Arg4 = other.Arg4;
        }
    }
}