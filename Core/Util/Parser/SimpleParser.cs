using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Helion.Util.Parser;

public readonly record struct ParserOffset(int Line, int Char);

public class SimpleParser
{
    private class ParserToken
    {
        public ParserToken(int line, int index, int length,
            int endLine = -1, int endIndex = -1)
        {
            Line = line;
            Index = index;
            Length = length;
            EndLine = endLine;
            EndIndex = endIndex;
        }

        public int Index { get; private set; }
        public int Line { get; private set; }
        public int Length { get; private set; }
        public int EndLine { get; private set; }
        public int EndIndex { get; private set; }
    }

    private readonly List<ParserToken> m_tokens = new();
    private readonly HashSet<char> m_special = new();
    private readonly ParseType m_parseType;
    private string[] m_lines = Array.Empty<string>();
    private Func<string, int, bool>? m_commentCallback;

    private int m_index = 0;

    private static readonly NumberFormatInfo DecimalFormat = new NumberFormatInfo { NumberDecimalSeparator = "." };

    public static bool TryParseDouble(string text, out double d) =>
        double.TryParse(text, NumberStyles.AllowDecimalPoint, DecimalFormat, out d);

    public static bool TryParseFloat(string text, out float f) =>
        float.TryParse(text, NumberStyles.AllowDecimalPoint, DecimalFormat, out f);

    private static readonly char[] SpecialChars = ['{', '}', '=', ';', ',', '[', ']'];
    private static readonly string[] SplitLines = ["\r\n", "\n"];

    public SimpleParser(ParseType parseType = ParseType.Normal)
    {
        m_parseType = parseType;
        SetSpecialChars(SpecialChars);
    }

    public void SetSpecialChars(IEnumerable<char> special)
    {
        m_special.Clear();
        foreach (char c in special)
            m_special.Add(c);
    }

    public void SetCommentCallback(Func<string, int, bool> callback) =>
        m_commentCallback = callback;

    public void Parse(string data, bool keepEmptyLines = false, bool parseQuotes = true)
    {
        m_index = 0;
        m_lines = data.Split(SplitLines, StringSplitOptions.None);
        bool multiLineComment = false;
        int lineCount = 0;
        int startLine = 0;

        bool isQuote = false;
        bool quotedString = false;
        bool split = false;
        int startIndex;
        int saveStartIndex = 0;

        foreach (string line in m_lines)
        {
            if (line.Length == 0)
            {
                if (keepEmptyLines || quotedString)
                    m_tokens.Add(new ParserToken(lineCount, 0, 0));
                lineCount++;
                continue;
            }

            if (!isQuote)
                ResetQuote();

            startIndex = 0;

            for (int i = 0; i < line.Length; i++)
            {
                if (!isQuote && IsSingleLineComment(line, i))
                {
                    if (i > 0)
                        AddToken(startIndex, i, lineCount, false);
                    startIndex = line.Length;
                    break;
                }

                if (!isQuote && IsStartMultiLineComment(line, ref i))
                    multiLineComment = true;

                if (multiLineComment && IsEndMultiLineComment(line, ref i))
                {
                    multiLineComment = false;
                    startIndex = i;
                }

                if (i >= line.Length)
                    break;

                if (multiLineComment)
                    continue;

                if (parseQuotes && line[i] == '"')
                {
                    quotedString = true;
                    isQuote = !isQuote;
                    if (isQuote)
                    {
                        AddToken(startIndex, i, lineCount, false);
                        saveStartIndex = i;
                    }
                    else
                    {
                        split = true;
                    }
                }

                if (!isQuote)
                {
                    bool special = CheckSpecial(line[i]);
                    if (split || special || CheckSplit(line[i]))
                    {
                        if (startLine == lineCount)
                            AddToken(startIndex, i, lineCount, quotedString);
                        else
                            AddToken(saveStartIndex, startLine, lineCount, i, quotedString);
                        startIndex = i + 1;
                        split = false;

                        ResetQuote();
                    }

                    // Also add the special char as a token (e.g. '{')
                    if (special)
                        AddToken(i, i + 1, lineCount, quotedString);
                }
            }

            if (!isQuote && !multiLineComment)
            {
                if (startLine == lineCount)
                    AddToken(startIndex, line.Length, lineCount, quotedString);
                else if (line.Length != startIndex)
                    AddToken(saveStartIndex, startLine, lineCount, startIndex, quotedString);
            }

            lineCount++;
        }

        void ResetQuote()
        {
            isQuote = false;
            quotedString = false;
            split = false;
            startLine = lineCount;
        }
    }

