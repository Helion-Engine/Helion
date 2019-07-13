using System.Collections.Generic;
using Helion.Util.Extensions;
using Helion.Util.Geometry;

namespace Helion.Input
{
    /// <summary>
    /// An object that remembers items being consumed (or read).
    /// </summary>
    /// <remarks>
    /// <para>An issue with layered windows and systems is that input can pass
    /// through multiple layers. This leads to problems because input events
    /// can leak to the next layer, which is undesirable.</para>
    /// <para>This class solves the problem by removing the key if it is read 
    /// by some caller. For example, suppose we want to pass input through a 
    /// menu, a hud, and then to the player in a world. If we have a menu open,
    /// it can consume the mouse x/y movements and any mouse clicks. This way
    /// if  it does, the data structure will clear those fields and any other 
    /// layers  after it will poll an empty result from mouse movements because
    /// it was  "consumed" by the menu layer.</para>
    /// <para>This allows for construction of this object from an input 
    /// collection, and then one can recursively pass it to all the different 
    /// layers in a worry-free way.</para>
    /// </remarks>
    public class ConsumableInput
    {
        private readonly HashSet<InputKey> m_keysDown = new HashSet<InputKey>();
        private readonly HashSet<InputKey> m_keysPressed = new HashSet<InputKey>();
        private readonly IList<char> m_typedCharacters = new List<char>();
        private Vec2I m_mouseDelta;
        private int m_mouseScroll;

        /// <summary>
        /// Creates a new consumable input from the provided input collection.
        /// </summary>
        /// <param name="inputEvent">The collection to populate the object data
        /// with.</param>
        public ConsumableInput(InputEvent inputEvent)
        {
            foreach (InputKey inputKey in inputEvent.InputDown) 
            {
                m_keysDown.Add(inputKey);

                if (!inputEvent.InputPrevDown.Contains(inputKey))
                    m_keysPressed.Add(inputKey);
            }

            inputEvent.CharactersTyped.ForEach(m_typedCharacters.Add);
            m_mouseDelta = inputEvent.MouseInput.Delta;
            m_mouseScroll = inputEvent.MouseInput.ScrollDelta;
        }

        /// <summary>
        /// Consumes all the input so no later consumer sees anything.
        /// </summary>
        public void ConsumeAll()
        {
            m_keysDown.Clear();
            m_keysPressed.Clear();
            m_typedCharacters.Clear();
            m_mouseDelta = new Vec2I(0, 0);
            m_mouseScroll = 0;
        }

        /// <summary>
        /// Consumes the key, returns the result of whether it's down or
        /// pressed depending whether it's been consumed or not.
        /// </summary>
        /// <param name="textKey">The name of the key to check.</param>
        /// <returns>True if it was pressed or down, false if it was not or if
        /// it was consumed before this invocation.</returns>
        public bool ConsumeKeyPressedOrDown(string textKey)
        {
            return ConsumeKeyPressedOrDown(InputKeyHelper.ToKey(textKey));
        }

        /// <summary>
        /// Consumes the key, returns the result of whether it's down or
        /// pressed depending whether it's been consumed or not.
        /// </summary>
        /// <param name="inputKey">The key to check.</param>
        /// <returns>True if it was pressed or down, false if it was not or if
        /// it was consumed before this invocation.</returns>
        public bool ConsumeKeyPressedOrDown(InputKey inputKey)
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
        /// <param name="textKey">The name of the key to check.</param>
        /// <returns>True if it was pressed, false if it was not or if it was
        /// consumed before this invocation.</returns>
        public bool ConsumeKeyPressed(string textKey)
        {
            return ConsumeKeyPressed(InputKeyHelper.ToKey(textKey));
        }

        /// <summary>
        /// Consumes the key, returns the result of whether it's pressed 
        /// depending whether it's been consumed or not.
        /// </summary>
        /// <param name="inputKey">The key to check.</param>
        /// <returns>True if it was pressed, false if it was not or if it was
        /// consumed before this invocation.</returns>
        public bool ConsumeKeyPressed(InputKey inputKey)
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
            m_mouseDelta = new Vec2I(0, 0);
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
        /// <returns>The typed characters, or an empty list if it was already 
        /// consumed.</returns>
        public IList<char> ConsumeTypedCharacters()
        {
            IList<char> typedChars = m_typedCharacters.Copy();
            m_typedCharacters.Clear();
            return typedChars;
        }
    }
}