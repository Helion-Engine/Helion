using System.Collections.Generic;
using System.Text;
using static Helion.Util.Assertion.Assert;

namespace Helion.Util.Parser
{
    /// <summary>
    /// Supports tokenizing a stream of characters into a list of tokens.
    /// </summary>
    public class Tokenizer
    {
        private readonly List<Token> m_tokens = new List<Token>();
        private readonly StringBuilder m_identifierBuilder = new StringBuilder();
        private readonly string m_text;
        private int m_lineNumber = 1;
        private int m_lineCharOffset;
        private int m_textIndex;

        private bool BuildingIdentifier => m_identifierBuilder.Length > 0;

        private Tokenizer(string text)
        {
            m_text = text;
        }
        
        /// <summary>
        /// Reads text into a series of tokens.
        /// </summary>
        /// <param name="text">The text to read.</param>
        /// <returns>A list of the tokens.</returns>
        /// <exception cref="ParserException">If there are malformed components
        /// like a string that doesn't have an ending before a new line, or a
        /// floating point number with badly placed periods.
        /// </exception>
        public static List<Token> Read(string text)
        {
            Tokenizer tokenizer = new Tokenizer(text);
            tokenizer.Tokenize();
            return tokenizer.m_tokens;
        }

        private static bool IsSpace(char c) => c == ' ' || c == '\t' || c == '\r';

        private static bool IsNumber(char c) => c >= '0' && c <= '9';
        
        private static bool IsPrintableCharacter(char c) => c >= 32 && c <= 126;

        private static bool IsEscapableStringChar(char c) => c == '"' || c == '\\';

        private static bool IsIdentifier(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '_';
        }

        private static bool IsSymbol(char c)
        {
            return (c >= 33 && c <= 47) || (c >= 58 && c <= 64) || (c >= 91 && c <= 96) || (c >= 123 && c <= 126);
        }
        
        private void ResetLineInfoTrackers()
        {
            // The next iteration will increment it to zero, which is what
            // we want the line character offset to be.
            m_lineCharOffset = -1;
            m_lineNumber++;
        }

        private void ReadEscapeCharacterOrThrow(StringBuilder innerStringBuilder)
        {
            if (m_textIndex + 1 >= m_text.Length)
                throw new ParserException(m_lineNumber, m_lineCharOffset, m_textIndex, "Expected character after escaping in a string");

            char nextChar = m_text[m_textIndex + 1];
            if (!IsEscapableStringChar(nextChar))
                throw new ParserException(m_lineNumber, m_lineCharOffset + 1, m_textIndex + 1, "Expecting an escaped quote to follow a backslash in a string");

            innerStringBuilder.Append(nextChar);
            m_textIndex++;
            m_lineCharOffset++;
        }

        private void CompleteIdentifierIfAvailable()
        {
            if (!BuildingIdentifier)
                return;

            string text = m_identifierBuilder.ToString();
            int lineCharOffset = m_lineCharOffset - text.Length;
            int charOffset = m_textIndex - text.Length;
            
            Token token = new Token(m_lineNumber, lineCharOffset, charOffset, text, TokenType.String);
            m_tokens.Add(token);
            
            m_identifierBuilder.Clear();
        }

        private void AddCompletedQuotedString(StringBuilder builder, int lineCharOffset, int textIndex)
        {
            string innerString = builder.ToString();
            Token token = new Token(m_lineNumber, lineCharOffset, textIndex, innerString, TokenType.QuotedString);
            m_tokens.Add(token);
        }

        private void ConsumeQuotedString()
        {
            int startingLineCharOffset = m_lineCharOffset;
            int startingTextIndex = m_textIndex;
            
            // We're on the opening quote, so move ahead to the starting character.
            m_textIndex++;
            m_lineCharOffset++;
            
            StringBuilder innerBuilder = new StringBuilder();
            for (; m_textIndex < m_text.Length; m_textIndex++, m_lineCharOffset++)
            {
                char c = m_text[m_textIndex];
                
                if (c == '"')
                {
                    AddCompletedQuotedString(innerBuilder, startingLineCharOffset, startingTextIndex);
                    return;
                }
                
                if (c == '\\')
                {
                    ReadEscapeCharacterOrThrow(innerBuilder);
                    continue;
                }
                
                if (IsPrintableCharacter(c))
                    innerBuilder.Append(c);
                else if (c == '\n')
                {
                    const string endingErrorMessage = "Ended line before finding terminating string quotation mark";
                    throw new ParserException(m_lineNumber, startingLineCharOffset, startingTextIndex, endingErrorMessage);
                }
            }
            
            // This is for some exotic case where we run into EOF when trying
            // to complete the quoted string.
            const string errorMessage = "String missing ending quote, found end of text instead";
            throw new ParserException(m_lineNumber, startingLineCharOffset, startingTextIndex, errorMessage);
        }

