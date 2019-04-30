using System.Collections.Generic;

namespace Helion.Input
{
    /// <summary>
    /// A collection of inputs that accumulate over time, and can be composed
    /// into a <see cref="ConsumableInput"/> object.
    /// </summary>
    /// <remarks>
    /// <para>Represents a collection of inputs and are tracked in an internal 
    /// state so the data structure contains a memory of what was pressed.
    /// </para>
    /// <para>Each input collection revolves around calling the tick() function
    /// for every frame or time span. Ticking the input collection prepares it 
    /// so that on the next iteration you can see what keys were pressed or 
    /// released.</para>
    /// <para>One useful feature is the ability to track inputs in multiple 
    /// collections by creating two InputCollection objects and filling them 
    /// both with the exact same input. One of them can be for your 'frame 
    /// input', and the other can be for the game ticker. This allows you to 
    /// track input that you want responses fast to (like the menu or some 
    /// overlay) while decoupling it from the game tick rate, which may be much 
    /// slower. You would do this because you do not want your "was pressed" 
    /// state to be tossed out quickly before you could even pick it up.</para>
    /// <para>This works because you will be constantly feeding it input data 
    /// over some span of time, so each 'tick' will be an accumulation of all 
    /// the past events.</para>
    /// </summary>
    public class InputCollection
    {
        /// <summary>
        /// All the input currentely down since ticking.
        /// </summary>
        public HashSet<InputKey> InputDown { get; private set; } = new HashSet<InputKey>();

        /// <summary>
        /// The input that was down in the previous tick.
        /// </summary>
        public HashSet<InputKey> InputPrevDown { get; private set; } = new HashSet<InputKey>();

        /// <summary>
        /// An ordered list of all the characters typed. Any character towards
        /// the front of the list was typed before the ones after it.
        /// </summary>
        public List<char> CharactersTyped { get; private set; } = new List<char>();

        /// <summary>
        /// The mouse input data.
        /// </summary>
        public readonly MouseInputData MouseInput = new MouseInputData();

        /// <summary>
        /// Ticks the input, meaning its state is reset for the next wave of
        /// input.
        /// </summary>
        public void Tick()
        {
            InputPrevDown = InputDown;
            CharactersTyped.Clear();
            MouseInput.Reset();
        }

        /// <summary>
        /// Checks if a key is down.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if it's down, false if not.</returns>
        public bool IsDown(InputKey key)
        {
            return InputDown.Contains(key);
        }

        /// <summary>
        /// Checks if a key wast just pressed this tick.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if it's just pressed, false if not.</returns>
        public bool IsPressed(InputKey key)
        {
            return IsDown(key) && !InputPrevDown.Contains(key);
        }

        /// <summary>
        /// Checks if a key is not down.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if it's up, false if not.</returns>
        public bool IsUp(InputKey key)
        {
            return !InputDown.Contains(key);
        }

        /// <summary>
        /// Checks if a key wast just released this tick.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if it's just released, false if not.</returns>
        public bool IsReleased(InputKey key)
        {
            return IsUp(key) && InputPrevDown.Contains(key);
        }

        /// <summary>
        /// Handles an input event by storing its data.
        /// </summary>
        /// <param name="inputEvent">The event to get the data from.</param>
        public void HandleInputEvent(InputEventArgs inputEvent)
        {
            foreach (InputKey key in inputEvent.InputDown)
                InputDown.Add(key);
            foreach (InputKey key in inputEvent.InputUp)
                InputDown.Remove(key);
            inputEvent.CharactersTyped.AddRange(CharactersTyped);
            MouseInput.Add(inputEvent.MouseInput);
        }
    }
}
