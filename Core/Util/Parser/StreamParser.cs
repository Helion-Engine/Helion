using Helion.Util.Container;
using System;
using System.Collections.Frozen;
using System.IO;

namespace Helion.Util.Parser;

public sealed class StreamParser
{
    public bool LastTokenWasQuoted;
    private bool m_peek;

    private readonly StreamReader m_stream;
    private readonly DynamicArray<char> m_buffer = new(1024);
    private readonly FrozenSet<char> m_specialChars;
    private int m_line;

    public StreamParser(Stream utf8Stream, FrozenSet<char> specialChars)
    {
        m_stream = new(utf8Stream, System.Text.Encoding.UTF8);
        m_specialChars = specialChars;
    }

    public void Consume(char c)
    {
        var data = ReadNextTokenSpan();
        if (data.Length != 1 || data[0] != c)
            throw new ParserException(m_line, -1, -1, $"Expected {c} but got {data}");
    }

    public double ConsumeDouble()
    {
        var data = ReadNextTokenSpan();
        if (!NumberParser.TryParseDouble(data, out var d))
            throw new ParserException(m_line, -1, -1, $"Expected double but got {data}");
        return d;
    }

    public void ConsumeString(string str)
    {
        var data = ReadNextTokenSpan();
        if (!data.Equals(str, StringComparison.OrdinalIgnoreCase))
            throw new ParserException(m_line, -1, -1, $"Expected {str} but got {data}");
    }

    public string ConsumeString()
    {
        return ReadNextTokenSpan().ToString();
    }

    // Uses the passed buffer to read the next token and returns the buffer data as a span.
    public ReadOnlySpan<char> ConsumeStringSpan(DynamicArray<char> buffer)
    {
        return ReadNextTokenSpan(buffer);
    }

    public bool IsDone()
    {
        return m_stream.EndOfStream;
    }

    public bool Peek(char c)
    {
        if (!m_peek)
        {
            ReadNextTokenSpan();
            m_peek = true;
        }
        return m_buffer.Length == 1 && m_buffer[0] == c;
    }

    public bool Peek(string str)
    {
        if (!m_peek)
        {
            ReadNextTokenSpan();
            m_peek = true;
        }
        var peekSpan = m_buffer.Data.AsSpan(0, m_buffer.Length);
        return str.AsSpan(0, str.Length).Equals(peekSpan, StringComparison.OrdinalIgnoreCase);
    }

    public double ParseDouble(ReadOnlySpan<char> data)
    {
        if (!NumberParser.TryParseDouble(data, out var d))
            throw new ParserException(m_line, -1, -1, $"Could not parse {data} as a double.");
        return d;
    }

    public float ParseFloat(ReadOnlySpan<char> data)
    {
        if (!NumberParser.TryParseFloat(data, out var d))
            throw new ParserException(m_line, -1, -1, $"Could not parse {data} as a float.");
        return d;
    }

    public int ParseInt(ReadOnlySpan<char> data)
    {
        if (!int.TryParse(data, out var d))
            throw new ParserException(m_line, -1, -1, $"Could not parse {data} as a int.");
        return d;
    }

    public ParserException MakeException(string exception)
    {
        throw new ParserException(m_line, -1, -1, exception);
    }

    private ReadOnlySpan<char> ReadNextTokenSpan(DynamicArray<char>? buffer = null)
    {
        if (m_peek)
        {
            m_peek = false;
            if (buffer != null && buffer != m_buffer)
            {
                buffer.EnsureCapacity(m_buffer.Length);
                Array.Copy(m_buffer.Data, buffer.Data, m_buffer.Length);
                buffer.Length = m_buffer.Length;
                return buffer.Data.AsSpan(0, buffer.Length);
            }
            return m_buffer.Data.AsSpan(0, m_buffer.Length);
        }

        LastTokenWasQuoted = false;
        buffer ??= m_buffer;
        buffer.Clear();
        int nextChar;

        while ((nextChar = m_stream.Peek()) != -1)
        {
            var c = (char)nextChar;
            var hasSpecial = m_specialChars.Contains(c);
            if (hasSpecial || c == ' ' || c == '\r' || c == '\n' || c == '\t')
            {
                if (c == '\n')
                    m_line++;

                m_stream.Read();

                var peekChar = m_stream.Peek();
                while (peekChar == '\n' || peekChar == '\r')
                {
                    if (c == '\n')
                        m_line++; 
                    m_stream.Read();
                    peekChar = m_stream.Peek();
                }

                if (hasSpecial)
                {
                    buffer.Data[0] = c;
                    buffer.Length = 1;
                    return buffer.Data.AsSpan(0, 1);
                }

                continue;
            }

            if (c == '/')
            {
                m_stream.Read();
                var nextCommentChar = m_stream.Peek();
                if (m_stream.Peek() == '/')
                {
                    m_stream.Read();
                    while ((nextChar = m_stream.Read()) != -1 && nextChar != '\n' && nextChar != '\r') ;
                    continue;
                }
                else if (nextCommentChar == '*')
                {
                    m_stream.Read();
                    while ((nextChar = m_stream.Read()) != -1)
                    {
                        if ((char)nextChar == '*')
                        {
                            if (m_stream.Peek() == '/')
                            {
                                m_stream.Read();
                                break;
                            }
                        }
                    }
                    continue;
                }
                else
                {
                    buffer.Add('/');
                    break;
                }
            }

            break;
        }

        if ((char)m_stream.Peek() == '"')
        {
            LastTokenWasQuoted = true;
            m_stream.Read();
            while ((nextChar = m_stream.Read()) != -1)
            {
                var c = (char)nextChar;
                if (c == '"')
                    break;

                buffer.Add(c);
            }
            return buffer.Data.AsSpan(0, buffer.Length);
        }

        while ((nextChar = m_stream.Peek()) != -1)
        {
            var c = (char)nextChar;
            if (c == '\n')
            {
                m_line++;
                break;
            }
            if (c == ' ' || c == '\r' || c == '\t' || m_specialChars.Contains(c))
                break;

            m_stream.Read();
            if (c == '/' && m_stream.Peek() == '/')
                break;

            buffer.Add(c);
        }

        while (m_stream.Peek() == '\n')
            m_stream.Read();

        if (buffer.Length == 0)
            throw new ParserException(m_line, -1, -1, "Hit end of file when expecting data.");

        return buffer.Data.AsSpan(0, buffer.Length);
    }
}
