using System;
using System.Runtime.InteropServices;
using fluid_synth_t_ptr = System.IntPtr;
using fluid_preset_t_ptr = System.IntPtr;
using fluid_sfont_t_ptr = System.IntPtr;
using fluid_sfloader_t_ptr = System.IntPtr;
using fluid_sample_t_ptr = System.IntPtr;

namespace NFluidsynth.Native
{
    internal static unsafe partial class LibFluidsynth
    {
        internal delegate fluid_sfont_t_ptr fluid_sfloader_load_t(fluid_sfloader_t_ptr loader, string filename);

        internal delegate void fluid_sfloader_free_t(fluid_sfloader_t_ptr loader);

        [DllImport(LibraryName)]
        internal static extern fluid_sfloader_t_ptr new_fluid_sfloader(fluid_sfloader_load_t load,
            fluid_sfloader_free_t free);

        [DllImport(LibraryName)]
        internal static extern fluid_sfloader_t_ptr new_fluid_defsfloader(IntPtr settings);

        [DllImport(LibraryName)]
        internal static extern void delete_fluid_sfloader(fluid_sfloader_t_ptr loader);

        internal delegate IntPtr fluid_sfloader_callback_open_t(string filename);

        internal delegate int fluid_sfloader_callback_read_t_2(IntPtr buf, int count, IntPtr handle);

        internal delegate int fluid_sfloader_callback_read_t_3(IntPtr buf, long count, IntPtr handle);

        internal delegate int fluid_sfloader_callback_seek_t_2(IntPtr handle, int offset, int origin);

        internal delegate int fluid_sfloader_callback_seek_t_3(IntPtr handle, long offset, int origin);

        internal delegate int fluid_sfloader_callback_close_t(IntPtr handle);

        internal delegate int fluid_sfloader_callback_tell_t_2(IntPtr handle);

        internal delegate long fluid_sfloader_callback_tell_t_3(IntPtr handle);

        [DllImport(LibraryName)]
        internal static extern int fluid_sfloader_set_callbacks(fluid_sfloader_t_ptr loader,
            IntPtr open, IntPtr read, IntPtr seek, IntPtr tell, IntPtr close);

        [DllImport(LibraryName)]
        internal static extern int fluid_sfloader_set_data(fluid_sfloader_t_ptr loader, IntPtr data);

        [DllImport(LibraryName)]
        internal static extern IntPtr fluid_sfloader_get_data(fluid_sfloader_t_ptr loader);

        internal delegate string fluid_sfont_get_name_t(fluid_sfont_t_ptr sfont);

        internal delegate fluid_preset_t_ptr fluid_sfont_get_preset_t(fluid_sfont_t_ptr sfont, uint bank, uint prenum);

        internal delegate int fluid_sfont_free_t(fluid_sfont_t_ptr sfont);

        [DllImport(LibraryName)]
        internal static extern fluid_sfont_t_ptr new_fluid_sfont(fluid_sfont_get_name_t get_name,
            fluid_sfont_get_preset_t get_preset,
            fluid_sfont_free_t free);

        [DllImport(LibraryName)]
        internal static extern int delete_fluid_sfont(fluid_sfont_t_ptr sfont);

        [DllImport(LibraryName)]
        internal static extern int fluid_sfont_set_data(fluid_sfont_t_ptr sfont, IntPtr data);

        [DllImport(LibraryName)]
        internal static extern IntPtr fluid_sfont_get_data(fluid_sfont_t_ptr sfont);

        internal delegate string fluid_preset_get_name_t(fluid_preset_t_ptr preset);

        internal delegate int fluid_preset_get_banknum_t(fluid_preset_t_ptr preset);

        internal delegate int fluid_preset_get_num_t(fluid_preset_t_ptr preset);

        internal delegate int fluid_preset_noteon_t(fluid_preset_t_ptr preset, fluid_synth_t_ptr synth, int chan,
            int key, int vel);

        internal delegate void fluid_preset_free_t(fluid_preset_t_ptr preset);

        [DllImport(LibraryName)]
        internal static extern fluid_preset_t_ptr new_fluid_preset(fluid_sfont_t_ptr parent_sfont,
            fluid_preset_get_name_t get_name,
            fluid_preset_get_banknum_t get_bank,
            fluid_preset_get_num_t get_num,
            fluid_preset_noteon_t noteon,
            fluid_preset_free_t free);

        [DllImport(LibraryName)]
        internal static extern void delete_fluid_preset(fluid_preset_t_ptr preset);

        [DllImport(LibraryName)]
        internal static extern int fluid_preset_set_data(fluid_preset_t_ptr preset, IntPtr data);

        [DllImport(LibraryName)]
        internal static extern IntPtr fluid_preset_get_data(fluid_preset_t_ptr preset);


        [DllImport(LibraryName)]
        internal static extern fluid_sample_t_ptr new_fluid_sample();

        [DllImport(LibraryName)]
        internal static extern void delete_fluid_sample(fluid_sample_t_ptr sample);

        [DllImport(LibraryName)]
        internal static extern IntPtr fluid_sample_sizeof();

        [DllImport(LibraryName)]
        internal static extern int fluid_sample_set_name(fluid_sample_t_ptr sample,
            [MarshalAs(LP_Str)] string name);

        [DllImport(LibraryName)]
        internal static extern int fluid_sample_set_sound_data(fluid_sample_t_ptr sample,
            IntPtr data,
            IntPtr data24,
            uint nbframes,
            uint sample_rate,
            short copy_data);

        [DllImport(LibraryName)]
        internal static extern int fluid_sample_set_loop(fluid_sample_t_ptr sample, uint loop_start, uint loop_end);

        [DllImport(LibraryName)]
        internal static extern int fluid_sample_set_pitch(fluid_sample_t_ptr sample, int root_key, int fine_tune);
    }
}
