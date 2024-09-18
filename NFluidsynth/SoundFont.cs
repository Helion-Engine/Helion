using System;
using NFluidsynth.Native;

namespace NFluidsynth
{
    public class SoundFont : FluidsynthObject
    {
        public static bool IsSoundFont(string filename)
        {
            return LibFluidsynth.fluid_is_soundfont(filename) == 1;
        }

        public static bool IsMidiFile(string filename)
        {
            return LibFluidsynth.fluid_is_midifile(filename) == 1;
        }

        protected internal SoundFont(IntPtr handle)
            : base(handle)
        {
        }
    }
}
