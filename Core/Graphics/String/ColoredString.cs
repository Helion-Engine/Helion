using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using MoreLinq;

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
        public static readonly Color DefaultColor = Color.White;

        private readonly List<ColoredChar> m_characters;

        /// <summary>
        /// How many characters the string has.
        /// </summary>
        public int Length => m_characters.Count;

        public ColoredString(List<ColoredChar> characters)
        {
            m_characters = characters;
        }

        /// <summary>
        /// Gets the character at the index provided. This is not bounds 
        /// checked.
        /// </summary>
        /// <param name="index">The index to use.</param>
        /// <returns>The colored character for the string.</returns>
        /// <throws>An out of bounds exception if out of range.</throws>
        public ColoredChar this[int index] => m_characters[index];

        public override string ToString()
        {
            StringBuilder builder = new StringBuilder(Length);
            m_characters.Select(coloredChar => coloredChar.Character).ForEach(c => builder.Append(c));
            return builder.ToString();
        }

        public IEnumerator<ColoredChar> GetEnumerator() => m_characters.GetEnumerator();

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
