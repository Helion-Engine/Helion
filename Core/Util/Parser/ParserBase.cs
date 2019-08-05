using System;
using System.Collections.Generic;
using System.Linq;
using Helion.Util.Assertion;
using Helion.Util.Extensions;

namespace Helion.Util.Parser
{
    /// <summary>
    /// The base class for a parser. All implementations should extend this to
    /// get access to parsing methods to aid in token consumption.
    /// </summary>
    /// <remarks>
    /// It is up to the child implementation to handle all of the tokens and
    /// convert them into the data structures they want. This class provides
    /// only the tools to do so.
    /// </remarks>
    public abstract class ParserBase
    {
        /// <summary>
        /// The current token index we're at. This can be retrieved or set to
        /// change the current token bring processed. Note that this should not
        /// be set ouf of range, or else bad things will happen.
        /// </summary>
        protected int CurrentTokenIndex;
        
        private List<Token> m_tokens = new List<Token>();
        
        /// <summary>
        /// Performs a full parsing on the text. On success, internal data
        /// structures will be populated. Returns success or failure status.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <returns>True on success, false if any errors occurred.</returns>
        public bool Parse(string text)
        {
            try
            {
                m_tokens = Tokenizer.Read(text);
                PerformParsing();
                return true;
            }
            catch (ParserException e)
            {
                // TODO: Log the parsing information.
                return false;
            }
            catch (AssertionException)
            {
                // We want this to leak through if it ever happens.
                throw;
            }
            catch (Exception e)
            {
                // TODO: Log the information.
                return false;
            }
        }

        /// <summary>
        /// Checks if all the tokens have been all consumed.
        /// </summary>
        protected bool Done => CurrentTokenIndex >= m_tokens.Count;

        /// <summary>
        /// Peeks to the next token (if it exists) to check if it is the same
        /// type as the one provided.
        /// </summary>
        /// <param name="type">The type to peek for.</param>
        /// <returns>True if the next token matches the type provided, false if
        /// it does not match or we are out of tokens to read.</returns>
        protected bool Peek(TokenType type)
        {
            if (CurrentTokenIndex < m_tokens.Count)
                return m_tokens[CurrentTokenIndex].Type == type;
            return false;
        }
        
        /// <summary>
        /// Peeks ahead to see if the character provided is next.
        /// </summary>
        /// <remarks>
        /// This can be used in place of checking certain symbols. For example
        /// calling this with ';' would be identical to calling it the other
        /// peek function with TokenType.Semicolon, except this does two checks
        /// instead of one. This will likely mean nothing performance-wise
        /// though because of how fast these functions are.
        /// </remarks>
        /// <param name="c">The character to check.</param>
        /// <returns>True if there is a character left that matches, false if
        /// the next one is not a character or we ran out of tokens.</returns>
        protected bool Peek(char c)
        {
            if (CurrentTokenIndex >= m_tokens.Count) 
                return false;
            
            string text = m_tokens[CurrentTokenIndex].Text;
            return text.Length == 1 && text[0] == c;
        }
        
        /// <summary>
        /// Checks to see if there's a string up next that matches the provided
        /// string argument. Comparison is case-insensitive.
        /// </summary>
        /// <remarks>
        /// Do not include quotation marks in the string, those are trimmed.
        /// </remarks>
        /// <param name="str">The string to check again.</param>
        /// <returns>True if the next token matches this, false if not or if we
        /// ran out of tokens.</returns>
        protected bool Peek(string str)
        {
            if (CurrentTokenIndex >= m_tokens.Count) 
                return false;

            TokenType type = m_tokens[CurrentTokenIndex].Type;
            if (type != TokenType.String || type != TokenType.QuotedString)
                return false;
            
            return string.Equals(str, m_tokens[CurrentTokenIndex].Text);
        }
        
        /// <summary>
        /// Checks if the next token is a floating point number (or an integer
        /// as well).
        /// </summary>
        /// <returns>True if it is a float or integer, false if not or if we
        /// ran out of tokens.</returns>
        protected bool PeekFloat()
        {
            if (CurrentTokenIndex >= m_tokens.Count) 
                return false;
            
            TokenType type = m_tokens[CurrentTokenIndex].Type;
            return type == TokenType.Integer || type == TokenType.FloatingPoint;
        }

        /// <summary>
        /// Checks to see if the next token is a whole number.
        /// </summary>
        /// <returns>True if it is, false if not or if we ran out of tokens.
        /// </returns>
        protected bool PeekInteger() => Peek(TokenType.Integer);

