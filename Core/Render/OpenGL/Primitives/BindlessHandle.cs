using System.Runtime.InteropServices;

namespace Helion.Render.OpenGL.Primitives
{
    [StructLayout(LayoutKind.Sequential, Pack = 4)]
    public readonly struct BindlessHandle
    {
        // These are ordered this way since we pass it to a function in the
        // shader that packs it properly. The specs of the function packUint2x32
        // say "The first vector component specifies the 32 least significant bits"
        // and "the second component specifies the 32 most significant bits".
        public readonly uint Lower;
        public readonly uint Upper;

        public BindlessHandle(ulong handle)
        {
            Upper = (uint)((handle & 0xFFFFFFFF00000000U) >> 32);
            Lower = (uint)(handle & 0x00000000FFFFFFFFU);
        }

        public BindlessHandle(uint upper, uint lower)
        {
            Upper = upper;
            Lower = lower;
        }

        public override string ToString() => $"{Upper}:{Lower}";
    }
}
