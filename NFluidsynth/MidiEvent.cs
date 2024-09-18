using System;
using NFluidsynth.Native;

namespace NFluidsynth
{
    public class MidiEvent : FluidsynthObject
    {
        private readonly bool _owned;

        public MidiEvent()
            : base(LibFluidsynth.new_fluid_midi_event())
        {
            _owned = true;
        }

        protected internal MidiEvent(IntPtr handle)
            : base(handle)
        {
            _owned = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (_owned && !Disposed)
            {
                LibFluidsynth.delete_fluid_midi_event(Handle);
            }
        }

        public int Type
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_midi_event_get_type(Handle);
            }
            set
            {
                ThrowIfDisposed();
                LibFluidsynth.fluid_midi_event_set_type(Handle, value);
            }
        }

        public int Channel
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_midi_event_get_channel(Handle);
            }
            set
            {
                ThrowIfDisposed();
                LibFluidsynth.fluid_midi_event_set_channel(Handle, value);
            }
        }

        public int Key
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_midi_event_get_key(Handle);
            }
            set
            {
                ThrowIfDisposed();
                LibFluidsynth.fluid_midi_event_set_key(Handle, value);
            }
        }

        public int Velocity
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_midi_event_get_velocity(Handle);
            }
            set
            {
                ThrowIfDisposed();
                LibFluidsynth.fluid_midi_event_set_velocity(Handle, value);
            }
        }

        public int Control
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_midi_event_get_control(Handle);
            }
            set
            {
                ThrowIfDisposed();
                LibFluidsynth.fluid_midi_event_set_control(Handle, value);
            }
        }

        public int Value
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_midi_event_get_value(Handle);
            }
            set
            {
                ThrowIfDisposed();
                LibFluidsynth.fluid_midi_event_set_value(Handle, value);
            }
        }

        public int Program
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_midi_event_get_program(Handle);
            }
            set
            {
                ThrowIfDisposed();
                LibFluidsynth.fluid_midi_event_set_program(Handle, value);
            }
        }

        public int Pitch
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_midi_event_get_pitch(Handle);
            }
            set
            {
                ThrowIfDisposed();
                LibFluidsynth.fluid_midi_event_set_pitch(Handle, value);
            }
        }

        public unsafe void SetSysex(void* data, int size, bool isDynamic)
        {
            LibFluidsynth.fluid_midi_event_set_sysex(Handle, data, size, isDynamic);
        }
    }
}