        /// <summary>
        /// Checks to see if the next token is a string or not.
        /// </summary>
        /// <returns>True if so, false if we ran out of tokens or it is not a
        /// string.</returns>
        protected bool PeekString()
        {
            if (CurrentTokenIndex >= m_tokens.Count) 
                return false;
            
            TokenType type = m_tokens[CurrentTokenIndex].Type;
            return type == TokenType.String || type == TokenType.QuotedString;
        }

        /// <summary>
        /// Consumes a character or throws.
        /// </summary>
        /// <remarks>
        /// Does not work on consuming characters in a string. This only works
        /// if the character was completely standalone, or is a symbol that
        /// forms its own token. This also does not return anything because you
        /// know exactly what character you are getting if this doesn't throw.
        /// </remarks>
        /// <param name="c">The character to consume.</param>
        /// <exception cref="ParserException">If there is no match or we ran
        /// out of tokens.</exception>
        protected void Consume(char c)
        {
            if (m_tokens[CurrentTokenIndex].Text.Length == 1 && m_tokens[CurrentTokenIndex].Text[0] == c)
            {
                CurrentTokenIndex++;
                return;
            }
            
            if (Done)
                ThrowOvershootException($"Expecting character '{c}', but ran out of tokens");

            Token token = m_tokens[CurrentTokenIndex];
            throw new ParserException(token, $"Expecting to find '{c}', got {token.Text} instead");
        }
        
        /// <summary>
        /// Consumes an integer and parses it. Throws if it cannot.
        /// </summary>
        /// <returns>The integer (if it doesn't throw).</returns>
        /// <exception cref="ParserException">If there is no match or we ran
        /// out of tokens or there was a parsing error with the integer.
        /// </exception>
        protected int ConsumeInteger()
        {
            if (PeekInteger() && int.TryParse(m_tokens[CurrentTokenIndex++].Text, out int number))
                return number;

            if (Done)
                ThrowOvershootException("Expecting a number, but ran out of tokens");
            
            Token token = m_tokens[CurrentTokenIndex];
            throw new ParserException(token, $"Expected a number, got a '{token.Type}' instead (which was \"{token.Text}\")");
        }
        
        /// <summary>
        /// Consumes an float (or integer) and parses it. Throws if it cannot.
        /// </summary>
        /// <returns>The floating point value (if it doesn't throw).</returns>
        /// <exception cref="ParserException">If there is no match or we ran
        /// out of tokens or there was a parsing error with the float.
        /// </exception>
        protected double ConsumeFloat()
        {
            if (PeekFloat() && double.TryParse(m_tokens[CurrentTokenIndex++].Text, out double number))
                return number;

            if (Done)
                ThrowOvershootException("Expecting a decimal number, but ran out of tokens");
            
            Token token = m_tokens[CurrentTokenIndex];
            throw new ParserException(token, $"Expected a decimal number, got a '{token.Type}' instead (which was \"{token.Text}\")");
        }

        /// <summary>
        /// Consumes a string. Throws if it cannot.
        /// </summary>
        /// <returns>The string value (if it doesn't throw).</returns>
        /// <exception cref="ParserException">If there is no match or we ran
        /// out of tokens.
        /// </exception>
        protected string ConsumeText()
        {
            if (PeekString())
                return m_tokens[CurrentTokenIndex++].Text;

            if (Done)
                ThrowOvershootException("Expecting text, but ran out of tokens");
            
            Token token = m_tokens[CurrentTokenIndex];
            throw new ParserException(token, $"Expected text, got a '{token.Type}' instead");
        }

        /// <summary>
        /// Consumes text if it is a match, which is case-insensitive.
        /// </summary>
        /// <remarks>
        /// Does not return the string because you know what string you are
        /// getting by what you provided.
        /// </remarks>
        /// <param name="str">The string characters to consume.</param>
        /// <exception cref="ParserException">If the string does not match, or
        /// if we ran out of tokens, or if the token is not a string value.
        /// </exception>
        protected void ConsumeText(string str)
        {
            if (string.Equals(str, m_tokens[CurrentTokenIndex].Text, StringComparison.OrdinalIgnoreCase))
            {
                CurrentTokenIndex++;
                return;
            }
            
            if (Done)
                ThrowOvershootException($"Expecting text '{str}', but ran out of tokens");

            Token token = m_tokens[CurrentTokenIndex];
            throw new ParserException(token, $"Expecting to find \"{str}\", got \"{token.Text}\" instead");
        }

        /// <summary>
        /// The start method of parsing. The inheriting child should implement
        /// this and carry out all of the parsing by using the methods provided
        /// in this class.
        /// </summary>
        protected abstract void PerformParsing();
        
        private void ThrowOvershootException(string message)
        {
            if (m_tokens.Empty())
                throw new ParserException(0, 0, 0, message);
            throw new ParserException(m_tokens.Last(), message);
        }
    }
}