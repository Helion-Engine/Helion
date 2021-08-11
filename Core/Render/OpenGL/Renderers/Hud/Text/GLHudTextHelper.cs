using System;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics.Fonts;
using Helion.Graphics.String;
using Helion.Render.Common;
using Helion.Render.Common.Enums;
using Helion.Render.OpenGL.Textures;
using Helion.Util.Container;
using static Helion.Util.Assertion.Assert;

namespace Helion.Render.OpenGL.Renderers.Hud.Text
{
    public class GLHudTextHelper
    {
        // If we don't call Reset(), this is our cutoff to warn the developer.
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
            return Calculate(text.String, fontTexture, fontSize, textAlign, maxWidth, maxHeight, scale, out drawArea);
        }

        public ReadOnlySpan<RenderableCharacter> Calculate(string text, GLFontTexture fontTexture, int fontSize,
            TextAlign textAlign, int maxWidth, int maxHeight, float scale, out Dimension drawArea)
        {
            drawArea = default;

            if (scale <= 0.0f || text == "")
                return ReadOnlySpan<RenderableCharacter>.Empty;
            
            Precondition(m_characters.Length < OverflowCount, "Not clearing the GL hud text helper characters");
            Precondition(m_sentences.Length < OverflowCount, "Not clearing the GL hud text helper sentences");

            Reset();
            
            CalculateCharacters(text, fontSize, scale, fontTexture.Font, maxWidth, maxHeight);
            CalculateSentences();
            PerformTextAlignment(textAlign);
            drawArea = CalculateDrawArea();
            
            ReadOnlySpan<RenderableCharacter> span = new(m_characters.Data, 0, m_characters.Length);
            Postcondition(span.Length == text.Length, "Lost characters when creating renderable character span");

            return span;
        }
        
        private void CalculateCharacters(ReadOnlySpan<char> text, int fontSize, float scale, Font font, int maxWidth, int maxHeight)
        {
            float totalScale = scale * ((float)fontSize / font.MaxHeight); 
            int x = 0;
            int y = 0;
            int height = (int)(font.MaxHeight * totalScale);

            for (int i = 0; i < text.Length; i++)
            {
                char c = text[i];
                Glyph glyph = font.Get(c);
                HudBox area = new((glyph.Area.Float * totalScale).Int);

                // Always draw at least one character. If we want strict bound
                // adherence in the future, then we could add a boolean that will
                // respect that. It's probably better for the end user that we
                // draw at least one character instead of drawing nothing, since
                // poorly chosen bounds making nothing appear might be frustrating
                // for users, especially given that we scaling is possible based on
                // mutable config/console input, or have an entirely new font than
                // what the caller intended it would be.
                if (HasDrawnAtLeastOne())
                {
                    if (OverflowsOnX(area.Width))
                    {
                        x = 0;
                        y += height;
                    }
                    
                    if (OverflowsOnY())
                        break;
                }

                // We always want to draw at least one character per line, unless
                // it overflows on the Y.
                Vec2I topLeft = (x, y);
                Vec2I bottomRight = topLeft + area.Dimension;
                RenderableCharacter renderableChar = new(c, (topLeft, bottomRight), glyph.UV);
                m_characters.Add(renderableChar);
                
                x += area.Width;
            }

            bool HasDrawnAtLeastOne() => m_characters.Length != 0;
            bool OverflowsOnX(int width) => x + width > maxWidth;
            bool OverflowsOnY() => y + height > maxHeight;
        }

        private void CalculateSentences()
        {
            if (m_characters.Length == 0)
                return;

            // We track when the top of the character moves, as that indicates a
            // new line has been found. This is what `sentenceY` tracks.
            int sentenceY = -1;
            int startIndex = 0;
            int count = 0;
            Vec2I topLeft = default;
            Vec2I bottomRight = default;

            for (int i = 0; i < m_characters.Length; i++)
            {
                RenderableCharacter c = m_characters[i];

                // If we find a character that has a different Y height, then
                // we've reached the line under this.
                if (sentenceY != c.Area.Top)
                {
                    AddSentenceIfPossible();
                    
                    // This is the set up for the new sentence. We assume that
                    // we're "adding" the character by tracking it as part of
                    // the sentence.
                    sentenceY = c.Area.Top;
                    startIndex = i;
                    count = 0; // Not set to 1 because the loop increments this.
                    topLeft = c.Area.TopLeft;
                    bottomRight = c.Area.BottomRight;
                }
                
                count++;
            }

            // Add any trailing sentences that we did not handle.
            AddSentenceIfPossible();
            
            void AddSentenceIfPossible()
            {
                if (count == 0)
                    return;
                
                RenderableSentence sentence = new(startIndex, count, (topLeft, bottomRight));
                m_sentences.Add(sentence);
            }
        }

        private void PerformTextAlignment(TextAlign textAlign)
        {
            if (textAlign == TextAlign.Left)
                return;
            
            int maxWidth = CalculateMaxSentenceWidth();
            
            for (int sentenceIndex = 0; sentenceIndex < m_sentences.Length; sentenceIndex++)
            {
                RenderableSentence sentence = m_sentences[sentenceIndex];
                int padding = maxWidth - sentence.Bounds.Width;
                if (padding <= 0)
                    continue;
                
                // We're either aligning right, or center. If it's right, we
                // will pad the entire way. If it's centering, then only half.
                if (textAlign == TextAlign.Center)
                    padding /= 2;
                
                for (int i = sentence.StartIndex; i < sentence.StartIndex + sentence.Count; i++)
                {
                    Precondition(i < m_characters.Length, "Renderable sentence index is out of bounds");
                    
                    RenderableCharacter oldChar = m_characters[i];
                    RenderableCharacter newChar = new(oldChar.Character, oldChar.Area + (0, padding), oldChar.UV);
                    m_characters[i] = newChar;
                }
            }
        }

        private int CalculateMaxSentenceWidth()
        {
            int width = 0;
            for (int i = 0; i < m_sentences.Length; i++)
                width = Math.Max(width, m_sentences[i].Bounds.Width);
            return width;
        }

        private Dimension CalculateDrawArea()
        {
            int w = 0;
            int h = 0;
            for (int i = 0; i < m_sentences.Length; i++)
            {
                HudBox bounds = m_sentences[i].Bounds;
                w = Math.Max(w, bounds.Right);
                h += bounds.Height;
            }

            return (w, h);
        }
    }
}
