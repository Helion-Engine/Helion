using System.Collections.Generic;
using Helion.Geometry.Vectors;
using Helion.Util.Extensions;

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

        private readonly HashSet<Key> m_keysDown = new();
        private readonly HashSet<Key> m_keysPressed = new();
        private readonly HashSet<Key> m_previouslyPressed = new();
        private readonly List<char> m_typedCharacters = new();
        private Vec2I m_mouseDelta;
        private double m_mouseScroll;

        internal InputEvent(InputManager manager)
        {
            Manager = manager;
        }

        public void Set(double mouseScroll, IEnumerable<Key> down, IEnumerable<Key> pressed, IEnumerable<Key> previouslyPressed)
        {
            m_mouseDelta = Manager.MouseMove;
            m_mouseScroll = mouseScroll;

            m_typedCharacters.Clear();
            m_keysDown.Clear();
            m_keysPressed.Clear();
            m_previouslyPressed.Clear();

            foreach (char c in Manager.TypedCharacters)
                m_typedCharacters.Add(c);
            foreach (var key in down)
                m_keysDown.Add(key);
            foreach (var key in pressed)
                m_keysPressed.Add(key);
            foreach (var key in previouslyPressed)
                m_previouslyPressed.Add(key);
        }
        
        public bool WasPreviouslyPressed(Key key) => m_previouslyPressed.Contains(key);

        /// <summary>
        /// Consumes all the input so no later consumer sees anything.
        /// </summary>
        public void ConsumeAll()
        {
            m_keysDown.Clear();
            m_keysPressed.Clear();
            m_typedCharacters.Clear();
            m_mouseDelta = Vec2I.Zero;
            m_mouseScroll = 0;
        }

        /// <summary>
        /// Checks if a key is pressed without consuming it.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>True if it was just pressed, false if not.</returns>
        public bool PeekKeyPressed(Key key)
        {
            return m_keysPressed.Contains(key);
        }

        /// <summary>
        /// Checks if a key is down without consuming it.
        /// </summary>
        /// <param name="key">The key.</param>
        /// <returns>True if it is down, false if not.</returns>
        public bool PeekKeyDown(Key key)
        {
            return m_keysDown.Contains(key);
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
        /// If a key has been held down for this polling and the previous polling,
        /// then it will be consumed.
        /// </summary>
        /// <param name="key">The key to check.</param>
        /// <returns>True if so, false otherwise (and no consumption happened).
        /// </returns>
        public bool ConsumeKeyContinuallyDown(Key key)
        {
            bool contains = m_keysPressed.Contains(key);
            bool previouslyContains = m_previouslyPressed.Contains(key);

            if (!contains || !previouslyContains)
                return false;
            
            m_keysPressed.Remove(key);
            m_previouslyPressed.Remove(key);
            return true;
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
        public double ConsumeMouseScroll()
        {
            double scroll = m_mouseScroll;
            m_mouseScroll = 0;
            return scroll;
        }

        /// <summary>
        /// Consumes the typed characters.
        /// </summary>
        public void ConsumeTypedCharacters()
        {
            m_typedCharacters.Clear();
        }

        /// <summary>
        /// Gets the typed characters.
        /// </summary>
        /// <returns>The typed characters, or null if there are none.</returns>
        public List<char> GetTypedCharacters() =>  m_typedCharacters;

        /// <summary>
        /// Consumes all letters provided. Intended so that capitals can be
        /// consumed as well (ex: Consume('a', 'A')).
        /// </summary>
        /// <param name="letters">The letters to consume.</param>
        /// <returns>True if one or more of the characters was found, false if
        /// not.</returns>
        public bool ConsumeTypedCharacter(params char[] letters)
        {
            bool found = false;

            foreach (char letter in letters)
            {
                if (!m_typedCharacters.Contains(letter))
                    continue;

                m_typedCharacters.RemoveAll(x => x == letter);
                found = true;
            }

            return found;
        }

        /// <summary>
        /// Checks if any keys have been pressed.
        /// </summary>
        /// <returns>True if one or more keys were just pressed, false
        /// otherwise.</returns>
        public bool HasAnyKeyPressed() => !m_keysPressed.Empty();
    }
}
