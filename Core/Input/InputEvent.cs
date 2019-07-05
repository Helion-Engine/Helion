using System.Collections.Generic;

namespace Helion.Input
{
    public class InputEvent
    {
        /// <summary>
        /// All the input currently down since ticking.
        /// </summary>
        public readonly HashSet<InputKey> InputDown = new HashSet<InputKey>();

        /// <summary>
        /// The input that was down in the previous tick.
        /// </summary>
        public readonly HashSet<InputKey> InputPrevDown = new HashSet<InputKey>();

        /// <summary>
        /// An ordered list of all the characters typed. Any character towards
        /// the front of the list was typed before the ones after it.
        /// </summary>
        public readonly List<char> CharactersTyped = new List<char>();

        /// <summary>
        /// The mouse input data.
        /// </summary>
        public readonly MouseInputData MouseInput = new MouseInputData();
    }
}