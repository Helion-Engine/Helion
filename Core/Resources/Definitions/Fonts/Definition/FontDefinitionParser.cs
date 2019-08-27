using System.Collections.Generic;
using Helion.Util.Extensions;
using Helion.Util.Parser;

namespace Helion.Resources.Definitions.Fonts.Definition
{
    /// <summary>
    /// Parses a new font definition file (not ZDooms one).
    /// </summary>
    /// <remarks>
    /// Has the following BNF format:
    /// <code>
    /// SPACING: "space" "=" NUMBER ";"
    /// GRAYSCALE: "grayscale" "=" BOOL ";"
    /// POSITION = "top" | "center" | "bottom"
    /// ALIGN = "align" "=" POSITION ";"
    /// ALIGNCHAR = "align" POSITION 
    /// CHARDEF: STRING STRING ["default"] [ALIGNCHAR] ";"
    /// CHARACTERS: "characters" "{" CHARDEF "}"
    /// FONTDEF: ALIGN | SPACING | GRAYSCALE | CHARACTERS
    /// FONT: "font" IDENTIFIER "{" FONTDEF* "}"
    /// </code>
    /// </remarks>
    public class FontDefinitionParser : ParserBase
    {
        public readonly IList<FontDefinition> Definitions = new List<FontDefinition>();
        private FontDefinition CurrentDefinition = new FontDefinition("none");

        protected override void PerformParsing()
        {
            Definitions.Clear();
            
            while (!Done)
                ParseFontDefinitions();
        }

        private FontAlignment ConsumeFontAlignmentType()
        {
            string alignName = ConsumeString();
            switch (alignName.ToUpper())
            {
            case "BOTTOM":
                return FontAlignment.Bottom;
            case "CENTER":
                return FontAlignment.Center;
            case "TOP":
                return FontAlignment.Top;
            default:
                ThrowException($"Expected alignment to be bottom/center/top, got {alignName} instead");
                return FontAlignment.Bottom;
            }
        }

        private void ConsumeAlignDefinition()
        {
            Consume('=');
            CurrentDefinition.Alignment = ConsumeFontAlignmentType();
            Consume(';');
        }

        private char ConsumeFontCharacter()
        {
            string character = ConsumeString();
            if (character.Length != 1)
                ThrowException($"Expected printable font character, got \"{character}\" instead");
            
            char c = character[0];
            if (c < 32 || c >= 127)
                ThrowException($"Unexpected font character index \"{(int)c}\", required to be in the range 32 - 126 inclusive");

            return c;
        }

        private void ConsumeCharacterDefinition()
        {
            char c = ConsumeFontCharacter();
            string imageName = ConsumeString();
            
            bool isDefault = ConsumeIf("DEFAULT");
            
            FontAlignment? alignment = null;
            if (ConsumeIf("ALIGN"))
                alignment = ConsumeFontAlignmentType();
            
            Consume(';');
            
            CurrentDefinition.CharDefinitions[c] = new CharDefinition(c, imageName, isDefault, alignment); 
        }

        private void ConsumeCharactersDefinition()
        {
            if (!CurrentDefinition.CharDefinitions.Empty())
                ThrowException("Multiple 'characters' definitions in font, should only be one");
            
            Consume('{');
            while (!Peek('}'))
                ConsumeCharacterDefinition();
            Consume('}');
        }

        private void ConsumeGrayscaleDefinition()
        {
            Consume('=');
            CurrentDefinition.Grayscale = ConsumeBoolean();
            Consume(';');
        }

        private void ConsumeSpaceDefinition()
        {
            Consume('=');
            CurrentDefinition.SpaceWidth = ConsumeInteger();
            Consume(';');
        }

        private void ConsumeFontDefinitionElement()
        {
            string identifier = ConsumeString();
            switch (identifier.ToUpper())
            {
            case "ALIGN":
                ConsumeAlignDefinition();
                break;
            case "CHARACTERS":
                ConsumeCharactersDefinition();
                break;
            case "GRAYSCALE":
                ConsumeGrayscaleDefinition();
                break;
            case "SPACE":
                ConsumeSpaceDefinition();
                break;
            default:
                ThrowException($"Expecting font definition (ex: 'characters', 'space', ...etc), got {identifier} instead");
                return;
            }
        }

        private void ParseFontDefinitions()
        {
            ConsumeString("font");

            string fontName = ConsumeString();
            if (fontName.Empty())
                ThrowException("Should not be getting an empty font definition name");
            
            CurrentDefinition = new FontDefinition(fontName);
            
            Consume('{');
            InvokeUntilAndConsume('}', ConsumeFontDefinitionElement);
            
            Definitions.Add(CurrentDefinition);
        }
    }
}