    // Just for debugging purposes
    public List<string> GetAllTokenStrings()
    {
        List<string> tokens = new();
        for (int i = 0; i < m_tokens.Count; i++)
            tokens.Add(GetData(i));
        return tokens;
    }

    private static bool IsEndMultiLineComment(string line, ref int i)
    {
        if (line.Length < 2 || i >= line.Length)
            return false;

        if (line[i] != '*' || !CheckNext(line, i, '/'))
            return false;

        i += 2;
        return true;
    }


    private static bool IsStartMultiLineComment(string line, ref int i)
    {
        if (line.Length < 2)
            return false;

        if (line[i] != '/' || !CheckNext(line, i, '*'))
            return false;

        i+=2;
        return true;
    }

    private bool IsSingleLineComment(string line, int i)
        => (m_commentCallback != null && m_commentCallback(line, i)) || (line[i] == '/' && CheckNext(line, i, '/'));

    private bool CheckSplit(char c)
    {
        if (m_parseType == ParseType.Normal)
            return c == ' ' || c == '\t';
        else
            return c == ',';
    }

    private bool CheckSpecial(char c)
    {
        if (m_parseType != ParseType.Normal)
            return false;

        return m_special.Contains(c);
    }

    private void AddToken(int startIndex, int currentIndex, int lineCount, bool quotedString)
    {
        if (quotedString)
            startIndex++;

        // Always add empty string if in quotes
        if (quotedString || startIndex != currentIndex)
            m_tokens.Add(new ParserToken(lineCount, startIndex, currentIndex - startIndex));
    }

    private void AddToken(int startIndex, int startLine, int endLine, int endIndex, bool quotedString)
    {
        if (quotedString)
            startIndex++;

        m_tokens.Add(new ParserToken(startLine, startIndex, endIndex, endLine, endIndex));
    }

    private static bool CheckNext(string str, int i, char c) => i + 1 < str.Length && str[i + 1] == c;

    public int GetCurrentLine() => IsDone() ? - 1 : m_tokens[m_index].Line;
    public int GetCurrentCharOffset() => IsDone() ? -1 : m_tokens[m_index].Index;

    public ParserOffset GetCurrentOffset() => new(GetCurrentLine(), GetCurrentCharOffset());
    public bool IsDone() => m_index >= m_tokens.Count;

    public bool Peek(char c)
    {
        if (IsDone())
            return false;

        if (!GetCharData(m_index, out char getChar))
            return false;

        if (char.ToUpperInvariant(getChar) == char.ToUpperInvariant(c))
            return true;

        return false;
    }

    public bool Peek(string str)
    {
        if (IsDone())
            return false;

        if (GetData(m_index).Equals(str, StringComparison.OrdinalIgnoreCase))
            return true;

        return false;
    }

    public string PeekString()
    {
        if (IsDone())
            return string.Empty;

        AssertData();
        return GetData(m_index);
    }

    public bool PeekString(int offset, out string? data)
    {
        data = null;
        if (m_index + offset >= m_tokens.Count)
            return false;

        data = GetData(m_index + offset);
        return true;
    }

    public bool PeekInteger(out int i)
    {
        if (IsDone())
        {
            i = 0;
            return false;
        }

        AssertData();
        return int.TryParse(GetData(m_index), out i);
    }

    public string ConsumeString()
    {
        AssertData();
        return GetData(m_index++);
    }

    public void ConsumeString(string str)
    {
        AssertData();

        ParserToken token = m_tokens[m_index];
        string data = GetData(m_index);
        if (!data.Equals(str, StringComparison.OrdinalIgnoreCase))
            throw new ParserException(token.Line, token.Index, -1, $"Expected {str} but got {data}");

        m_index++;
    }

