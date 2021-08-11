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

        public readonly string String;
        private readonly List<ColoredChar> m_characters;

        public int Length => m_characters.Count;
        public bool Empty => Length == 0;

        public ColoredString(List<ColoredChar> characters)
        {
            m_characters = characters;
            
            StringBuilder builder = new(Length);
            for (int i = 0; i < m_characters.Count; i++)
                builder.Append(characters[i].Char);
            String = builder.ToString();
        }

        public ColoredChar this[int index] => m_characters[index];

        public override string ToString() => String;
        public IEnumerator<ColoredChar> GetEnumerator() => m_characters.GetEnumerator();
        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    }
}
