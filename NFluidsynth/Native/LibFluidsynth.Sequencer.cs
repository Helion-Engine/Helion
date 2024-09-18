using System;
using System.Runtime.InteropServices;
using fluid_event_t_ptr = System.IntPtr;
using fluid_sequencer_t_ptr = System.IntPtr;
using fluid_seq_id_t = System.Int16;

namespace NFluidsynth.Native
{
    internal static partial class LibFluidsynth
    {
        internal unsafe delegate void fluid_event_callback_t(uint time, fluid_event_t_ptr @event,
            fluid_sequencer_t_ptr seq, void* data);

        [DllImport(LibraryName)]
        internal static extern fluid_sequencer_t_ptr new_fluid_sequencer2(int use_system_timer);

        [DllImport(LibraryName)]
        internal static extern void delete_fluid_sequencer(fluid_sequencer_t_ptr seq);

        [DllImport(LibraryName)]
        internal static extern int fluid_sequencer_get_use_system_timer(fluid_sequencer_t_ptr seq);

        [DllImport(LibraryName)]
        internal static extern unsafe fluid_seq_id_t fluid_sequencer_register_client(fluid_sequencer_t_ptr seq,
            [MarshalAs(LP_Str)] string name, IntPtr callback, void* data);

        [DllImport(LibraryName)]
        internal static extern void fluid_sequencer_unregister_client(fluid_sequencer_t_ptr seq, fluid_seq_id_t id);

        [DllImport(LibraryName)]
        internal static extern int fluid_sequencer_count_clients(fluid_sequencer_t_ptr seq);

        [DllImport(LibraryName)]
        internal static extern fluid_seq_id_t fluid_sequencer_get_client_id(fluid_sequencer_t_ptr seq, int index);

        [DllImport(LibraryName)]
        internal static extern unsafe byte* fluid_sequencer_get_client_name(fluid_sequencer_t_ptr seq,
            fluid_seq_id_t id);

        [DllImport(LibraryName)]
        internal static extern int fluid_sequencer_client_is_dest(fluid_sequencer_t_ptr seq, fluid_seq_id_t id);

        [DllImport(LibraryName)]
        internal static extern void fluid_sequencer_process(fluid_sequencer_t_ptr seq, uint msec);

        [DllImport(LibraryName)]
        internal static extern void fluid_sequencer_send_now(fluid_sequencer_t_ptr seq, fluid_event_t_ptr evt);

        [DllImport(LibraryName)]
        internal static extern int fluid_sequencer_send_at(fluid_sequencer_t_ptr seq, fluid_event_t_ptr evt, uint
            time, int absolute);

        [DllImport(LibraryName)]
        internal static extern void fluid_sequencer_remove_events(fluid_sequencer_t_ptr seq, fluid_seq_id_t source,
            fluid_seq_id_t dest, int type);

        [DllImport(LibraryName)]
        internal static extern uint fluid_sequencer_get_tick(fluid_sequencer_t_ptr seq);

        [DllImport(LibraryName)]
        internal static extern void fluid_sequencer_set_time_scale(fluid_sequencer_t_ptr seq, double scale);

        [DllImport(LibraryName)]
        internal static extern double fluid_sequencer_get_time_scale(fluid_sequencer_t_ptr seq);
    }
}