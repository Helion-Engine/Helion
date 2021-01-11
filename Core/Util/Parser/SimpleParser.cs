using System;
using System.Collections.Generic;

namespace Helion.Util.Parser
{
    public class SimpleParser
    {
        private class ParserToken
        {
            public ParserToken(int line, int index, int length)
            {
                Line = line;
                Index = index;
                Length = length;
            }

            public int Index { get; private set; }
            public int Line { get; private set; }
            public int Length { get; private set; }
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

            foreach (string line in m_lines)
            {
                bool isQuote = false;
                bool quotedString = false;
                bool split = false;
                int startIndex = 0;

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
                        if (!isQuote)
                            split = true;
                    }

                    if (!isQuote)
                    {
                        bool special = CheckSpecial(line[i]);
                        if (split || special || CheckSplit(line[i]))
                        {
                            AddToken(startIndex, i, lineCount, quotedString);
                            startIndex = i + 1;
                            split = false;
                            quotedString = false;
                        }

                        // Also add the special char as a token (e.g. '{')
                        if (special)
                            AddToken(i, i + 1, lineCount, quotedString);
                    }
                }

                if (isQuote)
                    throw new ParserException(lineCount, startIndex, 0, "Quote string was not ended.");

                if (!multiLineComment)
                    AddToken(startIndex, line.Length, lineCount, quotedString);

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

            return c == '{' || c == '}' || c == '=';
        }

        private void AddToken(int startIndex, int currentIndex, int lineCount, bool quotedString)
        {
            if (quotedString)
                startIndex++;

            if (startIndex != currentIndex)
                m_tokens.Add(new ParserToken(lineCount, startIndex, currentIndex - startIndex));
        }

        private static bool CheckNext(string str, int i, char c) => i + 1 < str.Length && str[i + 1] == c;

        public int GetCurrentLine() => m_tokens[m_index].Line;

        public bool IsDone() => m_index >= m_tokens.Count;

        public bool Peek(char c)
        {
            AssertData();

            if (char.ToUpperInvariant(GetCharData(m_index)) == char.ToUpperInvariant(c))
                return true;

            return false;
        }

        public bool Peek(string str)
        {
            AssertData();

            if (GetData(m_index).Equals(str, StringComparison.OrdinalIgnoreCase))
                return true;

            return false;
        }

        public string PeekString()
        {
            AssertData();
            return GetData(m_index);
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
            return m_lines[token.Line].Substring(token.Index, token.Length);
        }

        private char GetCharData(int index)
        {
            ParserToken token = m_tokens[index];
            return m_lines[token.Line].Substring(token.Index, 1)[0];
        }
    }
}
