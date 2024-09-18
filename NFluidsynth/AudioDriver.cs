using NFluidsynth.Native;

namespace NFluidsynth
{
    public class AudioDriver : FluidsynthObject
    {
        public delegate int AudioHandler(float[][] outBuffer);

        public AudioDriver(Settings settings, Synth synth)
            : base(LibFluidsynth.new_fluid_audio_driver(settings.Handle, synth.Handle))
        {
        }

        protected override void Dispose(bool disposing)
        {
            if (!Disposed)
            {
                LibFluidsynth.delete_fluid_audio_driver(Handle);
            }

            base.Dispose(disposing);
        }
    }
}