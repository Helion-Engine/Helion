using Helion.Util.Extensions;
using static Helion.Util.Assertion.Assert;

namespace Helion.Util.Parser;

/// <summary>
/// A token from a stream of characters.
/// </summary>
public struct Token
{
    /// <summary>
    /// The line number this token was read on.
    /// </summary>
    public readonly int LineNumber;

    /// <summary>
    /// The character offset from the beginning of the line. Zero implies
    /// it is the first character.
    /// </summary>
    public readonly int LineCharOffset;

    /// <summary>
    /// The character offset from the character stream.
    /// </summary>
    public readonly int CharOffset;

    /// <summary>
    /// The text for this token.
    /// </summary>
    public readonly string Text;

    /// <summary>
    /// The type of token this is.
    /// </summary>
    public readonly TokenType Type;

    /// <summary>
    /// Creates a new token.
    /// </summary>
    /// <param name="lineNumber">The line number.</param>
    /// <param name="lineCharOffset">The offset in characters from the left of
    /// the line.</param>
    /// <param name="charOffset">The offset from the beginning of the chars
    /// in the characters stream.</param>
    /// <param name="text">The actual characters.</param>
    /// <param name="type">The type of token this is.</param>
    public Token(int lineNumber, int lineCharOffset, int charOffset, string text, TokenType type)
    {
        Precondition(lineNumber > 0, "Line number must be 1 or greater");
        Precondition(lineCharOffset >= 0, "Line char offset must not be negative");
        Precondition(charOffset >= 0, "Char offset must not be negative");
        Precondition(type == TokenType.QuotedString || !text.Empty(), "A token cannot have an empty string (unless it's a quoted string)");

        LineNumber = lineNumber;
        LineCharOffset = lineCharOffset;
        CharOffset = charOffset;
        Text = text;
        Type = type;
    }

    /// <summary>
    /// Creates a token from a character which should be a symbol.
    /// </summary>
    /// <param name="lineNumber">The line number.</param>
    /// <param name="lineCharOffset">The offset in characters from the left of
    /// the line.</param>
    /// <param name="charOffset">The offset from the beginning of the chars
    /// in the characters stream.</param>
    /// <param name="c">The symbol to process. Should not be anything that
    /// is not a symbol.</param>
    public Token(int lineNumber, int lineCharOffset, int charOffset, char c) :
        this(lineNumber, lineCharOffset, charOffset, c.ToString(), ToTokenType(c))
    {
        Precondition(Type != TokenType.String, "Token symbol constructor did not get a symbol");
    }

    public override string ToString() => $"\"{Text}\" (line {LineNumber}, offset {LineCharOffset}, type {Type})";

    private static TokenType ToTokenType(char c)
    {
        switch (c)
        {
        case '`':
            return TokenType.Backtick;
        case '~':
            return TokenType.Tilde;
        case '!':
            return TokenType.Exclamation;
        case '@':
            return TokenType.At;
        case '#':
            return TokenType.Hash;
        case '$':
            return TokenType.Dollar;
        case '%':
            return TokenType.Percent;
        case '^':
            return TokenType.Caret;
        case '&':
            return TokenType.Ampersand;
        case '*':
            return TokenType.Asterisk;
        case '(':
            return TokenType.ParenLeft;
        case ')':
            return TokenType.ParenRight;
        case '+':
            return TokenType.Plus;
        case '-':
            return TokenType.Minus;
        case '=':
            return TokenType.Equals;
        case '[':
            return TokenType.BracketLeft;
        case ']':
            return TokenType.BracketRight;
        case '{':
            return TokenType.BraceLeft;
        case '}':
            return TokenType.BraceRight;
        case '\\':
            return TokenType.Backslash;
        case '|':
            return TokenType.Pipe;
        case ':':
            return TokenType.Colon;
        case ';':
            return TokenType.Semicolon;
        case '<':
            return TokenType.AngleLeft;
        case '>':
            return TokenType.AngleRight;
        case ',':
            return TokenType.Comma;
        case '.':
            return TokenType.Period;
        case '/':
            return TokenType.Slash;
        case '?':
            return TokenType.QuestionMark;
        default:
            return TokenType.String;
        }
    }
}

