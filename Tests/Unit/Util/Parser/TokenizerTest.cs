using System.Collections.Generic;
using System.Text;
using Helion.Util.Extensions;
using Helion.Util.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Unit.Util.Parser
{
    [TestClass]
    public class TokenizerTest
    {
        private const string AllSymbolTokens = "~`!@#$%^&*()+-=[]{}\\|:;<>,./?";

        private static readonly List<TokenType> AllSymbolTokenTypes = new List<TokenType>
        {
            TokenType.Tilde,
            TokenType.Backtick,
            TokenType.Exclamation,
            TokenType.At,
            TokenType.Hash,
            TokenType.Dollar,
            TokenType.Percent,
            TokenType.Caret,
            TokenType.Ampersand,
            TokenType.Asterisk,
            TokenType.ParenLeft,
            TokenType.ParenRight,
            TokenType.Plus,
            TokenType.Minus,
            TokenType.Equals,
            TokenType.BracketLeft,
            TokenType.BracketRight,
            TokenType.BraceLeft,
            TokenType.BraceRight,
            TokenType.Backslash,
            TokenType.Pipe,
            TokenType.Colon,
            TokenType.Semicolon,
            TokenType.AngleLeft,
            TokenType.AngleRight,
            TokenType.Comma,
            TokenType.Period,
            TokenType.Slash,
            TokenType.QuestionMark,
        };
        
        private static void AssertToken(Token token, int line, int lineOffset, int charOffset, string text, TokenType type)
        {
            Assert.AreEqual(line, token.LineNumber);
            Assert.AreEqual(lineOffset, token.LineCharOffset);
            Assert.AreEqual(charOffset, token.CharOffset);
            Assert.AreEqual(text, token.Text);
            Assert.AreEqual(type, token.Type);
        }
        
        [TestMethod]
        public void EmptyTextIsEmptyList()
        {
            Assert.IsTrue(Tokenizer.Read("").Empty());
        }
        
        [TestMethod]
        public void TokenizeAllSymbolsTogether()
        {
            List<Token> tokens = Tokenizer.Read(AllSymbolTokens);

            Assert.AreEqual(AllSymbolTokens.Length, tokens.Count);
            
            for (int i = 0; i < tokens.Count; i++)
            {
                Token token = tokens[i];
                Assert.AreEqual(1, token.LineNumber);
                Assert.AreEqual(i, token.LineCharOffset);
                Assert.AreEqual(i, token.CharOffset);
                Assert.AreEqual(AllSymbolTokens[i], token.Text[0]);
                Assert.AreEqual(AllSymbolTokenTypes[i], token.Type);
            }
        }
        
        [TestMethod]
        public void TokenizeAllSymbolsSpaced()
        {
            StringBuilder spacedBuilder = new StringBuilder();
            spacedBuilder.Append(" ");
            foreach (char c in AllSymbolTokens)
                spacedBuilder.Append(c + " ");
            
            StringBuilder tabbedBuilder = new StringBuilder();
            tabbedBuilder.Append("\t");
            foreach (char c in AllSymbolTokens)
                tabbedBuilder.Append(c + "\t");
            
            AssertExpectedTokens(Tokenizer.Read(spacedBuilder.ToString()));
            AssertExpectedTokens(Tokenizer.Read(tabbedBuilder.ToString()));

            void AssertExpectedTokens(IList<Token> tokens)
            {
                Assert.AreEqual(AllSymbolTokens.Length, tokens.Count);
                
                int charOffset = 1;
                for (int i = 0; i < tokens.Count; i++)
                {
                    Token token = tokens[i];
                    Assert.AreEqual(1, token.LineNumber);
                    Assert.AreEqual(charOffset, token.LineCharOffset);
                    Assert.AreEqual(charOffset, token.CharOffset);
                    Assert.AreEqual(AllSymbolTokens[i], token.Text[0]);
                    Assert.AreEqual(AllSymbolTokenTypes[i], token.Type);

                    charOffset += 2;
                }
            }
        }
        
        [TestMethod]
        public void TokenizeAllSymbolsNewLine()
        {
            StringBuilder builder = new StringBuilder();
            builder.Append("\n");
            foreach (char c in AllSymbolTokens)
                builder.Append(c + "\n");
            
            List<Token> tokens = Tokenizer.Read(builder.ToString());
            for (int i = 0; i < tokens.Count; i++)
            {
                Token token = tokens[i];
                Assert.AreEqual(2 + i, token.LineNumber);
                Assert.AreEqual(0, token.LineCharOffset);
                Assert.AreEqual((2 * i) + 1, token.CharOffset);
                Assert.AreEqual(AllSymbolTokens[i], token.Text[0]);
                Assert.AreEqual(AllSymbolTokenTypes[i], token.Type);
            }
        }
        
        [TestMethod]
        public void TokenizeStringAndQuotedString()
        {
            List<Token> tokens = Tokenizer.Read("abc _abc\t \tHello \"hi{-5\" \"\"");
            
            Assert.AreEqual(5, tokens.Count);
            AssertToken(tokens[0], 1, 0, 0, "abc", TokenType.String);
            AssertToken(tokens[1], 1, 4, 4, "_abc", TokenType.String);
            AssertToken(tokens[2], 1, 11, 11, "Hello", TokenType.String);
            AssertToken(tokens[3], 1, 17, 17, "hi{-5", TokenType.QuotedString);
            AssertToken(tokens[4], 1, 25, 25, "", TokenType.QuotedString);
        }

        [TestMethod]
        public void TokenizeQuotedStringEscape()
        {
            List<Token> tokens = Tokenizer.Read("\"\\\"\\\\\"");
            
            Assert.AreEqual(1, tokens.Count);
            AssertToken(tokens[0], 1, 0, 0, "\"\\", TokenType.QuotedString);
        }

        [TestMethod]
        public void TokenizeIntegersAndFloats()
        {
            List<Token> tokens = Tokenizer.Read("-5 12.34 -0.123");
            
            Assert.AreEqual(5, tokens.Count);
            AssertToken(tokens[0], 1, 0, 0, "-", TokenType.Minus);
            AssertToken(tokens[1], 1, 1, 1, "5", TokenType.Integer);
            AssertToken(tokens[2], 1, 3, 3, "12.34", TokenType.FloatingPoint);
            AssertToken(tokens[3], 1, 9, 9, "-", TokenType.Minus);
            AssertToken(tokens[4], 1, 10, 10, "0.123", TokenType.FloatingPoint);
        }
        
        [TestMethod]
        public void IgnoresSingleLineComments()
        {
            List<Token> tokens = Tokenizer.Read("// Start\nA//comment\nB //hi");
            
            Assert.AreEqual(2, tokens.Count);
            AssertToken(tokens[0], 2, 0, 9, "A", TokenType.String);
            AssertToken(tokens[1], 3, 0, 20, "B", TokenType.String);
        }
        
        [TestMethod]
        public void IgnoresMultiLineComments()
        {
            List<Token> tokens = Tokenizer.Read("/*\n*\n*/A/**/B/*");
            
            Assert.AreEqual(2, tokens.Count);
            AssertToken(tokens[0], 3, 2, 7, "A", TokenType.String);
            AssertToken(tokens[1], 3, 7, 12, "B", TokenType.String);
        }
        
        [TestMethod]
        public void ExtendedTest()
        {
            const string text = "// Something fun\r\n" +
                                "int main(char c) {\n" +
                                "  /* This is a comment */\n" +
                                "\tRETURN c + 5;\n" +
                                "}\n";
            
            List<Token> tokens = Tokenizer.Read(text);
            
            Assert.AreEqual(13, tokens.Count);
            AssertToken(tokens[0], 2, 0, 18, "int", TokenType.String);
            AssertToken(tokens[1], 2, 4, 22, "main", TokenType.String);
            AssertToken(tokens[2], 2, 8, 26, "(", TokenType.ParenLeft);
            AssertToken(tokens[3], 2, 9, 27, "char", TokenType.String);
            AssertToken(tokens[4], 2, 14, 32, "c", TokenType.String);
            AssertToken(tokens[5], 2, 15, 33, ")", TokenType.ParenRight);
            AssertToken(tokens[6], 2, 17, 35, "{", TokenType.BraceLeft);
            AssertToken(tokens[7], 4, 1, 64, "RETURN", TokenType.String);
            AssertToken(tokens[8], 4, 8, 71, "c", TokenType.String);
            AssertToken(tokens[9], 4, 10, 73, "+", TokenType.Plus);
            AssertToken(tokens[10], 4, 12, 75, "5", TokenType.Integer);
            AssertToken(tokens[11], 4, 13, 76, ";", TokenType.Semicolon);
            AssertToken(tokens[12], 5, 0, 78, "}", TokenType.BraceRight);
        }
        
        [TestMethod]
        [ExpectedException(typeof(ParserException))]
        public void ThrowsIfMissingEndingQuote()
        {
            Tokenizer.Read("\"hello");
        }
        
        [TestMethod]
        [ExpectedException(typeof(ParserException))]
        public void ThrowsIfNumberHasMultiplePeriods()
        {
            Tokenizer.Read("123.1.");
        }
        
        [TestMethod]
        [ExpectedException(typeof(ParserException))]
        public void ThrowsIfNumberHasMultiplePeriodsTogether()
        {
            Tokenizer.Read("123..5");
        }
        
        [TestMethod]
        [ExpectedException(typeof(ParserException))]
        public void ThrowsIfNumberMissingPeriodButNotEnd()
        {
            Tokenizer.Read("123.a");
        }
        
        [TestMethod]
        [ExpectedException(typeof(ParserException))]
        public void ThrowsIfNumberMissingPeriodAtEnd()
        {
            Tokenizer.Read("4.");
        }
        
        [TestMethod]
        [ExpectedException(typeof(ParserException))]
        public void TokenizeQuotedStringEscapeBadNextCharacter()
        {
            Tokenizer.Read("\"\\P\"");
        }
        
        // This just tells us we don't support \t right now. This can be
        // removed when we do end up supporting it (if ever) in the future.
        [TestMethod]
        [ExpectedException(typeof(ParserException))]
        public void TokenizeQuotedStringEscapeBadNextCharacterT()
        {
            Tokenizer.Read("\"\\t\"");
        }
        
        // This just tells us we don't support \n right now. This can be
        // removed when we do end up supporting it (if ever) in the future.
        [TestMethod]
        [ExpectedException(typeof(ParserException))]
        public void TokenizeQuotedStringEscapeBadNextCharacterN()
        {
            Tokenizer.Read("\"\\n\"");
        }
        
        [TestMethod]
        [ExpectedException(typeof(ParserException))]
        public void TokenizeQuotedStringEscapeNoNextCharacter()
        {
            Tokenizer.Read("\"\\");
        }
    }
}