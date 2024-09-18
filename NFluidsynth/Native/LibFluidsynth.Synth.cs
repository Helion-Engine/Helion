using System;
using System.Runtime.InteropServices;
using fluid_settings_t_ptr = System.IntPtr;
using fluid_synth_t_ptr = System.IntPtr;
using fluid_preset_t_ptr = System.IntPtr;
using fluid_sfont_t_ptr = System.IntPtr;
using fluid_synth_channel_info_t_ptr = System.IntPtr;
using fluid_voice_t_ptr = System.IntPtr;
using fluid_midi_router_t_ptr = System.IntPtr;
using fluid_sfloader_t_ptr = System.IntPtr;
using fluid_sample_t_ptr = System.IntPtr;
using fluid_midi_event_t_ptr = System.IntPtr;

namespace NFluidsynth.Native
{
    internal unsafe static partial class LibFluidsynth
    {
        private delegate int fluid_audio_callback_t(fluid_synth_t_ptr synth, int len,
            IntPtr out1, int loff, int lincr,
            IntPtr out2, int roff, int rincr);


        [DllImport(LibraryName)]
        internal static extern fluid_synth_t_ptr new_fluid_synth(fluid_settings_t_ptr settings);

        [DllImport(LibraryName)]
        internal static extern void delete_fluid_synth(fluid_synth_t_ptr synth);

        [DllImport(LibraryName)]
        internal static extern fluid_settings_t_ptr fluid_synth_get_settings(fluid_synth_t_ptr synth);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_noteon(fluid_synth_t_ptr synth, int chan, int key, int vel);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_noteoff(fluid_synth_t_ptr synth, int chan, int key);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_cc(fluid_synth_t_ptr synth, int chan, int ctrl, int val);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_get_cc(fluid_synth_t_ptr synth, int chan, int ctrl, out int pval);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_sysex(fluid_synth_t_ptr synth, byte* data,
            int len, byte* response,
            ref int response_len, [MarshalAs(UnmanagedType.I4)] out bool handled,
            [MarshalAs(UnmanagedType.I4)] bool dryrun);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_pitch_bend(fluid_synth_t_ptr synth, int chan, int val);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_get_pitch_bend(fluid_synth_t_ptr synth, int chan, out int ppitch_bend);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_pitch_wheel_sens(fluid_synth_t_ptr synth, int chan, int val);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_get_pitch_wheel_sens(fluid_synth_t_ptr synth, int chan, out int pval);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_program_change(fluid_synth_t_ptr synth, int chan, int program);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_channel_pressure(fluid_synth_t_ptr synth, int chan, int val);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_key_pressure(fluid_synth_t_ptr synth, int chan, int key, int val);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_bank_select(fluid_synth_t_ptr synth, int chan, uint bank);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_sfont_select(fluid_synth_t_ptr synth, int chan, uint sfont_id);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_program_select(fluid_synth_t_ptr synth, int chan, uint sfont_id,
            uint bank_num, uint preset_num);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_program_select_by_sfont_name(fluid_synth_t_ptr synth, int chan,
            [MarshalAs(LP_Str)] string sfont_name, uint bank_num, uint preset_num);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_get_program(fluid_synth_t_ptr synth, int chan, out int sfont_id,
            out int bank_num, out int preset_num);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_unset_program(fluid_synth_t_ptr synth, int chan);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_program_reset(fluid_synth_t_ptr synth);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_system_reset(fluid_synth_t_ptr synth);

        [DllImport(LibraryName)]
        internal static extern fluid_preset_t_ptr fluid_synth_get_channel_preset(fluid_synth_t_ptr synth, int chan);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_start(fluid_synth_t_ptr synth, uint id, fluid_preset_t_ptr preset,
            int audio_chan, int midi_chan, int key, int vel);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_stop(fluid_synth_t_ptr synth, uint id);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_sfload(fluid_synth_t_ptr synth,
            [MarshalAs(LP_Str)] string filename, bool reset_presets);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_sfreload(fluid_synth_t_ptr synth, uint id);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_sfunload(fluid_synth_t_ptr synth, uint id, bool reset_presets);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_add_sfont(fluid_synth_t_ptr synth, fluid_sfont_t_ptr sfont);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_remove_sfont(fluid_synth_t_ptr synth, fluid_sfont_t_ptr sfont);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_sfcount(fluid_synth_t_ptr synth);

        [DllImport(LibraryName)]
        internal static extern fluid_sfont_t_ptr fluid_synth_get_sfont(fluid_synth_t_ptr synth,
            uint num);

        [DllImport(LibraryName)]
        internal static extern fluid_sfont_t_ptr fluid_synth_get_sfont_by_id(fluid_synth_t_ptr synth,
            uint id);

        [DllImport(LibraryName)]
        internal static extern fluid_sfont_t_ptr fluid_synth_get_sfont_by_name(fluid_synth_t_ptr synth,
            [MarshalAs(LP_Str)] string name);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_set_bank_offset(fluid_synth_t_ptr synth, int sfont_id, int offset);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_get_bank_offset(fluid_synth_t_ptr synth, int sfont_id);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_set_reverb(fluid_synth_t_ptr synth, double roomsize, double damping,
            double width, double level);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_set_reverb_on(fluid_synth_t_ptr synth, bool on);

        [DllImport(LibraryName)]
        internal static extern double fluid_synth_get_reverb_roomsize(fluid_synth_t_ptr synth);

