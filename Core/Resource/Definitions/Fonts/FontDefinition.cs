using System.Collections.Generic;
using System.Linq;
using Helion.Util;
using Helion.Util.Extensions;
using NLog;
using static Helion.Util.Assertion.Assert;

namespace Helion.Resource.Definitions.Fonts
{
    public class FontDefinition
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

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

        public bool IsValid()
        {
            if (CharDefinitions.Empty())
            {
                Log.Error("Font {0} has no character definitions, font cannot be used", Name);
                return false;
            }

            if (!CharDefinitions.ContainsKey(' '))
            {
                if (SpaceWidth == null)
                {
                    Log.Error("Font {0} did not define a space character width and has no space character definition", Name);
                    return false;
                }

                if (SpaceWidth <= 0)
                {
                    Log.Error("Font {0} has no character definitions, font cannot be used", Name);
                    return false;
                }
            }

            int charsWithDefault = CharDefinitions.Count(charDef => charDef.Value.Default);
            switch (charsWithDefault)
            {
            case 0:
                Log.Error("Font {0} has no default character definition");
                return false;
            case 1:
                return true;
            default:
                Log.Error("Font {0} has multiple default character definitions, only support one default character");
                return false;
            }
        }
    }
}