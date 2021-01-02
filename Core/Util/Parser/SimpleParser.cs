using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

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

        private static readonly Regex CommentRegex = new Regex(@"(/\*([^*]|[\r\n]|(\*+([^*/]|[\r\n])))*\*+/)|(//.*)", RegexOptions.Singleline);
        private static readonly Regex NormalSplitRegex = new Regex("(?<=\")[^\"]*(?=\")|[^\" \t]+");
        private static readonly Regex CsvSplitRegex = new Regex("(?<=\")[^\"]*(?=\")|[^\",]+");

        private readonly List<ParserToken> m_tokens = new List<ParserToken>();
        private readonly ParseType m_parseType;

        private int m_index = 0;

        public SimpleParser(ParseType parseType = ParseType.Normal)
        {
            m_parseType = parseType;
        }

        public void Parse(string data)
        {
            m_index = 0;

            int lineCount = 0;
            string[] lines = data.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                string parseLine = CommentRegex.Replace(line, string.Empty).Trim();
                if (string.IsNullOrEmpty(parseLine))
                    continue;

                string[] subSplit;

                if (m_parseType == ParseType.Csv)
                    subSplit = CsvSplitRegex.Matches(parseLine).Cast<Match>().Select(x => x.Value).ToArray();
                else
                    subSplit = NormalSplitRegex.Matches(parseLine).Cast<Match>().Select(x => x.Value).ToArray();

                foreach (string sub in subSplit)
                    m_tokens.Add(new ParserToken(lineCount, sub.Trim()));

                lineCount++;
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

        public bool Peek(string str)
        {
            AssertData();

            if (m_tokens[m_index].Data == str)
                return true;

            return false;
        }

        public string ConsumeString()
        {
            AssertData();

            return m_tokens[m_index++].Data;
        }

        public void ConsumeString(string str)
        {
            AssertData();

            ParserToken token = m_tokens[m_index++];
            if (token.Data != str)
                throw new ParserException(token.Line, 0, 0, $"Expected {str} but got {token.Data}");
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

        public bool ConsumeBool()
        {
            AssertData();

            ParserToken token = m_tokens[m_index];
            if (bool.TryParse(token.Data, out bool b))
            {
                m_index++;
                return b;
            }

            throw new ParserException(token.Line, 0, 0, $"Could not parse {token.Data} as a bool.");
        }

        public void Consume(char c)
        {
            AssertData();

            ParserToken token = m_tokens[m_index];
            if (token.Data.Length != 1 || token.Data[0] != c)
                throw new ParserException(token.Line, 0, 0, $"Expected {c} but got {token.Data}.");

            m_index++;
        }

        /// <summary>
        /// Eats the rest of the tokens until the current line is consumed.
        /// </summary>
        public void ConsumeLine()
        {
            int startLine = m_tokens[m_index].Line;
            while (m_index < m_tokens.Count && m_tokens[m_index].Line != startLine)
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
    }
}
