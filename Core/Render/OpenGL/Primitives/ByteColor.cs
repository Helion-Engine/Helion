using System;
using System.Drawing;
using System.Runtime.InteropServices;

namespace Helion.Render.OpenGL.Primitives
{
    [StructLayout(LayoutKind.Sequential, Pack = 1)]
    public readonly struct ByteColor
    {
        public readonly byte R;
        public readonly byte G;
        public readonly byte B;
        public readonly byte A;

        public ByteColor(byte r, byte g, byte b, byte a = 255)
        {
            R = r;
            G = g;
            B = b;
            A = a;
        }

        public ByteColor(Color color) : this(color.R, color.G, color.B, color.A)
        {
        }
        
        public static implicit operator ByteColor(ValueTuple<byte, byte, byte, byte> tuple)
        {
            return new(tuple.Item1, tuple.Item2, tuple.Item3, tuple.Item4);
        }
        
        public override string ToString() => $"{R}, {G}, {B}, {A}";
    }
}
