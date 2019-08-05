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
        
        /// <summary>
        /// A list of the tokens to be processed.
        /// </summary>
        protected List<Token> Tokens = new List<Token>();
        
        /// <summary>
        /// Performs a full parsing on the text. On success, internal data
        /// structures will be populated. Returns success or failure status.
        /// </summary>
        /// <param name="text">The text to parse.</param>
        /// <returns>True on success, false if any errors occurred. Failure
        /// occurs when the grammar is not correct, or tokenizing fails from
        /// malformed data (ex: string missing an ending quotation mark).
        /// </returns>
        public bool Parse(string text)
        {
            try
            {
                Tokens = Tokenizer.Read(text);
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
        protected bool Done => CurrentTokenIndex >= Tokens.Count;

        /// <summary>
        /// Peeks to the next token (if it exists) to check if it is the same
        /// type as the one provided.
        /// </summary>
        /// <param name="type">The type to peek for.</param>
        /// <returns>True if the next token matches the type provided, false if
        /// it does not match or we are out of tokens to read.</returns>
        protected bool Peek(TokenType type)
        {
            if (CurrentTokenIndex < Tokens.Count)
                return Tokens[CurrentTokenIndex].Type == type;
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
            if (CurrentTokenIndex >= Tokens.Count) 
                return false;
            
            string text = Tokens[CurrentTokenIndex].Text;
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
            if (CurrentTokenIndex >= Tokens.Count) 
                return false;

            TokenType type = Tokens[CurrentTokenIndex].Type;
            if (type != TokenType.String || type != TokenType.QuotedString)
                return false;
            
            return string.Equals(str, Tokens[CurrentTokenIndex].Text);
        }
        
        /// <summary>
        /// Checks if the next token is a floating point number (or an integer
        /// as well).
        /// </summary>
        /// <returns>True if it is a float or integer, false if not or if we
        /// ran out of tokens.</returns>
        protected bool PeekFloat()
        {
            if (CurrentTokenIndex >= Tokens.Count) 
                return false;
            
            TokenType type = Tokens[CurrentTokenIndex].Type;
            return type == TokenType.Integer || type == TokenType.FloatingPoint;
        }

        /// <summary>
        /// Checks to see if the next token is a whole number. This will not
        /// read negatives, only unsigned numbers. If you want to read negative
        /// numbers, use <see cref="PeekSignedInteger"/>.
        /// </summary>
        /// <returns>True if it is, false if not or if we ran out of tokens.
        /// </returns>
        protected bool PeekInteger() => Peek(TokenType.Integer);
        
        /// <summary>
        /// Checks to see if either an integer or a negative integer is next.
        /// If true, this means you can consume a signed integer.
        /// </summary>
        /// <returns>True if there is a next token that is an integer, or if
        /// there are two tokens and they are a minus sign followed by an
        /// integer; false otherwise.</returns>
        protected bool PeekSignedInteger()
        {
            if (Done)
                return false;

            if (PeekInteger())
                return true;

            if (Peek(TokenType.Minus) && CurrentTokenIndex + 1 < Tokens.Count)
                return Tokens[CurrentTokenIndex + 1].Type == TokenType.Integer;

            return false;
        }
        
        /// <summary>
        /// Checks to see if either a float or a negative float is next. If
        /// true, this means you can consume a signed float.
        /// </summary>
        /// <returns>True if there is a next token that is an float, or if
        /// there are two tokens and they are a minus sign followed by a
        /// float; false otherwise.</returns>
        protected bool PeekSignedFloat()
        {
            if (Done)
                return false;

            if (PeekFloat())
                return true;

            if (Peek(TokenType.Minus) && CurrentTokenIndex + 1 < Tokens.Count)
                return Tokens[CurrentTokenIndex + 1].Type == TokenType.FloatingPoint;

            return false;
        }

        /// <summary>
        /// Checks to see if the next token is a string or not.
        /// </summary>
        /// <returns>True if so, false if we ran out of tokens or it is not a
        /// string.</returns>
        protected bool PeekString()
        {
            if (CurrentTokenIndex >= Tokens.Count) 
                return false;
            
            TokenType type = Tokens[CurrentTokenIndex].Type;
            return type == TokenType.String || type == TokenType.QuotedString;
        }
        
        /// <summary>
        /// Consumes the token type provided, or throws.
        /// </summary>
        /// <param name="type">The type to consume.</param>
        /// <exception cref="ParserException">If the type does not match or we
        /// ran out of tokens.</exception>
        protected void Consume(TokenType type)
        {
            if (Tokens[CurrentTokenIndex].Type == type)
            {
                CurrentTokenIndex++;
                return;
            }
            
            if (Done)
                ThrowOvershootException($"Expecting token type {type}, but ran out of tokens");

            Token token = Tokens[CurrentTokenIndex];
            throw new ParserException(token, $"Expecting to find {type}, got {Tokens[CurrentTokenIndex].Type} instead");
        }

        /// <summary>
        /// Consumes a character or throws.
        /// </summary>
        /// <remarks>
        /// Does not work on consuming characters in a string. This only works
        /// if the character was completely standalone, or is a symbol that
        /// forms its own token. This also does not return anything because you
        /// know exactly what character you are getting if this doesn't throw.
        /// This is also a convenience function since writing Consume('{') is
        /// easier than something like Consume(TokenType.LeftBrace).
        /// </remarks>
        /// <param name="c">The character to consume.</param>
        /// <exception cref="ParserException">If there is no match or we ran
        /// out of tokens.</exception>
        protected void Consume(char c)
        {
            if (Tokens[CurrentTokenIndex].Text.Length == 1 && Tokens[CurrentTokenIndex].Text[0] == c)
            {
                CurrentTokenIndex++;
                return;
            }
            
            if (Done)
                ThrowOvershootException($"Expecting character '{c}', but ran out of tokens");

            Token token = Tokens[CurrentTokenIndex];
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
            if (PeekInteger())
            {
                if (int.TryParse(Tokens[CurrentTokenIndex].Text, out int number))
                {
                    CurrentTokenIndex++;
                    return number;
                }
            }

            if (Done)
                ThrowOvershootException("Expecting a number, but ran out of tokens");
            
            Token token = Tokens[CurrentTokenIndex];
            throw new ParserException(token, $"Expected a number, got a '{token.Type}' instead (which was \"{token.Text}\")");
        }
        
        /// <summary>
        /// Consumes a signed integer and parses it. Throws if it cannot.
        /// </summary>
        /// <returns>The integer (if it doesn't throw).</returns>
        /// <exception cref="ParserException">If there is no match or we ran
        /// out of tokens or there was a parsing error with the integer.
        /// </exception>
        protected int ConsumeSignedInteger()
        {
            if (PeekSignedInteger())
            {
                if (Tokens[CurrentTokenIndex].Type == TokenType.Integer)
                    return ConsumeInteger();

                Consume(TokenType.Minus);
                CurrentTokenIndex++;
                return -ConsumeInteger();
            }

            if (Done)
                ThrowOvershootException("Expecting a number, but ran out of tokens");
            
            Token token = Tokens[CurrentTokenIndex];
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
            if (PeekFloat() && double.TryParse(Tokens[CurrentTokenIndex++].Text, out double number))
                return number;

            if (Done)
                ThrowOvershootException("Expecting a decimal number, but ran out of tokens");
            
            Token token = Tokens[CurrentTokenIndex];
            throw new ParserException(token, $"Expected a decimal number, got a '{token.Type}' instead (which was \"{token.Text}\")");
        }
        
        /// <summary>
        /// Consumes a signed float and parses it. Throws if it cannot.
        /// </summary>
        /// <returns>The float (if it doesn't throw).</returns>
        /// <exception cref="ParserException">If there is no match or we ran
        /// out of tokens or there was a parsing error with the float.
        /// </exception>
        protected double ConsumeSignedFloat()
        {
            if (PeekSignedFloat())
            {
                if (Tokens[CurrentTokenIndex].Type == TokenType.FloatingPoint)
                    return ConsumeFloat();

                Consume(TokenType.Minus);
                CurrentTokenIndex++;
                return -ConsumeFloat();
            }

            if (Done)
                ThrowOvershootException("Expecting a decimal number, but ran out of tokens");
            
            Token token = Tokens[CurrentTokenIndex];
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
                return Tokens[CurrentTokenIndex++].Text;

            if (Done)
                ThrowOvershootException("Expecting text, but ran out of tokens");
            
            Token token = Tokens[CurrentTokenIndex];
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
            if (string.Equals(str, Tokens[CurrentTokenIndex].Text, StringComparison.OrdinalIgnoreCase))
            {
                CurrentTokenIndex++;
                return;
            }
            
            if (Done)
                ThrowOvershootException($"Expecting text '{str}', but ran out of tokens");

            Token token = Tokens[CurrentTokenIndex];
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
            if (Tokens.Empty())
                throw new ParserException(0, 0, 0, message);
            throw new ParserException(Tokens.Last(), message);
        }
    }
}