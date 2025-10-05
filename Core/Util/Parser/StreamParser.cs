using Helion.Util.Container;
using System;
using System.IO;

namespace Helion.Util.Parser
{
    public class StreamParser
    {
        private readonly StreamReader m_stream;
        private readonly DynamicArray<char> m_buffer = new(1024);
        private int m_line;

        public StreamParser(Stream stream)
        {
            m_stream = new StreamReader(stream);
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
            if (!SimpleParser.TryParseDouble(data, out var d))
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
            return GetNextToken();
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
            if (m_stream.EndOfStream)
                return false;

            return c == m_stream.Peek();
        }

        public double ParseDouble(ReadOnlySpan<char> data)
        {
            if (!SimpleParser.TryParseDouble(data, out var d))
                throw new ParserException(m_line, -1, -1, $"Could not parse {data} as a double.");
            return d;
        }

        public float ParseFloat(ReadOnlySpan<char> data)
        {
            if (!SimpleParser.TryParseFloat(data, out var d))
                throw new ParserException(m_line, -1, -1, $"Could not parse {data} as a float.");
            return d;
        }

        public int ParseInt(ReadOnlySpan<char> data)
        {
            if (!int.TryParse(data, out var d))
                throw new ParserException(m_line, -1, -1, $"Could not parse {data} as a int.");
            return d;
        }

        private string GetNextToken()
        {
            AssertData();
            return ReadNextTokenSpan().ToString();
        }

        private ReadOnlySpan<char> ReadNextTokenSpan(DynamicArray<char>? buffer = null)
        {
            buffer ??= m_buffer;
            buffer.Clear();
            int nextChar;

            while ((nextChar = m_stream.Peek()) != -1)
            {
                var c = (char)nextChar;  
                if (c == ' ' || c == ';' || c == '\r' || c == '\n' || c == '(' || c == ')')
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

                    if (c == ';')
                        return ";";
                    if (c == ')')
                        return ")";
                    if (c == '(')
                        return "(";

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
                if (c == ' ' || c == ';' || c == '\r')
                    break;
                if (c == '/' && m_stream.Peek() == '/')
                    break;

                buffer.Add((char)m_stream.Read());
            }

            while (m_stream.Peek() == '\n')
                m_stream.Read();

            return buffer.Data.AsSpan(0, buffer.Length);
        }

        private void AssertData()
        {
            if (m_stream.EndOfStream)
                throw new ParserException(m_line, -1, -1, "Hit end of file when expecting data.");
        }
    }
}
