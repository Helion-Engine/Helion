using System.Runtime.InteropServices;
using fluid_midi_event_t_ptr = System.IntPtr;
using fluid_sequencer_t_ptr = System.IntPtr;
using fluid_seq_id_t = System.Int16;
using fluid_synth_t_ptr = System.IntPtr;

namespace NFluidsynth.Native
{
    internal static partial class LibFluidsynth
    {
        [DllImport(LibraryName)]
        internal static extern fluid_seq_id_t fluid_sequencer_register_fluidsynth(fluid_sequencer_t_ptr seq,
            fluid_synth_t_ptr synth);

        [DllImport(LibraryName)]
        internal static extern unsafe int fluid_sequencer_add_midi_event_to_buffer(void* data, fluid_midi_event_t_ptr @event);
    }
}