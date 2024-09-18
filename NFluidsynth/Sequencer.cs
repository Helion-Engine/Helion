using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using NFluidsynth.Native;

namespace NFluidsynth
{
    public class Sequencer : FluidsynthObject
    {
        private readonly Dictionary<short, LibFluidsynth.fluid_event_callback_t> _clientCallbacks =
            new Dictionary<short, LibFluidsynth.fluid_event_callback_t>();

        public Sequencer(bool useSystemTimer) : base(LibFluidsynth.new_fluid_sequencer2(useSystemTimer ? 1 : 0))
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                LibFluidsynth.delete_fluid_sequencer(Handle);
            }

            base.Dispose(disposing);
        }

        public bool UseSystemTimer
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_sequencer_get_use_system_timer(Handle) != 0;
            }
        }

        public uint Tick
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_sequencer_get_tick(Handle);
            }
        }

        public double TimeScale
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_sequencer_get_time_scale(Handle);
            }
            set
            {
                ThrowIfDisposed();
                LibFluidsynth.fluid_sequencer_set_time_scale(Handle, value);
            }
        }

        public int CountClients
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_sequencer_count_clients(Handle);
            }
        }

        public unsafe SequencerClientId RegisterClient(string name, SequencerEventCallback callback)
        {
            ThrowIfDisposed();

            LibFluidsynth.fluid_event_callback_t wrapper = null;
            var callbackPtr = IntPtr.Zero;
            if (callback != null)
            {
                wrapper = (time, @event, seq, data) =>
                {
                    using (var ev = new SequencerEvent(@event))
                    {
                        callback(time, ev);
                    }
                };

                callbackPtr = Marshal.GetFunctionPointerForDelegate(wrapper);
            }

            var id = LibFluidsynth.fluid_sequencer_register_client(Handle, name, callbackPtr, null);
            if (id == LibFluidsynth.FluidFailed)
            {
                throw new FluidSynthInteropException("fluid_sequencer_register_client failed");
            }

            if (wrapper != null)
            {
                _clientCallbacks.Add(id, wrapper);
            }

            return new SequencerClientId(id);
        }

        public void UnregisterClient(SequencerClientId id)
        {
            ThrowIfDisposed();

            _clientCallbacks.Remove(id.Value);

            LibFluidsynth.fluid_sequencer_unregister_client(Handle, id.Value);
        }

        public SequencerClientId GetClientId(int index)
        {
            ThrowIfDisposed();

            var id = LibFluidsynth.fluid_sequencer_get_client_id(Handle, index);

            if (id == LibFluidsynth.FluidFailed)
            {
                throw new ArgumentException("Sequencer client not found.");
            }

            return new SequencerClientId(id);
        }

        public unsafe string GetClientName(SequencerClientId id)
        {
            ThrowIfDisposed();

            var ptr = LibFluidsynth.fluid_sequencer_get_client_name(Handle, id.Value);

            return Utility.PtrToStringUTF8(ptr);
        }

        public bool ClientIsDestination(SequencerClientId id)
        {
            ThrowIfDisposed();

            return LibFluidsynth.fluid_sequencer_client_is_dest(Handle, id.Value) != 0;
        }

        public void Process(uint msec)
        {
            ThrowIfDisposed();

            LibFluidsynth.fluid_sequencer_process(Handle, msec);
        }

        public void SendNow(SequencerEvent evt)
        {
            ThrowIfDisposed();

            evt.ThrowIfDisposed();

            LibFluidsynth.fluid_sequencer_send_now(Handle, evt.Handle);
        }

        public void SendAt(SequencerEvent evt, uint time, bool absolute)
        {
            ThrowIfDisposed();

            evt.ThrowIfDisposed();

            var ret = LibFluidsynth.fluid_sequencer_send_at(Handle, evt.Handle, time, absolute ? 1 : 0);

            if (ret == LibFluidsynth.FluidFailed)
            {
                throw new FluidSynthInteropException("fluid_sequencer_send_at failed");
            }
        }

        public void RemoveEvents(SequencerClientId source, SequencerClientId dest, int type)
        {
            ThrowIfDisposed();

            LibFluidsynth.fluid_sequencer_remove_events(Handle, source.Value, dest.Value, type);
        }

        public SequencerClientId RegisterFluidsynth(Synth synth)
        {
            ThrowIfDisposed();
            synth.ThrowIfDisposed();

            var id = LibFluidsynth.fluid_sequencer_register_fluidsynth(Handle, synth.Handle);
            if (id == LibFluidsynth.FluidFailed)
            {
                throw new FluidSynthInteropException("fluid_sequencer_register_fluidsynth failed");
            }

            return new SequencerClientId(id);
        }

        public unsafe void AddMidiEventToBuffer(MidiEvent @event)
        {
            ThrowIfDisposed();
            @event.ThrowIfDisposed();

            var ret = LibFluidsynth.fluid_sequencer_add_midi_event_to_buffer((void*) Handle, @event.Handle);
            if (ret == LibFluidsynth.FluidFailed)
            {
                throw new FluidSynthInteropException("fluid_sequencer_add_midi_event_to_buffer failed");
            }
        }
    }
}