using System;
using NFluidsynth.Native;

namespace NFluidsynth
{
    public class SequencerEvent : FluidsynthObject
    {
        private readonly bool _owned;

        public SequencerEvent()
            : base(LibFluidsynth.new_fluid_event())
        {
            _owned = true;
        }

        protected internal SequencerEvent(IntPtr handle)
            : base(handle)
        {
            _owned = false;
        }

        protected override void Dispose(bool disposing)
        {
            if (_owned && !Disposed)
            {
                LibFluidsynth.delete_fluid_event(Handle);
            }
        }

        public FluidSequencerEventType Type
        {
            get
            {
                ThrowIfDisposed();
                return (FluidSequencerEventType) LibFluidsynth.fluid_event_get_type(Handle);
            }
        }

        public SequencerClientId Source
        {
            get
            {
                ThrowIfDisposed();
                return new SequencerClientId(LibFluidsynth.fluid_event_get_source(Handle));
            }
            set
            {
                ThrowIfDisposed();
                LibFluidsynth.fluid_event_set_source(Handle, value.Value);
            }
        }

        public SequencerClientId Dest
        {
            get
            {
                ThrowIfDisposed();
                return new SequencerClientId(LibFluidsynth.fluid_event_get_dest(Handle));
            }
            set
            {
                ThrowIfDisposed();
                LibFluidsynth.fluid_event_set_dest(Handle, value.Value);
            }
        }

