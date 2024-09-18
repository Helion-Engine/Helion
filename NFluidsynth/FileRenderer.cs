using System;
using NFluidsynth.Native;

namespace NFluidsynth
{
    public class FileRenderer : FluidsynthObject
    {
        public FileRenderer(Synth synth)
            : base(LibFluidsynth.new_fluid_file_renderer(synth.Handle))
        {
        }

        protected override void Dispose(bool disposing)
        {
            LibFluidsynth.delete_fluid_file_renderer(Handle);
        }

        public void ProcessBlock()
        {
            LibFluidsynth.fluid_file_renderer_process_block(Handle);
        }
    }
}