using NFluidsynth.Native;

namespace NFluidsynth
{
    public class MidiRouter : FluidsynthObject
    {
        // Keep these around to prevent the GC eating them.
        // ReSharper disable once NotAccessedField.Local
        private readonly Settings _settings;
        // ReSharper disable once NotAccessedField.Local
        private readonly LibFluidsynth.handle_midi_event_func_t _handler;

        public unsafe MidiRouter(Settings settings, MidiEventHandler handler)
            : base(LibFluidsynth.new_fluid_midi_router(settings.Handle,
                Utility.PassDelegatePointer<LibFluidsynth.handle_midi_event_func_t>(
                    (d, e) =>
                    {
                        using (var ev = new MidiEvent(e))
                        {
                            return handler(ev);
                        }
                    },
                    out var wrapHandler), null))
        {
            _settings = settings;
            _handler = wrapHandler;
        }

        protected override void Dispose(bool disposed)
        {
            if (!Disposed)
            {
                LibFluidsynth.delete_fluid_midi_router(Handle);
            }

            base.Dispose(disposed);
        }

        public void SetDefaultRules()
        {
            ThrowIfDisposed();
            LibFluidsynth.fluid_midi_router_set_default_rules(Handle);
        }

        public void ClearRules()
        {
            ThrowIfDisposed();
            LibFluidsynth.fluid_midi_router_clear_rules(Handle);
        }

        public void AddRule(MidiRouterRule rule, FluidMidiRouterRuleType type)
        {
            ThrowIfDisposed();
            LibFluidsynth.fluid_midi_router_add_rule(Handle, rule.Handle, type);
        }
    }
}