        [DllImport(LibraryName)]
        internal static extern double fluid_synth_get_reverb_damp(fluid_synth_t_ptr synth);

        [DllImport(LibraryName)]
        internal static extern double fluid_synth_get_reverb_level(fluid_synth_t_ptr synth);

        [DllImport(LibraryName)]
        internal static extern double fluid_synth_get_reverb_width(fluid_synth_t_ptr synth);

        [DllImport(LibraryName)]
        internal static extern void fluid_synth_set_chorus(fluid_synth_t_ptr synth,
            int nr, double level, double speed, double depth_ms,
            FluidChorusMod type);

        [DllImport(LibraryName)]
        internal static extern void fluid_synth_set_chorus_on(fluid_synth_t_ptr synth, bool on);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_get_chorus_nr(fluid_synth_t_ptr synth);

        [DllImport(LibraryName)]
        internal static extern double fluid_synth_get_chorus_level(fluid_synth_t_ptr synth);

        [DllImport(LibraryName)]
        internal static extern double fluid_synth_get_chorus_speed_Hz(fluid_synth_t_ptr synth);

        [DllImport(LibraryName)]
        internal static extern double fluid_synth_get_chorus_depth_ms(fluid_synth_t_ptr synth);

        [DllImport(LibraryName)]
        internal static extern FluidChorusMod fluid_synth_get_chorus_type(fluid_synth_t_ptr synth);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_count_midi_channels(fluid_synth_t_ptr synth);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_count_audio_channels(fluid_synth_t_ptr synth);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_count_audio_groups(fluid_synth_t_ptr synth);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_count_effects_channels(fluid_synth_t_ptr synth);

        [DllImport(LibraryName)]
        internal static extern void fluid_synth_set_sample_rate(fluid_synth_t_ptr synth, float sample_rate);

        [DllImport(LibraryName)]
        internal static extern void fluid_synth_set_gain(fluid_synth_t_ptr synth, float gain);

        [DllImport(LibraryName)]
        internal static extern float fluid_synth_get_gain(fluid_synth_t_ptr synth);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_set_polyphony(fluid_synth_t_ptr synth, int polyphony);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_get_polyphony(fluid_synth_t_ptr synth);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_get_active_voice_count(fluid_synth_t_ptr synth);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_get_internal_bufsize(fluid_synth_t_ptr synth);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_set_interp_method(fluid_synth_t_ptr synth, int chan,
            FluidInterpolation interp_method);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_set_gen(fluid_synth_t_ptr synth,
            int chan, int param, float value);

        [DllImport(LibraryName)]
        internal static extern float fluid_synth_get_gen(fluid_synth_t_ptr synth,
            int chan, int param);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_activate_key_tuning(fluid_synth_t_ptr synth,
            int bank, int prog, [MarshalAs(LP_Str)] string name,
            double* pitch, bool apply);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_activate_octave_tuning(fluid_synth_t_ptr synth,
            int bank, int prog, [MarshalAs(LP_Str)] string name,
            double* pitch, bool apply);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_tune_notes(fluid_synth_t_ptr synth,
            int bank, int prog,
            int len, int* keys, double* pitch,
            bool apply);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_activate_tuning(fluid_synth_t_ptr synth,
            int chan, int bank,
            int prog, bool apply);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_deactivate_tuning(fluid_synth_t_ptr synth,
            int chan, bool apply);

        [DllImport(LibraryName)]
        internal static extern void fluid_synth_tuning_iteration_start(fluid_synth_t_ptr synth);

        [DllImport(LibraryName)]
        [return: MarshalAs(UnmanagedType.I4)]
        internal static extern bool fluid_synth_tuning_iteration_next(fluid_synth_t_ptr synth, out int bank,
            out int prog);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_tuning_dump(fluid_synth_t_ptr synth,
            int bank, int prog, byte* name,
            int len, double* pitch);

        [DllImport(LibraryName)]
        internal static extern double fluid_synth_get_cpu_load(fluid_synth_t_ptr synth);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_write_s16(fluid_synth_t_ptr synth,
            int len, ushort* lout, int loff,
            int lincr, ushort* rout, int roff,
            int rincr);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_write_float(fluid_synth_t_ptr synth,
            int len, float* lout, int loff,
            int lincr, float* rout, int roff,
            int rincr);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_process(fluid_synth_t_ptr synth,
            int len, int nin, float** in_ignored,
            int nout, float** outBuffer);

        [DllImport(LibraryName)]
        internal static extern void fluid_synth_add_sfloader(fluid_synth_t_ptr synth, fluid_sfloader_t_ptr loader);

        [DllImport(LibraryName)]
        internal static extern fluid_voice_t_ptr fluid_synth_alloc_voice(fluid_synth_t_ptr synth,
            fluid_sample_t_ptr sample, int channum,
            int key, int vel);

        [DllImport(LibraryName)]
        internal static extern void fluid_synth_start_voice(fluid_synth_t_ptr synth, fluid_voice_t_ptr voice);

        [DllImport(LibraryName)]
        internal static extern void fluid_synth_get_voicelist(fluid_synth_t_ptr synth, fluid_voice_t_ptr* buf,
            int bufsize, int ID);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_handle_midi_event(IntPtr data, fluid_midi_event_t_ptr @event);

        [DllImport(LibraryName)]
        internal static extern int fluid_synth_all_notes_off(fluid_synth_t_ptr synth, int channel);
    }
}
