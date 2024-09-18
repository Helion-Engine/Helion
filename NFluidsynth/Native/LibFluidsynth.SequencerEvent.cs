using System.Runtime.InteropServices;
using fluid_event_t_ptr = System.IntPtr;
using fluid_seq_id_t = System.Int16;

// ReSharper disable InconsistentNaming
// ReSharper disable IdentifierTypo

namespace NFluidsynth.Native
{
    internal static unsafe partial class LibFluidsynth
    {


        /* Event alloc/free */
        [DllImport(LibraryName)]
        internal static extern fluid_event_t_ptr new_fluid_event();

        [DllImport(LibraryName)]
        internal static extern void delete_fluid_event(fluid_event_t_ptr evt);

        /* Initializing events */
        [DllImport(LibraryName)]
        internal static extern void fluid_event_set_source(fluid_event_t_ptr evt, fluid_seq_id_t src);

        [DllImport(LibraryName)]
        internal static extern void fluid_event_set_dest(fluid_event_t_ptr evt, fluid_seq_id_t dest);

        /* Timer events */
        [DllImport(LibraryName)]
        internal static extern void fluid_event_timer(fluid_event_t_ptr evt, void* data);

        /* Note events */
        [DllImport(LibraryName)]
        internal static extern void fluid_event_note(fluid_event_t_ptr evt, int channel,
            short key, short vel,
            uint duration);

        [DllImport(LibraryName)]
        internal static extern void fluid_event_noteon(fluid_event_t_ptr evt, int channel, short key, short vel);

        [DllImport(LibraryName)]
        internal static extern void fluid_event_noteoff(fluid_event_t_ptr evt, int channel, short key);

        [DllImport(LibraryName)]
        internal static extern void fluid_event_all_sounds_off(fluid_event_t_ptr evt, int channel);

        [DllImport(LibraryName)]
        internal static extern void fluid_event_all_notes_off(fluid_event_t_ptr evt, int channel);

        /* Instrument selection */
        [DllImport(LibraryName)]
        internal static extern void fluid_event_bank_select(fluid_event_t_ptr evt, int channel, short bank_num);

        [DllImport(LibraryName, EntryPoint="fluid_event_program_change")]
        internal static extern void fluid_event_program_change_2(fluid_event_t_ptr evt, int channel, short preset_num);

        [DllImport(LibraryName, EntryPoint="fluid_event_program_change")]
        internal static extern void fluid_event_program_change_3(fluid_event_t_ptr evt, int channel, int preset_num);

        [DllImport(LibraryName)]
        internal static extern void fluid_event_program_select(fluid_event_t_ptr evt, int channel, uint
            sfont_id, short bank_num, short preset_num);

        /* Real-time generic instrument controllers */
        [DllImport(LibraryName, EntryPoint="fluid_event_control_change")]
        internal static extern
            void fluid_event_control_change_2(fluid_event_t_ptr evt, int channel, short control, short val);

        [DllImport(LibraryName, EntryPoint="fluid_event_control_change")]
        internal static extern
            void fluid_event_control_change_3(fluid_event_t_ptr evt, int channel, short control, int val);

        /* Real-time instrument controllers shortcuts */
        [DllImport(LibraryName)]
        internal static extern void fluid_event_pitch_bend(fluid_event_t_ptr evt, int channel, int val);

        [DllImport(LibraryName, EntryPoint="fluid_event_pitch_wheelsens")]
        internal static extern void fluid_event_pitch_wheelsens_2(fluid_event_t_ptr evt, int channel, short val);

        [DllImport(LibraryName, EntryPoint="fluid_event_pitch_wheelsens")]
        internal static extern void fluid_event_pitch_wheelsens_3(fluid_event_t_ptr evt, int channel, int val);

        [DllImport(LibraryName, EntryPoint="fluid_event_modulation")]
        internal static extern void fluid_event_modulation_2(fluid_event_t_ptr evt, int channel, short val);

        [DllImport(LibraryName, EntryPoint="fluid_event_modulation")]
        internal static extern void fluid_event_modulation_3(fluid_event_t_ptr evt, int channel, int val);

        [DllImport(LibraryName, EntryPoint="fluid_event_sustain")]
        internal static extern void fluid_event_sustain_2(fluid_event_t_ptr evt, int channel, short val);

        [DllImport(LibraryName, EntryPoint="fluid_event_sustain")]
        internal static extern void fluid_event_sustain_3(fluid_event_t_ptr evt, int channel, int val);

        [DllImport(LibraryName, EntryPoint="fluid_event_pan")]
        internal static extern void fluid_event_pan_2(fluid_event_t_ptr evt, int channel, short val);

