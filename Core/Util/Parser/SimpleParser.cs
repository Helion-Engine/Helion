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

        private static readonly Regex NormalSplitRegex = new Regex("(?<=\")[^\"]*(?=\")|[^\" \t]+");
        private static readonly Regex CsvSplitRegex = new Regex("(?<=\")[^\"]*(?=\")|[^\",]+");

        private readonly List<ParserToken> m_tokens = new List<ParserToken>();
        private readonly ParseType m_parseType;

        private int m_index = 0;
        private bool m_multiLineComment;

        public SimpleParser(ParseType parseType = ParseType.Normal)
        {
            m_parseType = parseType;
        }

        public void Parse(string data)
        {
            m_index = 0;
            m_multiLineComment = false;
            int lineCount = 0;
            string[] lines = data.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.RemoveEmptyEntries);
            foreach (string line in lines)
            {
                string parseLine = line.Trim();
                if (string.IsNullOrEmpty(parseLine))
                    continue;

                string[] subSplit;

                if (m_parseType == ParseType.Csv)
                    subSplit = CsvSplitRegex.Matches(parseLine).Cast<Match>().Select(x => x.Value).ToArray();
                else
                    subSplit = NormalSplitRegex.Matches(parseLine).Cast<Match>().Select(x => x.Value).ToArray();

                foreach (string sub in subSplit)
                {
                    bool comment = false;
                    string parseSub = sub;

                    if (IsComment(parseSub))
                    {
                        comment = true;
                        parseSub = StripComment(parseSub);
                    }

                    if (m_multiLineComment)
                    {
                        if (!IsEndMultiLineComment(parseSub))
                            continue;

                        m_multiLineComment = false;
                        parseSub = StripEndMultiLineComment(parseSub);
                    }

                    if (IsStartMultiLineComment(parseSub))
                    {
                        m_multiLineComment = !IsEndMultiLineComment(parseSub);
                        parseSub = StripStartMultiLineComment(parseSub);
                    }

                    parseSub = parseSub.Trim();
                    if (parseSub.Length > 0)
                        m_tokens.Add(new ParserToken(lineCount, parseSub));

                    if (comment)
                        break;
                }

                lineCount++;
            }
        }


        public int GetCurrentLine() => m_tokens[m_index].Line;

        public bool IsDone() => m_index >= m_tokens.Count;

        public bool Peek(char c)
        {
            AssertData();

            if (char.ToUpperInvariant(m_tokens[m_index].Data[0]) == char.ToUpperInvariant(c))
                return true;

            return false;
        }

        public bool Peek(string str)
        {
            AssertData();

            if (m_tokens[m_index].Data.Equals(str, StringComparison.OrdinalIgnoreCase))
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
            if (!token.Data.Equals(str, StringComparison.OrdinalIgnoreCase))
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
            if (token.Data.Length != 1 || char.ToUpperInvariant(token.Data[0]) != char.ToUpperInvariant(c))
                throw new ParserException(token.Line, 0, 0, $"Expected {c} but got {token.Data}.");

            m_index++;
        }

        /// <summary>
        /// Eats the rest of the tokens until the current line is consumed.
        /// </summary>
        public void ConsumeLine()
        {
            int startLine = m_tokens[m_index].Line;
            while (m_index < m_tokens.Count && m_tokens[m_index].Line == startLine)
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

        private bool IsComment(string data) => data.Contains("//");
        private bool IsStartMultiLineComment(string data) => data.Contains("/*");
        private bool IsEndMultiLineComment(string data) => data.Contains("*/");

        private string StripComment(string data) => data.Substring(0, data.IndexOf("//"));
        private string StripStartMultiLineComment(string data) => data.Substring(0, data.IndexOf("/*"));
        private string StripEndMultiLineComment(string data) => data.Substring(data.IndexOf("*/") + 2);
    }
}
