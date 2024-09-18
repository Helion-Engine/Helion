using System;
using System.Runtime.InteropServices;

namespace NFluidsynth.Native
{
    internal static partial class LibFluidsynth
    {
        [DllImport(LibraryName)]
        internal static extern int fluid_is_soundfont([MarshalAs(LP_Str)] string filename);

        [DllImport(LibraryName)]
        internal static extern int fluid_is_midifile([MarshalAs(LP_Str)] string filename);

        [DllImport(LibraryName)]
        internal static extern IntPtr fluid_set_log_function(int severity, Logger.LoggerDelegate func, IntPtr data);
    }
}
