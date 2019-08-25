using System.Collections.Generic;
using Helion.Util;
using static Helion.Util.Assertion.Assert;

namespace Helion.Resources.Definitions.Fonts.Definition
{
    public class FontDefinition
    {
        public readonly CIString Name;
        public readonly Dictionary<char, CharDefinition> CharDefinitions = new Dictionary<char, CharDefinition>();
        public bool Grayscale;
        public int? SpaceWidth;
        public FontAlignment Alignment = FontAlignment.Bottom;

        public FontDefinition(CIString name)
        {
            Precondition(!name.Empty, "Should not have an empty font definition name");
            
            Name = name;
        }
    }
}