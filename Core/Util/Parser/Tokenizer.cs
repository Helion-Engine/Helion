using System.Collections.Generic;
using System.Text;

namespace Helion.Util.Parser
{
    /// <summary>
    /// Supports tokenizing a stream of characters into a list of tokens.
    /// </summary>
    public class Tokenizer
    {
        private readonly List<Token> m_tokens = new List<Token>();
        private readonly StringBuilder m_identifierBuilder = new StringBuilder();
        private int lineNumber = 1;
        private int lineCharOffset;
        private int textIndex;

        private bool BuildingIdentifier => m_identifierBuilder.Length > 0;

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
            Tokenizer tokenizer = new Tokenizer();
            tokenizer.Tokenize(text);
            return tokenizer.m_tokens;
        }

        private static bool IsSpace(char c) => c == ' ' || c == '\t';

        private static bool IsNumber(char c) => c >= '0' && c <= '9';
        
        private static bool IsPrintableCharacter(char c) => c >= 32 && c <= 126;

        private static bool IsIdentifier(char c)
        {
            return (c >= 'A' && c <= 'Z') || (c >= 'a' && c <= 'z') || c == '_';
        }

        private static bool IsSymbol(char c)
        {
            return (c >= 33 && c <= 47) || (c >= 58 && c <= 64) || (c >= 91 && c <= 96) || (c >= 123 && c <= 126);
        }
        
        private void CompleteIdentifierIfAvailable()
        {
            if (!BuildingIdentifier)
                return;

            string text = m_identifierBuilder.ToString();
            Token token = new Token(lineNumber, lineCharOffset, textIndex, text, TokenType.String);
            m_tokens.Add(token);
            
            m_identifierBuilder.Clear();
        }
        
        private void ConsumeQuotedString(string text)
        {
            // We're on the opening quote, so move ahead to the starting character.
            textIndex++;
            
            int startingLineCharOffset = lineCharOffset;
            int startingTextIndex = textIndex;
            StringBuilder innerStringBuilder = new StringBuilder();
            
            for (; textIndex < text.Length; textIndex++, lineCharOffset++)
            {
                char c = text[textIndex];
                
                if (IsPrintableCharacter(c))
                    innerStringBuilder.Append(c);
                else if (c == '\n')
                    throw new ParserException(lineNumber, lineCharOffset, textIndex, "String missing ending quote");
                else if (c == '"')
                {
                    string innerString = innerStringBuilder.ToString();
                    Token token = new Token(lineNumber, lineCharOffset, textIndex, innerString, TokenType.QuotedString);
                    m_tokens.Add(token);
                    return;
                }
            }
            
            // This is for some exotic case where we run into EOF when trying
            // to complete the quoted string.
            const string errorMessage = "String missing ending quote, found end of text instead";
            throw new ParserException(lineNumber, startingLineCharOffset, startingTextIndex, errorMessage);
        }

        private void ConsumeNumber()
        {
            // TODO
        }
        
        private void ConsumeSlashTokenOrComments(string text)
        {
            if (textIndex + 1 < text.Length)
            {
                char nextChar = text[textIndex + 1];
                
                if (nextChar == '/')
                {
                    textIndex += 2;
                    lineCharOffset += 2;
                    ConsumeSingleLineComment(text);
                    return;
                }
                
                if (nextChar == '*')
                {
                    textIndex += 2;
                    lineCharOffset += 2;
                    ConsumeMultiLineComment(text);
                    return;
                }
            }
                
            Token token = new Token(lineNumber, lineCharOffset, textIndex, '/');
            m_tokens.Add(token);
        }

        private void ConsumeSingleLineComment(string text)
        {
            for (; textIndex < text.Length; textIndex++)
            {
                if (text[textIndex] != '\n') 
                    continue;
                
                // The next iteration will increment it to zero, which is what
                // we want the line character offset to be.
                lineCharOffset = -1;
                lineNumber++;
                return;
            }
        }

        private void ConsumeMultiLineComment(string text)
        {
            // We want to start one ahead so comments like /*/ don't work.
            textIndex++;
            
            // Note that the way this loop works means that if find EOF first,
            // it is considered okay. This is what zdoom appears to do, so we
            // will have to do it as well for compatibility reasons since the
            // enforcement of requiring a terminating */ will break wads.
            for (; textIndex < text.Length; textIndex++, lineCharOffset++)
            {
                char c = text[textIndex];

                if (c == '\n')
                {
                    // The next iteration will increment it to zero, which is what
                    // we want the line character offset to be.
                    lineCharOffset = -1;
                    lineNumber++;
                    continue;
                }
                
                if (c == '/' && text[textIndex - 1] == '*')
                {
                    textIndex++;
                    lineCharOffset++;
                    return;
                }
            }
        }
        
        private void ConsumeMinusTokenOrNegativeNumber(string text)
        {
            // TODO: Check if it's a number that's negated.
            
            Token token = new Token(lineNumber, lineCharOffset, textIndex, '-');
            m_tokens.Add(token);
        }

        private void Tokenize(string text)
        {
            for (textIndex = 0; textIndex < text.Length; textIndex++, lineCharOffset++)
            {
                char c = text[textIndex];

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
                        ConsumeQuotedString(text);
                    else if (c == '/')
                        ConsumeSlashTokenOrComments(text);
                    else if (c == '-')
                        ConsumeMinusTokenOrNegativeNumber(text);
                    else
                    {
                        Token token = new Token(lineNumber, lineCharOffset, textIndex, c);
                        m_tokens.Add(token);
                    }
                }
                else if (c == '\n')
                {
                    CompleteIdentifierIfAvailable();

                    // The next iteration will increment it to zero, which is
                    // what we want the line character offset to be.
                    lineCharOffset = -1;
                    lineNumber++;
                }
            }

            // If there's any lingering uncompleted identifier we are building,
            // we should consume it.
            CompleteIdentifierIfAvailable();
        }
    }
}