        [DllImport(LibraryName, EntryPoint="fluid_event_pan")]
        internal static extern void fluid_event_pan_3(fluid_event_t_ptr evt, int channel, int val);

        [DllImport(LibraryName, EntryPoint="fluid_event_volume")]
        internal static extern void fluid_event_volume_2(fluid_event_t_ptr evt, int channel, short val);

        [DllImport(LibraryName, EntryPoint="fluid_event_volume")]
        internal static extern void fluid_event_volume_3(fluid_event_t_ptr evt, int channel, int val);

        [DllImport(LibraryName, EntryPoint="fluid_event_reverb_send")]
        internal static extern void fluid_event_reverb_send_2(fluid_event_t_ptr evt, int channel, short val);

        [DllImport(LibraryName, EntryPoint="fluid_event_reverb_send")]
        internal static extern void fluid_event_reverb_send_3(fluid_event_t_ptr evt, int channel, int val);

        [DllImport(LibraryName, EntryPoint="fluid_event_chorus_send")]
        internal static extern void fluid_event_chorus_send_2(fluid_event_t_ptr evt, int channel, short val);

        [DllImport(LibraryName, EntryPoint="fluid_event_chorus_send")]
        internal static extern void fluid_event_chorus_send_3(fluid_event_t_ptr evt, int channel, int val);

        [DllImport(LibraryName, EntryPoint="fluid_event_key_pressure")]
        internal static extern void fluid_event_key_pressure_2(fluid_event_t_ptr evt, int channel, short key, short val);

        [DllImport(LibraryName, EntryPoint="fluid_event_key_pressure")]
        internal static extern void fluid_event_key_pressure_3(fluid_event_t_ptr evt, int channel, short key, int val);

        [DllImport(LibraryName, EntryPoint="fluid_event_channel_pressure")]
        internal static extern void fluid_event_channel_pressure_2(fluid_event_t_ptr evt, int channel, short val);

        [DllImport(LibraryName, EntryPoint="fluid_event_channel_pressure")]
        internal static extern void fluid_event_channel_pressure_3(fluid_event_t_ptr evt, int channel, int val);

        [DllImport(LibraryName)]
        internal static extern void fluid_event_system_reset(fluid_event_t_ptr evt);


        /* Only for removing events */
        [DllImport(LibraryName, EntryPoint="fluid_event_any_control_change")]
        internal static extern void fluid_event_any_control_change_2(fluid_event_t_ptr evt, int channel);

        /* Only when unregistering clients */
        [DllImport(LibraryName)]
        internal static extern void fluid_event_unregistering(fluid_event_t_ptr evt);

        /* Accessing event data */
        [DllImport(LibraryName)]
        internal static extern int fluid_event_get_type(fluid_event_t_ptr evt);

        [DllImport(LibraryName)]
        internal static extern fluid_seq_id_t fluid_event_get_source(fluid_event_t_ptr evt);

        [DllImport(LibraryName)]
        internal static extern fluid_seq_id_t fluid_event_get_dest(fluid_event_t_ptr evt);

        [DllImport(LibraryName)]
        internal static extern int fluid_event_get_channel(fluid_event_t_ptr evt);

        [DllImport(LibraryName)]
        internal static extern short fluid_event_get_key(fluid_event_t_ptr evt);

        [DllImport(LibraryName)]
        internal static extern short fluid_event_get_velocity(fluid_event_t_ptr evt);

        [DllImport(LibraryName)]
        internal static extern short fluid_event_get_control(fluid_event_t_ptr evt);

        [DllImport(LibraryName, EntryPoint="fluid_event_get_value")]
        internal static extern short fluid_event_get_value_2(fluid_event_t_ptr evt);

        [DllImport(LibraryName, EntryPoint="fluid_event_get_value")]
        internal static extern int fluid_event_get_value_3(fluid_event_t_ptr evt);

        [DllImport(LibraryName, EntryPoint="fluid_event_get_program")]
        internal static extern short fluid_event_get_program_2(fluid_event_t_ptr evt);

        [DllImport(LibraryName, EntryPoint="fluid_event_get_program")]
        internal static extern int fluid_event_get_program_3(fluid_event_t_ptr evt);

        [DllImport(LibraryName)]
        internal static extern void* fluid_event_get_data(fluid_event_t_ptr evt);

        [DllImport(LibraryName)]
        internal static extern uint fluid_event_get_duration(fluid_event_t_ptr evt);

        [DllImport(LibraryName)]
        internal static extern short fluid_event_get_bank(fluid_event_t_ptr evt);

        [DllImport(LibraryName)]
        internal static extern int fluid_event_get_pitch(fluid_event_t_ptr evt);

        [DllImport(LibraryName)]
        internal static extern uint fluid_event_get_sfont_id(fluid_event_t_ptr evt);
    }
}