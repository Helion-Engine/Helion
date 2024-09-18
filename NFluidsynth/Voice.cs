using System;

namespace NFluidsynth
{
    public class Voice : FluidsynthObject
    {
        protected internal Voice(IntPtr handle)
            : base(handle)
        {
        }
    }
}