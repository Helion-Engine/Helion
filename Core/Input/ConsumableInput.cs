using Helion.Util.Geometry;
using Helion.Util.Extensions;
using System.Collections.Generic;

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
        private readonly HashSet<InputKey> keysDown = new HashSet<InputKey>();
        private readonly HashSet<InputKey> keysPressed = new HashSet<InputKey>();
        private readonly IList<char> typedCharacters = new List<char>();
        private Vec2I mouseDelta;
        private int mouseScroll;

        /// <summary>
        /// Creates a new consumable input from the provided input collection.
        /// </summary>
        /// <param name="inputEvent">The collection to populate the object data
        /// with.</param>
        public ConsumableInput(InputEvent inputEvent)
        {
            foreach (InputKey inputKey in inputEvent.InputDown) {
                keysDown.Add(inputKey);

                if (!inputEvent.InputPrevDown.Contains(inputKey))
                    keysPressed.Add(inputKey);
            }

            inputEvent.CharactersTyped.ForEach(typedCharacters.Add);
            mouseDelta = inputEvent.MouseInput.Delta;
            mouseScroll = inputEvent.MouseInput.ScrollDelta;
        }

        /// <summary>
        /// Consumes all the input so no later consumer sees anything.
        /// </summary>
        public void ConsumeAll()
        {
            keysDown.Clear();
            keysPressed.Clear();
            typedCharacters.Clear();
            mouseDelta = new Vec2I(0, 0);
            mouseScroll = 0;
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
            bool contains = keysPressed.Contains(inputKey) || keysDown.Contains(inputKey);
            keysPressed.Remove(inputKey);
            keysDown.Remove(inputKey);
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
            bool contains = keysPressed.Contains(inputKey);
            keysPressed.Remove(inputKey);
            return contains;
        }

        /// <summary>
        /// Consumes the mouse movement.
        /// </summary>
        /// <returns>The mouse movement, or an zero value result if it was
        /// already consumed.</returns>
        public Vec2I ConsumeMouseDelta()
        {
            Vec2I delta = mouseDelta;
            mouseDelta = new Vec2I(0, 0);
            return delta;
        }

        /// <summary>
        /// Consumes the mouse scroll movement.
        /// </summary>
        /// <returns>The mouse scroll movement, or an zero value result if it
        /// was already consumed.</returns>
        public int ConsumeMouseScroll()
        {
            int scroll = mouseScroll;
            mouseScroll = 0;
            return scroll;
        }

        /// <summary>
        /// Consumes the typed characters.
        /// </summary>
        /// <returns>The typed characters, or an empty list if it was already 
        /// consumed.</returns>
        public IList<char> ConsumeTypedCharacters()
        {
            IList<char> typedChars = typedCharacters.Copy();
            typedCharacters.Clear();
            return typedChars;
        }
    }
}
