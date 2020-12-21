using System;
using System.Collections.Generic;

namespace Helion.Util.Parser
{
    public class SimpleParser
    {
        private class ParserToken
        {
            public ParserToken(int line, string data)
            {
                Line = line;
                Data = data;
            }

            public int Line { get; private set; }
            public string Data { get; private set; }
        }

        private readonly List<ParserToken> m_tokens = new List<ParserToken>();

        private int m_index = 0;
        private bool m_multiLineComment;

        public void Parse(string data)
        {
            m_index = 0;
            m_multiLineComment = false;

            int lineCount = 0;
            string[] lines = data.Split(new[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                string parseLine = StripComments(line);
                string[] subSplit = parseLine.Split(new[] { '\t', ' ' }, StringSplitOptions.RemoveEmptyEntries);

                foreach (string sub in subSplit)
                    m_tokens.Add(new ParserToken(lineCount, sub));
            }
        }

        public int GetCurrentLine() => m_tokens[m_index].Line;

        public bool IsDone() => m_index >= m_tokens.Count;

        public bool Peek(char c)
        {
            AssertData();

            if (m_tokens[m_index].Data[0] == c)
                return true;

            return false;
        }

        public string ConsumeString()
        {
            return m_tokens[m_index++].Data;
        }

        public int ConsumeInteger()
        {
            AssertData();

            ParserToken token = m_tokens[m_index];
            if (int.TryParse(token.Data, out int i))
            {
                m_index++;
                return i;
            }

            throw new ParserException(token.Line, 0, 0, $"Could not parse {token.Data} as integer.");
        }

        public void Consume(char c)
        {
            AssertData();

            ParserToken token = m_tokens[m_index];
            if (token.Data.Length != 1 || token.Data[0] != c)
                throw new ParserException(token.Line, 0, 0, $"Expected {c} but got {token.Data}.");

            m_index++;
        }

        private void AssertData()
        {
            if (IsDone())
            {
                int line = m_tokens.Count == 0 ? 0 : m_tokens[^1].Line;
                throw new ParserException(line, 0, 0, "Hit end of file when expecting data.");
            }
        }

        private string StripComments(string line)
        {
            int start = 0;
            for (int i = 0; i < line.Length - 1; i++)
            {
                if (m_multiLineComment)
                {
                    if (line[i] == '*' && line[i + 1] == '/')
                    {
                        m_multiLineComment = false;
                        start = i + 1;
                    }
                }
                else
                {
                    if (line[i] == '/')
                    {
                        if (line[i + 1] == '/')
                        {
                            return line.Substring(start, i);
                        }
                        else if (line[i] == '*')
                        {
                            m_multiLineComment = true;
                            return line.Substring(start, i);
                        }
                    }
                }
            }

            return line;
        }
    }
}
