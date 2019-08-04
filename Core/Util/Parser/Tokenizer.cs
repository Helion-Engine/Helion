using System.Collections.Generic;

namespace Helion.Util.Parser
{
    public class Tokenizer
    {
        private List<Token> m_tokens = new List<Token>();

        public static List<Token> Read(string text)
        {
            Tokenizer tokenizer = new Tokenizer();
            tokenizer.Tokenize(text);
            return tokenizer.m_tokens;
        }

        private void Tokenize(string text)
        {
            int lineNumber = 1;
            for (int index = 0; index < text.Length; index++)
            {
                char currentChar = text[index];
                // TODO
            }
        }
    }
}