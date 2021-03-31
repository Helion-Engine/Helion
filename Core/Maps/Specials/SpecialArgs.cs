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
        public int Arg0;
        public int Arg1;
        public int Arg2;
        public int Arg3;
        public int Arg4;

        public SpecialArgs(int arg0 = 0, int arg1 = 0, int arg2 = 0, int arg3 = 0, int arg4 = 0)
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