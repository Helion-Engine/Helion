using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using fluid_settings_t_ptr = System.IntPtr;

namespace NFluidsynth.Native
{
    [SuppressMessage("ReSharper", "IdentifierTypo")]
    internal static unsafe partial class LibFluidsynth
    {
        internal delegate IntPtr fluid_settings_foreach_option_t(void* data, byte* name, byte* option);

        internal delegate IntPtr fluid_settings_foreach_t(void* data, byte* name, FluidTypes type);

        [DllImport(LibraryName)]
        internal static extern fluid_settings_t_ptr new_fluid_settings();

        [DllImport(LibraryName)]
        internal static extern void delete_fluid_settings(fluid_settings_t_ptr settings);

        [DllImport(LibraryName)]
        internal static extern FluidTypes fluid_settings_get_type(fluid_settings_t_ptr settings,
            [MarshalAs(LP_Str)] string name);

        [DllImport(LibraryName)]
        internal static extern int fluid_settings_get_hints(fluid_settings_t_ptr settings,
            [MarshalAs(LP_Str)] string name,
            out FluidHint hints);

        [DllImport(LibraryName)]
        [return: MarshalAs(UnmanagedType.I4)]
        internal static extern bool fluid_settings_is_realtime(fluid_settings_t_ptr settings,
            [MarshalAs(LP_Str)] string name);

        [DllImport(LibraryName)]
        internal static extern int fluid_settings_setstr(fluid_settings_t_ptr settings,
            [MarshalAs(LP_Str)] string name, [MarshalAs(LP_Str)] string str);

        [DllImport(LibraryName)]
        internal static extern int fluid_settings_copystr(fluid_settings_t_ptr settings,
            [MarshalAs(LP_Str)] string name, byte* str,
            int len);

        // There is no chance to release the returned pointer, so do not bind it.
        // This is literally guaranteed to leak memory. There is nothing we can do. Blame fluidsynth.
        [DllImport(LibraryName)]
        internal static extern int fluid_settings_dupstr(fluid_settings_t_ptr settings,
            [MarshalAs(LP_Str)] string name, out byte* str);

        [DllImport(LibraryName)]
        internal static extern int fluid_settings_getstr_default(fluid_settings_t_ptr settings,
            [MarshalAs(LP_Str)] string name,
            out byte* str);

        [DllImport(LibraryName)]
        [return: MarshalAs(UnmanagedType.I4)]
        internal static extern bool fluid_settings_str_equal(fluid_settings_t_ptr settings,
            [MarshalAs(LP_Str)] string name, [MarshalAs(LP_Str)] string s);

        [DllImport(LibraryName)]
        internal static extern int fluid_settings_setnum(fluid_settings_t_ptr settings,
            [MarshalAs(LP_Str)] string name, double val);

        [DllImport(LibraryName)]
        internal static extern int fluid_settings_getnum(fluid_settings_t_ptr settings,
            [MarshalAs(LP_Str)] string name, out double val);

        [DllImport(LibraryName)]
        internal static extern int fluid_settings_getnum_default(fluid_settings_t_ptr settings,
            [MarshalAs(LP_Str)] string name,
            out double val);

        [DllImport(LibraryName)]
        internal static extern int fluid_settings_getnum_range(fluid_settings_t_ptr settings,
            [MarshalAs(LP_Str)] string name,
            out double min, out double max);

        [DllImport(LibraryName)]
        internal static extern int fluid_settings_setint(fluid_settings_t_ptr settings,
            [MarshalAs(LP_Str)] string name, int val);

        [DllImport(LibraryName)]
        internal static extern int fluid_settings_getint(fluid_settings_t_ptr settings,
            [MarshalAs(LP_Str)] string name, out int val);

        [DllImport(LibraryName)]
        internal static extern int fluid_settings_getint_default(fluid_settings_t_ptr settings,
            [MarshalAs(LP_Str)] string name,
            out int val);

        [DllImport(LibraryName)]
        internal static extern int fluid_settings_getint_range(fluid_settings_t_ptr settings,
            [MarshalAs(LP_Str)] string name, out int min,
            out int max);

        [DllImport(LibraryName)]
        internal static extern void fluid_settings_foreach_option(fluid_settings_t_ptr settings,
            [MarshalAs(LP_Str)] string name,
            IntPtr data, fluid_settings_foreach_option_t func);

        [DllImport(LibraryName)]
        internal static extern int fluid_settings_option_count(fluid_settings_t_ptr settings,
            [MarshalAs(LP_Str)] string name);

        // note: the returned value has to be released.
        [DllImport(LibraryName)]
        internal static extern byte* fluid_settings_option_concat(fluid_settings_t_ptr settings,
            [MarshalAs(LP_Str)] string name,
            string separator);

        [DllImport(LibraryName)]
        internal static extern void fluid_settings_foreach(fluid_settings_t_ptr settings,
            [MarshalAs(LP_Str)] string name, void* data,
            fluid_settings_foreach_t func);
    }
}