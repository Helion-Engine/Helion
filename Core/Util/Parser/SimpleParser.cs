using System;
using System.Collections.Generic;
using System.Text;

namespace Helion.Util.Parser
{
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

        private readonly List<ParserToken> m_tokens = new List<ParserToken>();
        private readonly ParseType m_parseType;
        private string[] m_lines = Array.Empty<string>();

        private int m_index = 0;

        public SimpleParser(ParseType parseType = ParseType.Normal)
        {
            m_parseType = parseType;
        }

        public void Parse(string data)
        {
            m_index = 0;
            m_lines = data.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
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
                if (!isQuote)
                {
                    isQuote = false;
                    quotedString = false;
                    split = false;
                    startLine = lineCount;
                }

                startIndex = 0;

                for (int i = 0; i < line.Length; i++)
                {
                    if (IsSingleLineComment(line, i))
                    {
                        if (i > 0)
                            AddToken(startIndex, i, lineCount, false);
                        startIndex = line.Length;
                        break;
                    }

                    if (IsStartMultiLineComment(line, i))
                    {
                        multiLineComment = true;
                        i += 2;
                    }

                    if (multiLineComment && IsEndMultiLineComment(line, i))
                    {
                        multiLineComment = false;
                        i += 2;
                        startIndex = i;
                    }

                    if (i >= line.Length)
                        break;

                    if (multiLineComment)
                        continue;

                    if (line[i] == '"')
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
                            quotedString = false;
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
        }

        private static bool IsEndMultiLineComment(string line, int i)
            => line[i] == '*' && CheckNext(line, i, '/');

        private static bool IsStartMultiLineComment(string line, int i)
            => line[i] == '/' && CheckNext(line, i, '*');

        private static bool IsSingleLineComment(string line, int i)
            => line[i] == '/' && CheckNext(line, i, '/');

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

            return c == '{' || c == '}' || c == '=' || c == ';' || c == ',';
        }

        private void AddToken(int startIndex, int currentIndex, int lineCount, bool quotedString)
        {
            if (quotedString)
                startIndex++;

            if (startIndex != currentIndex)
                m_tokens.Add(new ParserToken(lineCount, startIndex, currentIndex - startIndex));
        }

        private void AddToken(int startIndex, int startLine, int endLine, int endIndex, bool quotedString)
        {
            if (quotedString)
                startIndex++;

            m_tokens.Add(new ParserToken(startLine, startIndex, endIndex, endLine, endIndex));
        }

        private static bool CheckNext(string str, int i, char c) => i + 1 < str.Length && str[i + 1] == c;

        public int GetCurrentLine() => m_tokens[m_index].Line;
        public int GetCurrentCharOffset() => m_tokens[m_index].Index;

        public bool IsDone() => m_index >= m_tokens.Count;

        public bool Peek(char c)
        {
            if (IsDone())
                return false;

            if (char.ToUpperInvariant(GetCharData(m_index)) == char.ToUpperInvariant(c))
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
            AssertData();
            return GetData(m_index);
        }

        public bool PeekInteger(out int i)
        {
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
                throw new ParserException(token.Line, token.Index, 0, $"Expected {str} but got {data}");

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
                return i;

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

            throw new ParserException(token.Line, token.Index, 0, $"Could not parse {data} as integer.");
        }

        public double ConsumeDouble()
        {
            AssertData();

            ParserToken token = m_tokens[m_index];
            string data = GetData(m_index);
            if (double.TryParse(data, out double d))
            {
                m_index++;
                return d;
            }

            throw new ParserException(token.Line, token.Index, 0, $"Could not parse {data} as a double.");
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

            throw new ParserException(token.Line, token.Index, 0, $"Could not parse {data} as a bool.");
        }

        public void Consume(char c)
        {
            AssertData();

            ParserToken token = m_tokens[m_index];
            string data = GetData(m_index);
            if (data.Length != 1 || char.ToUpperInvariant(data[0]) != char.ToUpperInvariant(c))
                throw new ParserException(token.Line, token.Index, 0, $"Expected {c} but got {data}.");

            m_index++;
        }

        /// <summary>
        /// Eats the rest of the tokens until the current line is consumed.
        /// </summary>
        public string ConsumeLine()
        {
            AssertData();

            ParserToken token = m_tokens[m_index];
            int startLine = m_tokens[m_index].Line;
            while (m_index < m_tokens.Count && m_tokens[m_index].Line == startLine)
                m_index++;

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
                throw new ParserException(line, m_lines[^1].Length - 1, 0, "Hit end of file when expecting data.");
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
                StringBuilder sb = new StringBuilder();
                for (int i = token.Line; i < token.EndLine + 1; i++)
                {
                    if (i == token.EndLine)
                    {
                        sb.Append(m_lines[i].Substring(0, token.EndIndex));
                    }
                    else
                    {
                        if (i == token.Line)
                            sb.Append(m_lines[i].Substring(token.Index));
                        else
                            sb.Append(m_lines[i]);

                        sb.Append('\n');
                    }
                }

                return sb.ToString();
            }
        }

        private char GetCharData(int index)
        {
            ParserToken token = m_tokens[index];
            return m_lines[token.Line].Substring(token.Index, 1)[0];
        }
    }
}
