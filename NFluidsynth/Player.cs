using System;
using NFluidsynth.Native;

namespace NFluidsynth
{
    public class Player : FluidsynthObject
    {
        // Keep this here so the GC doesn't erase it from existence
        private LibFluidsynth.handle_midi_event_func_t _handler;

        public Player(Synth synth)
            : base(LibFluidsynth.new_fluid_player(synth.Handle))
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                LibFluidsynth.delete_fluid_player(Handle);
            }

            base.Dispose(disposing);
        }

        public void Add(string midifile)
        {
            ThrowIfDisposed();
            LibFluidsynth.fluid_player_add(Handle, midifile);
        }

        #if NETCOREAPP
        public unsafe void AddMem(ReadOnlySpan<byte> buffer)
        {
            ThrowIfDisposed();
            fixed (byte* ptr = buffer)
                AddMem ((IntPtr) ptr, buffer.Length);
        }
        #endif
        
        public unsafe void AddMem(byte [] buffer, int offset, int length)
        {
            ThrowIfDisposed();
            fixed (byte* ptr = buffer)
                AddMem ((IntPtr) (ptr + offset), buffer.Length);
        }

        public void AddMem(IntPtr buffer, int length)
        {
            ThrowIfDisposed();
            LibFluidsynth.fluid_player_add_mem(Handle, buffer, length);
        }

        public void Play()
        {
            ThrowIfDisposed();
            LibFluidsynth.fluid_player_play(Handle);
        }

        public void Stop()
        {
            ThrowIfDisposed();
            LibFluidsynth.fluid_player_stop(Handle);
        }

        public void Join()
        {
            ThrowIfDisposed();
            LibFluidsynth.fluid_player_join(Handle);
        }

        public void SetLoop(int loop)
        {
            ThrowIfDisposed();
            LibFluidsynth.fluid_player_set_loop(Handle, loop);
        }
        
        public void Seek(int tick)
        {
            ThrowIfDisposed();
            LibFluidsynth.fluid_player_seek(Handle, tick);
        }

        [Obsolete("Use " + nameof(MidiTempo) + " property instead.")]
        public void SetTempo(int tempo)
        {
            ThrowIfDisposed();
            LibFluidsynth.fluid_player_set_midi_tempo(Handle, tempo);
        }

        [Obsolete("Use " + nameof(Bpm) + " property instead.")]
        public void SetBpm(int bpm)
        {
            ThrowIfDisposed();
            LibFluidsynth.fluid_player_set_bpm(Handle, bpm);
        }

        public int CurrentTick
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_player_get_current_tick(Handle);
            }
        }

        public int GetTotalTicks
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_player_get_total_ticks(Handle);
            }
        }

        public int Bpm
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_player_get_bpm(Handle);
            }
            set
            {
                ThrowIfDisposed();
                LibFluidsynth.fluid_player_set_bpm(Handle, value);
            }
        }

        public int MidiTempo
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_player_get_midi_tempo(Handle);
            }
            set
            {
                ThrowIfDisposed();
                LibFluidsynth.fluid_player_set_midi_tempo(Handle, value);
            }
        }

        public unsafe void SetPlaybackCallback(MidiEventHandler handler)
        {
            ThrowIfDisposed();
            LibFluidsynth.fluid_player_set_playback_callback(Handle,
                Utility.PassDelegatePointer<LibFluidsynth.handle_midi_event_func_t>(
                    (d, e) =>
                    {
                        using (var ev = new MidiEvent(e))
                        {
                            return handler(ev);
                        }
                    }, out var b), null);
            _handler = b;
        }

        public FluidPlayerStatus Status
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_player_get_status(Handle);
            }
        }
    }
}
