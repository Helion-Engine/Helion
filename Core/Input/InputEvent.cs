using System.Collections.Generic;
using Helion.Util.Geometry.Vectors;

namespace Helion.Input
{
    /// <summary>
    /// An event that can be passed through layers safely.
    /// </summary>
    /// <remarks>
    /// The input values on this object are 'consumable', which means that when
    /// they are queried, it is assumed that they are not to be visible by any
    /// other viewer after. This makes it so if we consume input on the console
    /// then that input should no longer be visible to the world. This solves
    /// the problem of an input leaking through multiple layers and causing
    /// unexpected actions (which happens in ports like ZDoom).
    /// </remarks>
    public class InputEvent
    {
        /// <summary>
        /// The creator of this event. This object can be used to get the raw
        /// input state of the application.
        /// </summary>
        public readonly InputManager Manager;

        private readonly HashSet<Key> m_keysDown;
        private readonly HashSet<Key> m_keysPressed;
        private string m_typedCharacters;
        private Vec2I m_mouseDelta;
        private int m_mouseScroll;

        internal InputEvent(InputManager manager, IEnumerable<Key> down, IEnumerable<Key> pressed)
        {
            Manager = manager;
            m_typedCharacters = manager.TypedCharacters;
            m_mouseDelta = manager.MouseMove;
            m_mouseScroll = manager.MouseScroll;
            m_keysDown = new(down);
            m_keysPressed = new(pressed);
        }

        /// <summary>
        /// Consumes all the input so no later consumer sees anything.
        /// </summary>
        public void ConsumeAll()
        {
            m_keysDown.Clear();
            m_keysPressed.Clear();
            m_typedCharacters = "";
            m_mouseDelta = Vec2I.Zero;
            m_mouseScroll = 0;
        }

        /// <summary>
        /// Consumes the key, returns the result of whether it's down or
        /// pressed depending whether it's been consumed or not.
        /// </summary>
        /// <param name="inputKey">The key to check.</param>
        /// <returns>True if it was pressed or down, false if it was not or if
        /// it was consumed before this invocation.</returns>
        public bool ConsumeKeyPressedOrDown(Key inputKey)
        {
            bool contains = m_keysPressed.Contains(inputKey) || m_keysDown.Contains(inputKey);
            m_keysPressed.Remove(inputKey);
            m_keysDown.Remove(inputKey);
            return contains;
        }

        /// <summary>
        /// Consumes the key, returns the result of whether it's pressed
        /// depending whether it's been consumed or not.
        /// </summary>
        /// <param name="inputKey">The key to check.</param>
        /// <returns>True if it was pressed, false if it was not or if it was
        /// consumed before this invocation.</returns>
        public bool ConsumeKeyPressed(Key inputKey)
        {
            bool contains = m_keysPressed.Contains(inputKey);
            m_keysPressed.Remove(inputKey);
            return contains;
        }

        /// <summary>
        /// Consumes the mouse movement.
        /// </summary>
        /// <returns>The mouse movement, or an zero value result if it was
        /// already consumed.</returns>
        public Vec2I ConsumeMouseDelta()
        {
            Vec2I delta = m_mouseDelta;
            m_mouseDelta = Vec2I.Zero;
            return delta;
        }

        /// <summary>
        /// Consumes the mouse scroll movement.
        /// </summary>
        /// <returns>The mouse scroll movement, or an zero value result if it
        /// was already consumed.</returns>
        public int ConsumeMouseScroll()
        {
            int scroll = m_mouseScroll;
            m_mouseScroll = 0;
            return scroll;
        }

        /// <summary>
        /// Consumes the typed characters.
        /// </summary>
        /// <returns>The typed characters, or null if there are none.</returns>
        public string ConsumeTypedCharacters()
        {
            string typedChars = m_typedCharacters;
            m_typedCharacters = "";
            return typedChars;
        }
    }
}
