using System;
using Helion.Geometry;
using Helion.Graphics.Fonts;
using Helion.Graphics.String;
using Helion.Render.Common.Enums;
using Helion.Render.OpenGL.Textures;
using Helion.Util.Container;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Hud.Text
{
    public class GLHudTextHelper
    {
        // If we don't call reset, this is our cutoff.
        private const int OverflowCount = 10000;
        
        private readonly DynamicArray<RenderableCharacter> m_characters = new();
        private readonly DynamicArray<RenderableSentence> m_sentences = new();
        
        public void Reset()
        {
            m_characters.Clear();
            m_sentences.Clear();
        }
        
        public ReadOnlySpan<RenderableCharacter> Calculate(ColoredString text, GLFontTexture fontTexture, 
            int fontSize, TextAlign textAlign, int maxWidth, int maxHeight, float scale, out Dimension drawArea)
        {
            // TODO
            
            drawArea = default;
            return ReadOnlySpan<RenderableCharacter>.Empty;
        }

        public ReadOnlySpan<RenderableCharacter> Calculate(string text, GLFontTexture fontTexture, int fontSize,
            TextAlign textAlign, int maxWidth, int maxHeight, float scale, out Dimension drawArea)
        {
            drawArea = default;

            if (scale <= 0.0f || text == "")
                return ReadOnlySpan<RenderableCharacter>.Empty;
            
            Precondition(m_characters.Length < OverflowCount, "Not clearing the GL hud text helper characters");
            Precondition(m_sentences.Length < OverflowCount, "Not clearing the GL hud text helper sentences");
            
            // The X and Y are the top left corners in the HUD coordinate system.
            int x = 0;
            int y = 0;
            int textIndex = 0;
            int height = (int)(fontSize * scale);
            Font font = fontTexture.Font;
            bool drawnAtLeastOne = false;

            // We always want at least one iteration to occur on both the X and Y
            // directions, which forces at least one sentences, and at least one
            // character per sentence to be drawn.
            while (textIndex < text.Length)
            {
                char c = text[textIndex];
                textIndex++;
                Glyph glyph = font.Get(c);
                
                // Box2I area = (glyph.Area.Float * scale).Int;
                // RenderableCharacter renderableChar = new(area, glyph.UV);

                // if x overflows AND have drawn at least one character:
                //     y += fontHeight
                //     start new sentence
                // if y overflows AND have drawn at least one character:
                //     return
                // Draw(x, y, c) 
            }

            drawArea = default; // TODO
            return new ReadOnlySpan<RenderableCharacter>(m_characters.Data, 0, m_characters.Data.Length);
        }
    }
}
