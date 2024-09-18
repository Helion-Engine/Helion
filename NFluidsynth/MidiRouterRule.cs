using NFluidsynth.Native;

namespace NFluidsynth
{
    public class MidiRouterRule : FluidsynthObject
    {
        public MidiRouterRule()
            : base(LibFluidsynth.new_fluid_midi_router_rule())
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                LibFluidsynth.delete_fluid_midi_router_rule(Handle);
            }

            base.Dispose(disposing);
        }

        public void SetChannel(int min, int max, float mul, int add)
        {
            LibFluidsynth.fluid_midi_router_rule_set_chan(Handle, min, max, mul, add);
        }

        public void SetParam1(int min, int max, float mul, int add)
        {
            LibFluidsynth.fluid_midi_router_rule_set_param1(Handle, min, max, mul, add);
        }

        public void SetParam2(int min, int max, float mul, int add)
        {
            LibFluidsynth.fluid_midi_router_rule_set_param2(Handle, min, max, mul, add);
        }
    }
}