using System;

namespace Helion.Input.Adapter
{
    /// <summary>
    /// An interface that maps some kind of input into a known key.
    /// </summary>
    /// <remarks>
    /// This is intended to be a middleman between some kind of input (ex: the
    /// input from OpenTK, SDL, WinForms, raw input... etc) which translates
    /// that input into a known type. This way we can have multiple adapters or
    /// change adapters during runtime.
    /// </remarks>
    public abstract class InputAdapter
    {
        /// <summary>
        /// The event emitter for input events.
        /// </summary>
        public event EventHandler<InputEventArgs> InputEventEmitter;

        // For some reason (probably the language implementation) we can't call
        // events from a child class, so we need to get them to do it this way.
        protected void EmitEvent(InputEventArgs inputEvent)
        {
            InputEventEmitter(this, inputEvent);
        }

        /// <summary>
        /// Polls the underlying system for input (if any).
        /// </summary>
        public abstract void PollInput();
    }
}