    public bool ConsumeIf(string str)
    {
        if (IsDone())
            return false;

        if (str.Equals(PeekString(), StringComparison.OrdinalIgnoreCase))
        {
            ConsumeString();
            return true;
        }

        return false;
    }

    public int? ConsumeIfInt()
    {
        if (IsDone())
            return null;

        if (PeekInteger(out int i))
        {
            ConsumeString();
            return i;
        }

        return null;
    }

    public int ConsumeInteger()
    {
        AssertData();

        ParserToken token = m_tokens[m_index];
        string data = GetData(m_index);
        if (int.TryParse(data, out int i))
        {
            m_index++;
            return i;
        }

        throw new ParserException(token.Line, token.Index, -1, $"Could not parse {data} as integer.");
    }

    public double ConsumeDouble()
    {
        AssertData();

        ParserToken token = m_tokens[m_index];
        string data = GetData(m_index);
        if (TryParseDouble(data, out double d))
        {
            m_index++;
            return d;
        }

        throw new ParserException(token.Line, token.Index, -1, $"Could not parse {data} as a double.");
    }

    public bool ConsumeBool()
    {
        AssertData();

        ParserToken token = m_tokens[m_index];
        string data = GetData(m_index);
        if (bool.TryParse(data, out bool b))
        {
            m_index++;
            return b;
        }

        throw new ParserException(token.Line, token.Index, -1, $"Could not parse {data} as a bool.");
    }

    public void Consume(char c)
    {
        AssertData();

        ParserToken token = m_tokens[m_index];
        string data = GetData(m_index);
        if (data.Length != 1 || char.ToUpperInvariant(data[0]) != char.ToUpperInvariant(c))
            throw new ParserException(token.Line, token.Index, -1, $"Expected {c} but got {data}.");

        m_index++;
    }

    /// <summary>
    /// Eats the rest of the tokens until the current line is consumed.
    /// </summary>
    public string ConsumeLine(bool keepBeginningSpaces = false)
    {
        AssertData();

        ParserToken token = m_tokens[m_index];
        int startLine = m_tokens[m_index].Line;
        while (m_index < m_tokens.Count && m_tokens[m_index].Line == startLine)
            m_index++;

        if (keepBeginningSpaces)
            return m_lines[token.Line];

        return m_lines[token.Line][token.Index..];
    }

    /// <summary>
    /// Returns all tokens until the next line is hit.
    /// </summary>
    public string PeekLine()
    {
        AssertData();
        int index = m_index;

        ParserToken token = m_tokens[index];
        int startLine = m_tokens[index].Line;
        while (index < m_tokens.Count && m_tokens[index].Line == startLine)
            index++;

        return m_lines[token.Line][token.Index..];
    }

    public ParserException MakeException(string reason)
    {
        ParserToken token;
        if (m_index < m_tokens.Count)
            token = m_tokens[m_index];
        else
            token = m_tokens[^1];

        return new ParserException(token.Line, token.Index, 0, reason);
}

    private void AssertData()
    {
        if (IsDone())
        {
            int line = m_tokens.Count == 0 ? 0 : m_tokens[^1].Line;
            throw new ParserException(line, m_lines[^1].Length - 1, -1, "Hit end of file when expecting data.");
        }
    }

    private string GetData(int index)
    {
        ParserToken token = m_tokens[index];

        if (token.EndLine == -1)
        {
            return m_lines[token.Line].Substring(token.Index, token.Length);
        }
        else
        {
            StringBuilder sb = new();
            for (int i = token.Line; i < token.EndLine + 1; i++)
            {
                if (i == token.EndLine)
                {
                    sb.Append(m_lines[i].AsSpan(0, token.EndIndex));
                }
                else
                {
                    if (i == token.Line)
                        sb.Append(m_lines[i].AsSpan(token.Index));
                    else
                        sb.Append(m_lines[i]);

                    sb.Append('\n');
                }
            }

            return sb.ToString();
        }
    }

    private bool GetCharData(int index, out char c)
    {
        ParserToken token = m_tokens[index];
        var line = m_lines[token.Line];

        if (token.Index >= line.Length)
        {
            c = ' ';
            return false;
        }

        c = line[token.Index];
        return true;
    }
}
