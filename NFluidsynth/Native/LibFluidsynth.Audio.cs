using System;
using System.Runtime.InteropServices;
using fluid_audio_driver_t_ptr = System.IntPtr;
using fluid_file_renderer_t_ptr = System.IntPtr;
using fluid_settings_t_ptr = System.IntPtr;
using fluid_synth_t_ptr = System.IntPtr;

namespace NFluidsynth.Native
{
    internal static unsafe partial class LibFluidsynth
    {
        internal delegate int fluid_audio_func_t(void* data, int len, int nin, IntPtr inBuffers, int nout,
            float** outBuffers);

        [DllImport(LibraryName)]
        internal static extern fluid_audio_driver_t_ptr new_fluid_audio_driver(fluid_settings_t_ptr settings,
            fluid_synth_t_ptr synth);

        [DllImport(LibraryName)]
        internal static extern fluid_audio_driver_t_ptr new_fluid_audio_driver2(fluid_settings_t_ptr settings,
            fluid_audio_func_t func, float** @out);

        [DllImport(LibraryName)]
        internal static extern void delete_fluid_audio_driver(fluid_audio_driver_t_ptr driver);

        [DllImport(LibraryName)]
        internal static extern fluid_file_renderer_t_ptr new_fluid_file_renderer(fluid_synth_t_ptr synth);

        [DllImport(LibraryName)]
        internal static extern int fluid_file_renderer_process_block(fluid_file_renderer_t_ptr dev);

        [DllImport(LibraryName)]
        internal static extern void delete_fluid_file_renderer(fluid_file_renderer_t_ptr dev);
    }
}