        public int Channel
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_event_get_channel(Handle);
            }
        }

        public short Key
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_event_get_key(Handle);
            }
        }

        public short Velocity
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_event_get_velocity(Handle);
            }
        }

        public short Control
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_event_get_control(Handle);
            }
        }

        public int Value
        {
            get
            {
                ThrowIfDisposed();

                if (LibFluidsynth.LibraryVersion == 2)
                    return LibFluidsynth.fluid_event_get_value_2(Handle);
                else 
                    return LibFluidsynth.fluid_event_get_value_3(Handle);
            }
        }

        public int Program
        {
            get
            {
                ThrowIfDisposed();

                if (LibFluidsynth.LibraryVersion == 2)
                    return LibFluidsynth.fluid_event_get_program_2(Handle);
                else
                    return LibFluidsynth.fluid_event_get_program_3(Handle);
            }
        }

        public unsafe void* Data
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_event_get_data(Handle);
            }
        }

        public uint Duration
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_event_get_duration(Handle);
            }
        }

        public short Bank
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_event_get_bank(Handle);
            }
        }

        public int Pitch
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_event_get_pitch(Handle);
            }
        }

        public uint SoundFontId
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_event_get_sfont_id(Handle);
            }
        }

        public unsafe void Timer(void* data)
        {
            ThrowIfDisposed();
            LibFluidsynth.fluid_event_timer(Handle, data);
        }

        public void Note(int channel, short key, short vel, uint duration)
        {
            ThrowIfDisposed();
            LibFluidsynth.fluid_event_note(Handle, channel, key, vel, duration);
        }

        public void NoteOn(int channel, short key, short vel)
        {
            ThrowIfDisposed();
            LibFluidsynth.fluid_event_noteon(Handle, channel, key, vel);
        }

        public void NoteOff(int channel, short key)
        {
            ThrowIfDisposed();
            LibFluidsynth.fluid_event_noteoff(Handle, channel, key);
        }

        public void AllSoundsOff(int channel)
        {
            ThrowIfDisposed();
            LibFluidsynth.fluid_event_all_sounds_off(Handle, channel);
        }

        public void AllNotesOff(int channel)
        {
            ThrowIfDisposed();
            LibFluidsynth.fluid_event_all_notes_off(Handle, channel);
        }

        public void BankSelect(int channel, short bankNum)
        {
            ThrowIfDisposed();
            LibFluidsynth.fluid_event_bank_select(Handle, channel, bankNum);
        }

        public void ProgramChange(int channel, short val)
        {
            ThrowIfDisposed();
            
            if (LibFluidsynth.LibraryVersion == 2)
                LibFluidsynth.fluid_event_program_change_2(Handle, channel, val);
            else
                LibFluidsynth.fluid_event_program_change_3(Handle, channel, val);
        }

        public void ProgramSelect(int channel, uint soundFontId, short bankNum, short presetNum)
        {
            ThrowIfDisposed();
            LibFluidsynth.fluid_event_program_select(Handle, channel, soundFontId, bankNum, presetNum);
        }

        public void ControlChange(int channel, short control, short val)
        {
            ThrowIfDisposed();
            
            if (LibFluidsynth.LibraryVersion == 2)
                LibFluidsynth.fluid_event_control_change_2(Handle, channel, control, val);
            else
                LibFluidsynth.fluid_event_control_change_3(Handle, channel, control, val);
        }

        public void PitchBend(int channel, int pitch)
        {
            ThrowIfDisposed();
            LibFluidsynth.fluid_event_pitch_bend(Handle, channel, pitch);
        }

        public void PitchWheelSensitivity(int channel, short value)
        {
            ThrowIfDisposed();

            if (LibFluidsynth.LibraryVersion == 2)
                LibFluidsynth.fluid_event_pitch_wheelsens_2(Handle, channel, value);
            else
                LibFluidsynth.fluid_event_pitch_wheelsens_3(Handle, channel, value);
        }

        public void Modulation(int channel, short val)
        {
            ThrowIfDisposed();

            if (LibFluidsynth.LibraryVersion == 2)
                LibFluidsynth.fluid_event_modulation_2(Handle, channel, val);
            else
                LibFluidsynth.fluid_event_modulation_3(Handle, channel, val);
        }

        public void Sustain(int channel, short val)
        {
            ThrowIfDisposed();

            if (LibFluidsynth.LibraryVersion == 2)
                LibFluidsynth.fluid_event_sustain_2(Handle, channel, val);
            else
                LibFluidsynth.fluid_event_sustain_3(Handle, channel, val);
        }

        public void Pan(int channel, short val)
        {
            ThrowIfDisposed();

            if (LibFluidsynth.LibraryVersion == 2)
                LibFluidsynth.fluid_event_pan_2(Handle, channel, val);
            else
                LibFluidsynth.fluid_event_pan_3(Handle, channel, val);
        }

        public void Volume(int channel, short val)
        {
            ThrowIfDisposed();

            if (LibFluidsynth.LibraryVersion == 2)
                LibFluidsynth.fluid_event_volume_2(Handle, channel, val);
            else
                LibFluidsynth.fluid_event_volume_3(Handle, channel, val);
        }

        public void ReverbSend(int channel, short val)
        {
            ThrowIfDisposed();

            if (LibFluidsynth.LibraryVersion == 2)
                LibFluidsynth.fluid_event_reverb_send_2(Handle, channel, val);
            else
                LibFluidsynth.fluid_event_reverb_send_3(Handle, channel, val);
        }

        public void ChorusSend(int channel, short val)
        {
            ThrowIfDisposed();

            if (LibFluidsynth.LibraryVersion == 2)
                LibFluidsynth.fluid_event_chorus_send_2(Handle, channel, val);
            else
                LibFluidsynth.fluid_event_chorus_send_3(Handle, channel, val);
        }

        public void KeyPressure(int channel, short key, short val)
        {
            ThrowIfDisposed();

            if (LibFluidsynth.LibraryVersion == 2)
                LibFluidsynth.fluid_event_key_pressure_2(Handle, channel, key, val);
            else
                LibFluidsynth.fluid_event_key_pressure_3(Handle, channel, key, val);
        }

        public void ChannelPressure(int channel, short val)
        {
            ThrowIfDisposed();

            if (LibFluidsynth.LibraryVersion == 2)
                LibFluidsynth.fluid_event_channel_pressure_2(Handle, channel, val);
            else
                LibFluidsynth.fluid_event_channel_pressure_3(Handle, channel, val);
        }

        public void SystemReset()
        {
            ThrowIfDisposed();
            LibFluidsynth.fluid_event_system_reset(Handle);
        }

        public void Unregistering()
        {
            ThrowIfDisposed();
            LibFluidsynth.fluid_event_unregistering(Handle);
        }
    }
}