namespace Helion.Maps.Specials
{
    /// <summary>
    /// A wrapper around five argument values that guarantees no out of range
    /// index errors.
    /// </summary>
    public class SpecialArgs
    {
        public const int TotalArgs = 5;
        
        private readonly byte[] m_args;

        public byte Arg0
        {
            get => m_args[0];
            set => m_args[0] = value;
        }

        public byte Arg1
        {
            get => m_args[1];
            set => m_args[1] = value;
        }
    
        public byte Arg2
        {
            get => m_args[2];
            set => m_args[2] = value;
        }
    
        public byte Arg3
        {
            get => m_args[3];
            set => m_args[3] = value;
        }
    
        public byte Arg4
        {
            get => m_args[4];
            set => m_args[4] = value;
        }

        public SpecialArgs(byte arg0 = 0, byte arg1 = 0, byte arg2 = 0, byte arg3 = 0, byte arg4 = 0)
        {
            m_args = new[] { arg0, arg1, arg2, arg3, arg4 };
        }
    }
}