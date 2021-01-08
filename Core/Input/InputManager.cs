using System.Collections.Generic;
using System.Text;
using Helion.Util.Geometry.Vectors;

namespace Helion.Input
{
    /// <summary>
    /// Manages all of the input. All methods here are based on the raw state,
    /// so they can be polled without worrying about values being cleared (in
    /// contrast to <see cref="InputEvent"/> where they are).
    /// </summary>
    public class InputManager
    {
        /// <summary>
        /// The mouse movement since the last update.
        /// </summary>
        public Vec2I MouseMove { get; private set; } = Vec2I.Zero;

        /// <summary>
        /// The mouse scrolling since the last update.
        /// </summary>
        public int MouseScroll { get; private set; }

        private readonly StringBuilder m_typedCharacters = new();
        private HashSet<Key> m_inputDown = new();
        private HashSet<Key> m_inputPrevDown = new();

        /// <summary>
        /// The characters typed on the keyboard since the last update.
        /// </summary>
        public string TypedCharacters => m_typedCharacters.ToString();

        /// <summary>
        /// Gets a read-only view of all the keys that are down.
        /// </summary>
        /// <remarks>
        /// This is useful for checking what keys are down, which helps for
        /// detecting input when the user is asked to press 'any key' for the
        /// setting of new key binds.
        /// </remarks>
        public IReadOnlySet<Key> KeysDown => m_inputDown;

        /// <summary>
        /// Checks if a key is down.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if so, false if not.</returns>
        public bool IsKeyDown(Key key) => m_inputDown.Contains(key);

        /// <summary>
        /// Checks if a key is not down.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if so, false if not.</returns>
        public bool IsKeyUp(Key key) => !IsKeyDown(key);

        /// <summary>
        /// Checks if a key was just pressed.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if so, false if not.</returns>
        public bool IsKeyPressed(Key key) => IsKeyDown(key) && !m_inputPrevDown.Contains(key);

        /// <summary>
        /// Checks if a key was just released.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if so, false if not.</returns>
        public bool IsKeyReleased(Key key) => !IsKeyDown(key) && m_inputPrevDown.Contains(key);

        private void ClearStates()
        {
            m_typedCharacters.Clear();
            m_inputPrevDown = m_inputDown;
            m_inputDown = new();
            MouseMove = Vec2I.Zero;
            MouseScroll = 0;
        }

        public InputEvent PollInput()
        {
            InputEvent inputEvent = new(this, m_inputDown, m_inputPrevDown);
            ClearStates();
            return inputEvent;
        }
    }
}