        private void ConsumeNumber()
        {
            bool isFloat = false;
            int startCharOffset = m_textIndex;
            int startLineCharOffset = m_lineCharOffset;
            StringBuilder numberBuilder = new StringBuilder();

            while (m_textIndex < m_text.Length)
            {
                char c = m_text[m_textIndex];

                if (IsNumber(c))
                    numberBuilder.Append(c);
                else if (c == '.')
                {
                    if (isFloat)
                        throw new ParserException(m_lineNumber, m_lineCharOffset, m_textIndex, "Decimal number cannot have two decimals");
                    isFloat = true;
                    numberBuilder.Append(c);
                }
                else
                    break;

                m_textIndex++;
                m_lineCharOffset++;
            }

            string text = numberBuilder.ToString();
            if (text.EndsWith("."))
                throw new ParserException(m_lineNumber, m_lineCharOffset - 1, m_textIndex - 1, "Decimal number cannot end with a period");
                
            if (isFloat)
            {
                Token floatToken = new Token(m_lineNumber, startLineCharOffset, startCharOffset, text, TokenType.FloatingPoint);
                m_tokens.Add(floatToken);
            }
            else
            {
                Token intToken = new Token(m_lineNumber, startLineCharOffset, startCharOffset, text, TokenType.Integer);
                m_tokens.Add(intToken);
            }
            
            // When we return control to the iteration loop, it'll consume a
            // character and bypass it. Decrementing here makes it so this will
            // not happen.
            m_textIndex--;
            m_lineCharOffset--;

            Postcondition(SimpleParser.TryParseDouble(text, out double _), "Returning a number token but cannot parse a number out of it");
        }
        
        private void ConsumeSlashTokenOrComments()
        {
            if (m_textIndex + 1 < m_text.Length)
            {
                char nextChar = m_text[m_textIndex + 1];
                
                if (nextChar == '/')
                {
                    m_textIndex += 2;
                    m_lineCharOffset += 2;
                    ConsumeSingleLineComment();
                    return;
                }
                
                if (nextChar == '*')
                {
                    m_textIndex += 2;
                    m_lineCharOffset += 2;
                    ConsumeMultiLineComment();
                    return;
                }
            }
                
            Token token = new Token(m_lineNumber, m_lineCharOffset, m_textIndex, '/');
            m_tokens.Add(token);
        }

        private void ConsumeSingleLineComment()
        {
            for (; m_textIndex < m_text.Length; m_textIndex++)
            {
                if (m_text[m_textIndex] != '\n') 
                    continue;
                
                ResetLineInfoTrackers();
                return;
            }
        }

        private void ConsumeMultiLineComment()
        {
            // We want to start one ahead so comments like /*/ don't work.
            m_textIndex++;
            m_lineCharOffset++;
            
            // However we can skip \n by moving ahead, so track that data.
            int prevIndex = m_textIndex - 1;
            if (prevIndex < m_text.Length && m_text[prevIndex] == '\n')
                ResetLineInfoTrackers();

            // Note that the way this loop works means that if find EOF first,
            // it is considered okay. This is what zdoom appears to do, so we
            // will have to do it as well for compatibility reasons since the
            // enforcement of requiring a terminating */ will break wads.
            for (; m_textIndex < m_text.Length; m_textIndex++, m_lineCharOffset++)
            {
                char c = m_text[m_textIndex];

                if (c == '\n')
                {
                    ResetLineInfoTrackers();
                    continue;
                }

                // We don't increment here because when we return the control
                // back to the main loop, it'll do the incrementing instead.
                if (c == '/' && m_text[m_textIndex - 1] == '*')
                    return;
            }
        }

        private void Tokenize()
        {
            for (m_textIndex = 0; m_textIndex < m_text.Length; m_textIndex++, m_lineCharOffset++)
            {
                char c = m_text[m_textIndex];

                if (IsIdentifier(c))
                {
                    m_identifierBuilder.Append(c);
                }
                else if (IsSpace(c))
                {
                    if (BuildingIdentifier)
                        CompleteIdentifierIfAvailable();
                }
                else if (IsNumber(c))
                {
                    if (BuildingIdentifier)
                        m_identifierBuilder.Append(c);
                    else
                        ConsumeNumber();
                }
                else if (IsSymbol(c))
                {
                    CompleteIdentifierIfAvailable();

                    if (c == '"')
                        ConsumeQuotedString();
                    else if (c == '/')
                        ConsumeSlashTokenOrComments();
                    else
                    {
                        Token token = new Token(m_lineNumber, m_lineCharOffset, m_textIndex, c);
                        m_tokens.Add(token);
                    }
                }
                else if (c == '\n')
                {
                    CompleteIdentifierIfAvailable();
                    ResetLineInfoTrackers();
                }
            }

            // If there's any lingering uncompleted identifier we are building,
            // we should consume it.
            CompleteIdentifierIfAvailable();
        }
    }
}