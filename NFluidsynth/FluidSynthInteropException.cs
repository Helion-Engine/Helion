using System;

namespace NFluidsynth
{
    public class FluidSynthInteropException : Exception
    {
        public FluidSynthInteropException() : this("Fluidsynth native error")
        {
        }

        public FluidSynthInteropException(string message) : base(message)
        {
        }

        public FluidSynthInteropException(string message, Exception innerException) : base(message, innerException)
        {
        }
    }
}