using System.Collections;
using System.Collections.Generic;
using System.Drawing;

namespace Helion.Graphics.String
{
    /// <summary>
    /// A string where each character has a color component.
    /// </summary>
    public class ColoredString : IEnumerable<ColoredChar>
    {
        /// <summary>
        /// The default color that should be applied to all colored strings 
        /// when no color information is available.
        /// </summary>
        public static readonly Color DEFAULT_COLOR = Color.White;

        private readonly List<ColoredChar> characters;

        /// <summary>
        /// How many characters the string has.
        /// </summary>
        public int Length => characters.Count;

        public ColoredString() => characters = new List<ColoredChar>();
        public ColoredString(List<ColoredChar> characters) => this.characters = characters;

        /// <summary>
        /// Gets the character at the index provided. This is not bounds 
        /// checked.
        /// </summary>
        /// <param name="index">The index to use.</param>
        /// <returns>The colored character for the string.</returns>
        /// <throws>An out of bounds exception if out of range.</throws>
        public ColoredChar this[int index] => characters[index];

        public IEnumerator<ColoredChar> GetEnumerator() => characters.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }
    }
}
