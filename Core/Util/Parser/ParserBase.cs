using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Helion.Resources.Archives.Entries;
using Helion.Util.Assertion;
using Helion.Util.Extensions;
using NLog;

namespace Helion.Util.Parser;

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
    private static readonly Logger Log = LogManager.GetCurrentClassLogger();

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
    /// Performs parsing a data entry. For more information on how this
    /// works, see <see cref="Parse(string)"/>.
    /// </summary>
    /// <param name="entry">The entry to read as text.</param>
    /// <returns>See <see cref="Parse(string)"/>.</returns>
    public bool Parse(Entry entry)
    {
        return Parse(entry.ReadData());
    }

    /// <summary>
    /// Performs parsing on a set of ASCII characters. For more information
    /// see <see cref="Parse(string)"/>.
    /// </summary>
    /// <param name="textData">The ASCII text data in byte form.</param>
    /// <returns>See <see cref="Parse(string)"/>.</returns>
    public bool Parse(byte[] textData)
    {
        return Parse(Encoding.ASCII.GetString(textData));
    }

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
        catch (ArgumentOutOfRangeException)
        {
            if (Tokens.Empty())
            {
                Log.Error("No tokens to parse, cannot read definition file");
                return false;
            }

            ParserException exception = new ParserException(Tokens.Last(), "Text ended too early when parsing");
            HandleParserException(exception, text);
            return false;
        }
        catch (ParserException e)
        {
            HandleParserException(e, text);
            return false;
        }
        catch (AssertionException)
        {
            // We want this to leak through if it ever happens.
            throw;
        }
        catch (Exception)
        {
            PrintUnexpectedErrorMessage();
            return false;
        }
    }

    /// <summary>
    /// Checks if all the tokens have been all consumed.
    /// </summary>
    protected bool Done => CurrentTokenIndex >= Tokens.Count;

    /// <summary>
    /// Gets the current token or returns null if there are no tokens left.
    /// </summary>
    /// <returns>The token we are currently at, null if none are left.
    /// </returns>
    protected Token? GetCurrentToken()
    {
        if (Done)
            return null;
        return Tokens[CurrentTokenIndex];
    }

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
        if (type != TokenType.String && type != TokenType.QuotedString)
            return false;

        return string.Equals(str, Tokens[CurrentTokenIndex].Text, StringComparison.OrdinalIgnoreCase);
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
        {
             TokenType type = Tokens[CurrentTokenIndex + 1].Type;
             return type == TokenType.FloatingPoint || type == TokenType.Integer;
        }

        return false;
    }

    /// <summary>
    /// Checks to see if the next token is an identifier or not. This means
    /// it is a string, and not a quoted string.
    /// </summary>
    /// <returns>True if so, false if we ran out of tokens or it is not a
    /// string.</returns>
    protected bool PeekIdentifier()
    {
        if (CurrentTokenIndex >= Tokens.Count)
            return false;
        return Tokens[CurrentTokenIndex].Type == TokenType.String;
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
    /// Peeks at the current token's text, or returns null if there are no
    /// more tokens.
    /// </summary>
    /// <returns>The text of the current token, or null if no token is
    /// available.</returns>
    protected string? PeekCurrentText()
    {
        return CurrentTokenIndex >= Tokens.Count ? null : Tokens[CurrentTokenIndex].Text;
    }

    /// <summary>
    /// Gets the next tokens text after the one we are currently pointing
    /// at.
    /// </summary>
    /// <returns>A string for the text of the next token, or null if there
    /// is no next token.</returns>
    protected string? PeekNextText()
    {
        return CurrentTokenIndex + 1 >= Tokens.Count ? null : Tokens[CurrentTokenIndex + 1].Text;
    }

    /// <summary>
    /// Consumes any token, regardless of the type.
    /// </summary>
    /// <exception cref="ParserException">If we ran out of tokens.
    /// </exception>
    protected void Consume()
    {
        if (Done)
            throw MakeException("Ran out of tokens to consume");
        CurrentTokenIndex++;
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

        Token token = Tokens[CurrentTokenIndex];
        throw new ParserException(token, $"Expecting to find '{c}', got {token.Text} instead");
    }

    /// <summary>
    /// Consumes text, which is case-insensitive. Throws if it cannot.
    /// </summary>
    /// <remarks>
    /// Does not return the string because you know what string you are
    /// getting by what you provided.
    /// </remarks>
    /// <param name="str">The string characters to consume.</param>
    /// <exception cref="ParserException">If the string does not match, or
    /// if we ran out of tokens, or if the token is not a string value.
    /// </exception>
    protected void Consume(string str)
    {
        if (string.Equals(str, Tokens[CurrentTokenIndex].Text, StringComparison.OrdinalIgnoreCase))
        {
            CurrentTokenIndex++;
            return;
        }

        Token token = Tokens[CurrentTokenIndex];
        throw new ParserException(token, $"Expecting to find \"{str}\", got \"{token.Text}\" instead");
    }

    /// <summary>
    /// Consumes a boolean and parses it. Throws if it cannot. This only
    /// works for the values 'true' and 'false' (not case sensitive).
    /// </summary>
    /// <returns>The boolean (if it doesn't throw).</returns>
    /// <exception cref="ParserException">If there is no match or we ran
    /// out of tokens or there was a parsing error with the boolean.
    /// </exception>
    protected bool ConsumeBoolean()
    {
        string str = ConsumeString();
        if (str.Equals("TRUE", StringComparison.OrdinalIgnoreCase))
            return true;
        else if (str.Equals("FALSE", StringComparison.OrdinalIgnoreCase))
            return false;
        else
            throw MakeException($"Expecting boolean value of 'true' or 'false', got {str} instead");
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
            return -ConsumeInteger();
        }

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
        if (PeekFloat() && SimpleParser.TryParseDouble(Tokens[CurrentTokenIndex++].Text, out double number))
            return number;

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
            if (Tokens[CurrentTokenIndex].Type == TokenType.Integer)
                return ConsumeInteger();

            Consume(TokenType.Minus);
            return -ConsumeFloat();
        }

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
    protected string ConsumeIdentifier()
    {
        if (PeekIdentifier())
            return Tokens[CurrentTokenIndex++].Text;

        Token token = Tokens[CurrentTokenIndex];
        throw new ParserException(token, $"Expected identifier, got a '{token.Type}' instead");
    }

    /// <summary>
    /// Consumes a string. Throws if it cannot.
    /// </summary>
    /// <returns>The string value (if it doesn't throw).</returns>
    /// <exception cref="ParserException">If there is no match or we ran
    /// out of tokens.
    /// </exception>
    protected string ConsumeString()
    {
        if (PeekString())
            return Tokens[CurrentTokenIndex++].Text;

        Token token = Tokens[CurrentTokenIndex];
        throw new ParserException(token, $"Expected text, got a '{token.Type}' instead");
    }

    /// <summary>
    /// Consumes the symbol provided in character form, or does nothing if
    /// there is no matching symbol after. This is equal to peeking, and
    /// consuming if it matches.
    /// </summary>
    /// <param name="c">The symbol character.</param>
    /// <returns>True if it consumed, false if not.</returns>
    protected bool ConsumeIf(char c)
    {
        if (!Peek(c))
            return false;

        Consume(c);
        return true;
    }

    /// <summary>
    /// Consumes the case insensitive string, or does nothing if there is
    /// no matching symbol after. This is equal to peeking, and consuming
    /// if it matches.
    /// </summary>
    /// <param name="str">The case insensitive string.</param>
    /// <returns>True if it consumed, false if not.</returns>
    protected bool ConsumeIf(string str)
    {
        if (!Peek(str))
            return false;

        Consume(str);
        return true;
    }

    /// <summary>
    /// Checks if the next token is an integer, and if so consumes it. This
    /// does not work for signed integers.
    /// </summary>
    /// <returns>The integer value if an integer is next, or null if not.
    /// </returns>
    protected int? ConsumeIfInt()
    {
        if (!PeekInteger())
            return null;
        return ConsumeSignedInteger();
    }

    protected double? ConsumeIfFloat()
    {
        if (!PeekFloat())
            return null;
        return ConsumeSignedFloat();
    }

    /// <summary>
    /// Will keep peeking for the character, and invoking the action if the
    /// peeking does not find it. As soon as it finds the character, it
    /// consumes it.
    /// </summary>
    /// <param name="c">The symbol character.</param>
    /// <param name="action">The action to invoke every time the character
    /// symbol is not found.</param>
    protected void InvokeUntilAndConsume(char c, Action action)
    {
        while (!Peek(c))
        {
            int beforeIndex = CurrentTokenIndex;
            action();
            if (CurrentTokenIndex == beforeIndex)
                throw new ParserException(Tokens[beforeIndex], "Infinite parsing loop detected, InvokeUntilAndConsume(char) usage incorrect (report to a developer)");
        }

        Consume(c);
    }

    /// <summary>
    /// Will keep peeking for the character, and invoking the action if the
    /// peeking does not find it. As soon as it finds the character, it
    /// consumes it.
    /// </summary>
    /// <param name="str">The symbol character.</param>
    /// <param name="action">The action to invoke every time the character
    /// symbol is not found.</param>
    protected void InvokeUntilAndConsume(string str, Action action)
    {
        while (!Peek(str))
        {
            int beforeIndex = CurrentTokenIndex;
            action();
            if (CurrentTokenIndex == beforeIndex)
                throw new ParserException(Tokens[beforeIndex], "Infinite parsing loop detected, InvokeUntilAndConsume(char) usage incorrect (report to a developer)");
        }

        Consume(str);
    }

    /// <summary>
    /// Throws an exception, halting parsing. The error message used is the
    /// one provided as the argument.
    /// </summary>
    /// <param name="reason">The reason for throwing.</param>
    /// <exception cref="ParserException">The exception.</exception>
    protected ParserException MakeException(string reason)
    {
        // If we get an index out of bounds, that is also handled. This can
        // occur when there are zero tokens in the stream, but it is all
        // handled.
        int index = (CurrentTokenIndex >= Tokens.Count ? Tokens.Count - 1 : CurrentTokenIndex);
        return new ParserException(Tokens[index], reason);
    }

    /// <summary>
    /// The start method of parsing. The inheriting child should implement
    /// this and carry out all of the parsing by using the methods provided
    /// in this class.
    /// </summary>
    /// <remarks>
    /// All the parsing invocations must be done in here. There is error
    /// handling that wraps this function which will take care of anything
    /// exceptional. Assertions will leak through though (as we'd want).
    /// </remarks>
    protected abstract void PerformParsing();

    private static void HandleParserException(ParserException e, string text)
    {
        // It may be possible for the log message to have interpolation
        // values in it. Don't know how the logging framework would
        // handle that correctly but I'll play it safe here by hoping
        // it doesn't recursively interpolate.
        Log.Error("Parsing error: {0}", e.Message);
        foreach (string logMessage in e.LogToReadableMessage(text))
            Log.Error("{0}", logMessage);
    }

    private void PrintUnexpectedErrorMessage()
    {
        if (CurrentTokenIndex >= 0 && CurrentTokenIndex < Tokens.Count)
        {
            Token token = Tokens[CurrentTokenIndex];
            Log.Error("Critical parsing error occured at token {0} ({1}), report this to a developer!", CurrentTokenIndex, token);
        }
        else
            Log.Error("Critical parsing error parsing text around character offset {0}, report this to a developer!)", CurrentTokenIndex);
    }
}
