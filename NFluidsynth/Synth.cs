using System;
using System.Text;
using NFluidsynth.Native;

namespace NFluidsynth
{
    public class Synth : FluidsynthObject
    {
        public Synth(Settings settings)
            : base(LibFluidsynth.new_fluid_synth(settings.Handle))
        {
            Settings = settings;
        }

        protected override void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                LibFluidsynth.delete_fluid_synth(Handle);
            }

            base.Dispose(disposing);
        }

        public Settings Settings { get; }

        private static void OnError(string message)
        {
            throw new FluidSynthInteropException(message);
        }

        public void NoteOn(int channel, int key, int vel)
        {
            ThrowIfDisposed();
            if (LibFluidsynth.fluid_synth_noteon(Handle, channel, key, vel) != 0)
            {
                OnError("noteon operation failed");
            }
        }

        public void NoteOff(int channel, int key)
        {
            ThrowIfDisposed();
            // not sure if we should always raise exception, it seems that it also returns FLUID_FAILED for not-on-state note.
            if (LibFluidsynth.fluid_synth_noteoff(Handle, channel, key) != 0)
            {
                OnError("noteoff operation failed");
            }
        }

        public void CC(int channel, int num, int val)
        {
            ThrowIfDisposed();
            if (LibFluidsynth.fluid_synth_cc(Handle, channel, num, val) != 0)
            {
                OnError("control change operation failed");
            }
        }

        public int GetCC(int channel, int num)
        {
            ThrowIfDisposed();
            if (LibFluidsynth.fluid_synth_get_cc(Handle, channel, num, out var ret) != 0)
            {
                OnError("control change get operation failed");
            }

            return ret;
        }

        #if NETCOREAPP
        public unsafe bool Sysex (ReadOnlySpan<byte> input, Span<byte> output, bool dryRun = false)
        {
            fixed (byte* iPtr = input)
            fixed (byte* oPtr = output)
                return Sysex ((IntPtr) iPtr, input.Length, (IntPtr) oPtr, output.Length, dryRun);
        }
        #endif

        public unsafe bool Sysex (byte [] input, int inputOffset, int inputLength, byte [] output, int outputOffset, int outputLength, bool dryRun = false)
        {
            fixed (byte* iPtr = input)
            fixed (byte* oPtr = output)
                return Sysex ((IntPtr) iPtr, inputLength, (IntPtr) oPtr, outputLength, dryRun);
        }

        public bool Sysex(IntPtr input, int inputLength, IntPtr output, int outputLength, bool dryRun = false)
        {
            ThrowIfDisposed();

            int outLen = outputLength;
            unsafe {
                if (LibFluidsynth.fluid_synth_sysex (Handle, (byte*) input, inputLength, (byte*) output, ref outLen, out var handled, dryRun) != 0) {
                    if (outLen != 0) {
                        OnError ("Output buffer is too small");
                    }

                    OnError ("sysex operation failed");
                }

                return handled;
            }
        }

        public void PitchBend(int channel, int val)
        {
            ThrowIfDisposed();

            if (LibFluidsynth.fluid_synth_pitch_bend(Handle, channel, val) != 0)
            {
                OnError("pitch bend change operation failed");
            }
        }

        public int GetPitchBend(int channel)
        {
            ThrowIfDisposed();

            if (LibFluidsynth.fluid_synth_get_pitch_bend(Handle, channel, out var ret) != 0)
            {
                OnError("pitch bend get operation failed");
            }

            return ret;
        }

        public void PitchWheelSens(int channel, int val)
        {
            ThrowIfDisposed();

            if (LibFluidsynth.fluid_synth_pitch_wheel_sens(Handle, channel, val) != 0)
            {
                OnError("pitch wheel sens change operation failed");
            }
        }

        public int GetPitchWheelSens(int channel)
        {
            ThrowIfDisposed();

            if (LibFluidsynth.fluid_synth_get_pitch_wheel_sens(Handle, channel, out var ret) != 0)
            {
                OnError("pitch wheel sens get operation failed");
            }

            return ret;
        }

        public void ProgramChange(int channel, int program)
        {
            ThrowIfDisposed();

            if (LibFluidsynth.fluid_synth_program_change(Handle, channel, program) != 0)
            {
                OnError("program change operation failed");
            }
        }

        public void ChannelPressure(int channel, int val)
        {
            ThrowIfDisposed();

            if (LibFluidsynth.fluid_synth_channel_pressure(Handle, channel, val) != 0)
            {
                OnError("channel pressure change operation failed");
            }
        }

        public void KeyPressure(int channel, int key, int val)
        {
            ThrowIfDisposed();

            if (LibFluidsynth.fluid_synth_key_pressure(Handle, channel, key, val) != 0)
            {
                OnError("key pressure change operation failed");
            }
        }

        public void BankSelect(int channel, uint bank)
        {
            ThrowIfDisposed();

            if (LibFluidsynth.fluid_synth_bank_select(Handle, channel, bank) != 0)
            {
                OnError("bank select operation failed");
            }
        }

        public void SoundFontSelect(int channel, uint soundFontId)
        {
            ThrowIfDisposed();

            if (LibFluidsynth.fluid_synth_sfont_select(Handle, channel, soundFontId) != 0)
            {
                OnError("sound font select operation failed");
            }
        }

        public void ProgramSelect(int channel, uint soundFontId, uint bank, uint preset)
        {
            ThrowIfDisposed();

            if (LibFluidsynth.fluid_synth_program_select(Handle, channel, soundFontId, bank, preset) != 0)
            {
                OnError("program select operation failed");
            }
        }

        public void ProgramSelectBySoundFontName(int channel, string soundFontName, uint bank, uint preset)
        {
            ThrowIfDisposed();

            if (LibFluidsynth.fluid_synth_program_select_by_sfont_name(Handle, channel, soundFontName, bank, preset) !=
                0)
            {
                OnError("program select (by sound font name) operation failed");
            }
        }

        public void GetProgram(int channel, out int soundFontId, out int bank, out int preset)
        {
            ThrowIfDisposed();

            if (LibFluidsynth.fluid_synth_get_program(Handle, channel, out soundFontId, out bank, out preset) != 0)
            {
                OnError("program get operation failed");
            }
        }

        public void UnsetProgram(int channel)
        {
            ThrowIfDisposed();

            if (LibFluidsynth.fluid_synth_unset_program(Handle, channel) != 0)
            {
                OnError("program unset operation failed");
            }
        }

        public void ProgramReset()
        {
            ThrowIfDisposed();

            if (LibFluidsynth.fluid_synth_program_reset(Handle) != 0)
            {
                OnError("program reset operation failed");
            }
        }

        public void SystemReset()
        {
            ThrowIfDisposed();

            if (LibFluidsynth.fluid_synth_system_reset(Handle) != 0)
            {
                OnError("system reset operation failed");
            }
        }

        // fluid_synth_get_channel_preset() is deprecated, so I don't bind it.
        // Then fluid_synth_start() takes fluid_preset_t* which is returned only by this deprecated function, so I don't bind it either.
        // Then fluid_synth_stop() is paired by the function above, so I don't bind it either.

        public uint LoadSoundFont(string filename, bool resetPresets)
        {
            ThrowIfDisposed();
            int result = LibFluidsynth.fluid_synth_sfload(Handle, filename, resetPresets);
            if (result < 0)
                OnError("sound font load operation failed");

            return (uint) result;
        }

        public void ReloadSoundFont(uint id)
        {
            ThrowIfDisposed();
            if (LibFluidsynth.fluid_synth_sfreload(Handle, id) != 0)
                OnError("sound font reload operation failed");
        }

        public void UnloadSoundFont(uint id, bool resetPresets)
        {
            ThrowIfDisposed();
            if (LibFluidsynth.fluid_synth_sfunload(Handle, id, resetPresets) != 0)
                OnError("sound font unload operation failed");
        }

        public void AddSoundFont(SoundFont soundFont)
        {
            ThrowIfDisposed();
            if (LibFluidsynth.fluid_synth_add_sfont(Handle, soundFont.Handle) != 0)
                OnError("sound font add operation failed");
        }

        public void RemoveSoundFont(SoundFont soundFont)
        {
            ThrowIfDisposed();
            if (LibFluidsynth.fluid_synth_remove_sfont(Handle, soundFont.Handle) != 0)
            {
                OnError("sound font remove operation failed");
            }
        }

        public int FontCount
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_synth_sfcount(Handle);
            }
        }

        public SoundFont GetSoundFont(uint index)
        {
            ThrowIfDisposed();
            IntPtr ret = LibFluidsynth.fluid_synth_get_sfont(Handle, index);
            return ret == IntPtr.Zero ? null : new SoundFont(ret);
        }

        public SoundFont GetSoundFontById(uint id)
        {
            ThrowIfDisposed();
            IntPtr ret = LibFluidsynth.fluid_synth_get_sfont_by_id(Handle, id);
            return ret == IntPtr.Zero ? null : new SoundFont(ret);
        }

        public SoundFont GetSoundFontByName(string name)
        {
            ThrowIfDisposed();
            IntPtr ret = LibFluidsynth.fluid_synth_get_sfont_by_name(Handle, name);
            return ret == IntPtr.Zero ? null : new SoundFont(ret);
        }

        public void SetBankOffset(int soundFontId, int offset)
        {
            ThrowIfDisposed();
            if (LibFluidsynth.fluid_synth_set_bank_offset(Handle, soundFontId, offset) != 0)
                OnError("bank offset set operation failed");
        }

        public void GetBankOffset(int soundFontId)
        {
            ThrowIfDisposed();
            LibFluidsynth.fluid_synth_get_bank_offset(Handle, soundFontId);
        }

        public void SetReverb(double roomSize, double damping, double width, double level)
        {
            ThrowIfDisposed();
            LibFluidsynth.fluid_synth_set_reverb(Handle, roomSize, damping, width, level);
        }

        public void SetReverbOn(bool enabled)
        {
            ThrowIfDisposed();
            LibFluidsynth.fluid_synth_set_reverb_on(Handle, enabled);
        }

        public double ReverbRoomSize
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_synth_get_reverb_roomsize(Handle);
            }
        }

        public double ReverbDamp
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_synth_get_reverb_damp(Handle);
            }
        }

        public double ReverbLevel
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_synth_get_reverb_level(Handle);
            }
        }

        public double ReverbWidth
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_synth_get_reverb_width(Handle);
            }
        }

        public void SetChorus(int numVoices, double level, double speed, double depthMS, FluidChorusMod type)
        {
            ThrowIfDisposed();
            LibFluidsynth.fluid_synth_set_chorus(Handle, numVoices, level, speed, depthMS, type);
        }

        public void SetChorusOn(bool enabled)
        {
            ThrowIfDisposed();
            LibFluidsynth.fluid_synth_set_chorus_on(Handle, enabled);
        }

        public int NumberOfChorusVoices
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_synth_get_chorus_nr(Handle);
            }
        }

        public double ChorusLevel
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_synth_get_chorus_level(Handle);
            }
        }

        public double ChorusSpeedHz
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_synth_get_chorus_speed_Hz(Handle);
            }
        }

        public double ChorusDepthMS
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_synth_get_chorus_depth_ms(Handle);
            }
        }

        public FluidChorusMod ChorusType
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_synth_get_chorus_type(Handle);
            }
        }

        public int MidiChannelCount
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_synth_count_midi_channels(Handle);
            }
        }

        public int AudioChannelCount
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_synth_count_audio_channels(Handle);
            }
        }

        public int AudioGroupCount
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_synth_count_audio_groups(Handle);
            }
        }

        public int EffectChannelCount
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_synth_count_effects_channels(Handle);
            }
        }

        public void SetChannelRate(float sampleRate)
        {
            ThrowIfDisposed();
            LibFluidsynth.fluid_synth_set_sample_rate(Handle, sampleRate);
        }

        public float Gain
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_synth_get_gain(Handle);
            }
            set
            {
                ThrowIfDisposed();
                LibFluidsynth.fluid_synth_set_gain(Handle, value);
            }
        }

        public int Polyphony
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_synth_get_polyphony(Handle);
            }
            set
            {
                ThrowIfDisposed();
                LibFluidsynth.fluid_synth_set_polyphony(Handle, value);
            }
        }

        public int ActiveVoiceCount
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_synth_get_active_voice_count(Handle);
            }
        }

        public int InternalBufferSize
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_synth_get_internal_bufsize(Handle);
            }
        }

        public void SetInterpolationMethod(int channel, FluidInterpolation interpolationMethod)
        {
            ThrowIfDisposed();
            if (LibFluidsynth.fluid_synth_set_interp_method(Handle, channel, interpolationMethod) != 0)
                OnError("interpolation method set operation failed");
        }

        public void SetGenerator(int channel, int param, float value)
        {
            ThrowIfDisposed();
            if (LibFluidsynth.fluid_synth_set_gen(Handle, channel, param, value) != 0)
                OnError("generator set operation failed");
        }

        public float GetGenerator(int channel, int param)
        {
            ThrowIfDisposed();
            return LibFluidsynth.fluid_synth_get_gen(Handle, channel, param);
        }

        #region Tuning

        #if NETCOREAPP
        public unsafe void ActivateKeyTuning (int bank, int prog, string name, ReadOnlySpan<double> pitch, bool apply)
        {
            fixed (double* pPtr = pitch)
                ActivateKeyTuning (bank, prog, name, (IntPtr) pPtr, pitch.Length, apply);
        }
        #endif

        public unsafe void ActivateKeyTuning (int bank, int prog, string name, double [] pitch, int pitchOffset, int pitchLength, bool apply)
        {
            fixed (double* pPtr = pitch)
                ActivateKeyTuning (bank, prog, name, (IntPtr) (pPtr + pitchOffset), pitchLength, apply);
        }

        public void ActivateKeyTuning(int bank, int prog, string name, IntPtr pitch, int pitchLength, bool apply)
        {
            ThrowIfDisposed();
            if (pitchLength != 128)
            {
                throw new ArgumentException("pitch span must be of 128 elements.");
            }

            unsafe
            {
                if (LibFluidsynth.fluid_synth_activate_key_tuning(Handle, bank, prog, name, (double*) pitch, apply) != 0)
                {
                    OnError("key tuning create operation failed");
                }
            }
        }

        #if NETCOREAPP
        public unsafe void ActivateOctaveTuning (int bank, int prog, string name, ReadOnlySpan<double> pitch,
            bool apply)
        {
            fixed (double* pPtr = pitch)
                ActivateOctaveTuning (bank, prog, name, (IntPtr) pPtr, pitch.Length, apply);
        }
        #endif

        public unsafe void ActivateOctaveTuning (int bank, int prog, string name, double [] pitch, int pitchOffset, int pitchLength, bool apply)
        {
            fixed (double* pPtr = pitch)
                ActivateOctaveTuning (bank, prog, name, (IntPtr) (pPtr + pitchOffset), pitchLength, apply);
        }

        public void ActivateOctaveTuning(int bank, int prog, string name, IntPtr pitch, int pitchLength, bool apply)
        {
            ThrowIfDisposed();
            if (pitchLength != 128)
            {
                throw new ArgumentException("pitch array must be of 128 elements.");
            }

            unsafe
            {
                if (LibFluidsynth.fluid_synth_activate_octave_tuning(Handle, bank, prog, name, (double*) pitch, apply) != 0)
                    OnError("key tuning create operation failed");
            }
        }

        #if NETCOREAPP
        public unsafe void TuneNotes (int bank, int prog, ReadOnlySpan<int> keys, ReadOnlySpan<double> pitch,
            bool apply)
        {
            fixed (int* kPtr = keys)
            fixed (double* pPtr = pitch)
                TuneNotes (bank, prog, (IntPtr) kPtr, keys.Length, (IntPtr) pPtr, pitch.Length, apply);
        }
        #endif

        public unsafe void TuneNotes (int bank, int prog, int [] keys, int keysOffset, int keysLength, double [] pitch, int pitchOffset, int pitchLength,
            bool apply)
        {
            fixed (int* kPtr = keys)
            fixed (double* pPtr = pitch)
                TuneNotes (bank, prog, (IntPtr) (kPtr + keysOffset), keysLength, (IntPtr) (pPtr + pitchOffset), pitchLength, apply);
        }

        public void TuneNotes(int bank, int prog, IntPtr keys, int keysLength, IntPtr pitch, int pitchLength, bool apply)
        {
            ThrowIfDisposed();
            if (keysLength != 128)
            {
                throw new ArgumentException("key array must be of 128 elements.");
            }

            if (pitchLength != 128)
            {
                throw new ArgumentException("pitch array must be of 128 elements.");
            }

            unsafe
            {
                if (LibFluidsynth.fluid_synth_tune_notes(Handle, bank, prog, keysLength, (int*) keys, (double*) pitch, apply) != 0)
                {
                    OnError("key tuning create operation failed");
                }
            }
        }

        public void ActivateTuning(int channel, int bank, int prog, bool apply)
        {
            ThrowIfDisposed();
            if (LibFluidsynth.fluid_synth_activate_tuning(Handle, channel, bank, prog, apply) != 0)
                OnError("tuning activate operation failed");
        }

        public void DeactivateTuning(int channel, bool apply)
        {
            ThrowIfDisposed();
            if (LibFluidsynth.fluid_synth_deactivate_tuning(Handle, channel, apply) != 0)
                OnError("tuning deactivate operation failed");
        }

        public void TuningIterationStart()
        {
            ThrowIfDisposed();
            LibFluidsynth.fluid_synth_tuning_iteration_start(Handle);
        }

        public bool TuningIterationNext(out int bank, out int prog)
        {
            ThrowIfDisposed();
            return LibFluidsynth.fluid_synth_tuning_iteration_next(Handle, out bank, out prog);
        }

        public unsafe double[] TuningDump(int bank, int prog, out string name)
        {
            ThrowIfDisposed();
            var ret = new double[128];
            var nm = new byte[64];

            fixed (double* rPtr = ret)
            fixed (byte* nPtr = nm)
            {
                LibFluidsynth.fluid_synth_tuning_dump(Handle, bank, prog, nPtr, nm.Length, rPtr);
            }

            name = Encoding.UTF8.GetString(nm, 0, nm.Length);
            return ret;
        }

        #endregion

        public double CpuLoad
        {
            get
            {
                ThrowIfDisposed();
                return LibFluidsynth.fluid_synth_get_cpu_load(Handle);
            }
        }

        #if NETCOREAPP
        public unsafe void WriteSample16 (int count, Span<ushort> leftOut, int leftOffset, int leftIncrement,
            Span<ushort> rightOut, int rightOffset, int rightIncrement)
        {
            fixed (ushort* lPtr = leftOut)
            fixed (ushort* rPtr = rightOut)
                WriteSample16 (count, (IntPtr) lPtr, leftOffset, leftOut.Length, leftIncrement,
                    (IntPtr) rPtr, rightOffset, rightOut.Length, rightIncrement);
        }
        #endif

        public unsafe void WriteSample16 (int count, ushort [] leftOut, int leftOffset, int leftOutLength, int leftIncrement,
            ushort [] rightOut, int rightOffset, int rightOutLength, int rightIncrement)
        {
            fixed (ushort* lPtr = leftOut)
            fixed (ushort* rPtr = rightOut)
                WriteSample16 (count, (IntPtr) lPtr, leftOffset, leftOut.Length, leftIncrement,
                    (IntPtr) rPtr, rightOffset, rightOut.Length, rightIncrement);
        }

        public void WriteSample16(int count, IntPtr leftOut, int leftOffset, int leftOutLength, int leftIncrement,
            IntPtr rightOut, int rightOffset, int rightOutLength, int rightIncrement)
        {
            ThrowIfDisposed();

            var leftNecessarySize = SampleOutputBufferSizeNecessary(count, leftOffset, leftIncrement);
            var rightNecessarySize = SampleOutputBufferSizeNecessary(count, rightOffset, rightIncrement);

            if (leftOutLength < leftNecessarySize)
            {
                throw new ArgumentException("Left output buffer is too short!", nameof(leftOut));
            }

            if (rightOutLength < rightNecessarySize)
            {
                throw new ArgumentException("Right output buffer is too short!", nameof(rightOut));
            }

            unsafe
            {
                if (LibFluidsynth.fluid_synth_write_s16(Handle, count, (ushort*) leftOut, leftOffset, leftIncrement,
                        (ushort*) rightOut, rightOffset, rightIncrement) != 0)
                {
                    OnError("16bit sample write operation failed");
                }
            }
        }

        #if NETCOREAPP
        public unsafe void WriteSampleFloat (int count, Span<float> leftOut, int leftOffset, int leftIncrement,
            Span<float> rightOut, int rightOffset, int rightIncrement)
        {
            fixed (float* lPtr = leftOut)
            fixed (float* rPtr = rightOut)
                WriteSampleFloat (count, (IntPtr) lPtr, leftOffset, leftOut.Length, leftIncrement,
                    (IntPtr) rPtr, rightOffset, rightOut.Length, rightIncrement);

        }
        #endif

        public unsafe void WriteSampleFloat(int count, float [] leftOut, int leftOffset, int leftOutLength, int leftIncrement,
            float [] rightOut, int rightOffset, int rightOutLength, int rightIncrement)
        {
            fixed (float* lPtr = leftOut)
            fixed (float* rPtr = rightOut)
                WriteSampleFloat (count, (IntPtr) lPtr, leftOffset, leftOut.Length, leftIncrement,
                    (IntPtr) rPtr, rightOffset, rightOut.Length, rightIncrement);
        }

        public void WriteSampleFloat(int count, IntPtr leftOut, int leftOffset, int leftOutLength, int leftIncrement,
            IntPtr rightOut, int rightOffset, int rightOutLength, int rightIncrement)
        {
            ThrowIfDisposed();

            var leftNecessarySize = SampleOutputBufferSizeNecessary(count, leftOffset, leftIncrement);
            var rightNecessarySize = SampleOutputBufferSizeNecessary(count, rightOffset, rightIncrement);

            if (leftOutLength < leftNecessarySize)
            {
                throw new ArgumentException("Left output buffer is too short!", nameof(leftOut));
            }

            if (rightOutLength < rightNecessarySize)
            {
                throw new ArgumentException("Right output buffer is too short!", nameof(rightOut));
            }

            unsafe
            {
                if (LibFluidsynth.fluid_synth_write_float(Handle, count, (float*) leftOut, leftOffset, leftIncrement,
                        (float*) rightOut, rightOffset, rightIncrement) != 0)
                    OnError("float sample write operation failed");
            }
        }

        private static int SampleOutputBufferSizeNecessary(int count, int offset, int increment)
        {
            if (count == 0)
            {
                // Fluidsynth writes nothing if count is zero, so passing a zero size buffer is valid I guess.
                // Why would you want to? No idea!
                return 0;
            }
            return 1 + (count - 1) * increment + offset;
        }

        public unsafe void Process(int length, int nFx, float** fx, int nOut, float** @out)
        {
            ThrowIfDisposed();
            if (LibFluidsynth.fluid_synth_process(Handle, length, nFx, fx, nOut, @out) != 0)
                OnError("float sample write operation failed");
        }

        public void AddSoundFontLoader(SoundFontLoader loader)
        {
            ThrowIfDisposed();
            LibFluidsynth.fluid_synth_add_sfloader(Handle, loader.Handle);
        }

        public Voice AllocateVoice(IntPtr sample, int channel, int key, int vel)
        {
            ThrowIfDisposed();
            var ret = LibFluidsynth.fluid_synth_alloc_voice(Handle, sample, channel, key, vel);
            if (ret == IntPtr.Zero)
                OnError("voice allocate operation failed");
            return new Voice(ret);
        }

        public void StartVoice(Voice voice)
        {
            ThrowIfDisposed();
            LibFluidsynth.fluid_synth_start_voice(Handle, voice.Handle);
        }

        public unsafe bool GetVoiceList(Voice[] voices, int voiceId)
        {
            ThrowIfDisposed();
            var arr = new IntPtr[voices.Length];
            fixed (IntPtr* aPtr = arr)
            {
                LibFluidsynth.fluid_synth_get_voicelist(Handle, aPtr, arr.Length, voiceId);
            }

            for (var i = 0; i < arr.Length; i++)
            {
                if (arr[i] == IntPtr.Zero)
                    return false;

                voices[i] = new Voice(arr[i]);
            }

            return true;
        }

        public int AllNotesOff(int channel)
        {
            ThrowIfDisposed();
            return LibFluidsynth.fluid_synth_all_notes_off(Handle, channel);
        }
    }
}
