using Helion.Util.Extensions;
using Helion.Util.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Unit.Util.Parser
{
    [TestClass]
    public class TokenTest
    {
        [TestMethod]
        public void ConstructFromAny()
        {
            const int lineNumber = 4;
            const int lineCharOffset = 3;
            const int charOffset = 84;
            string text = "hello";
            Token token = new Token(lineNumber, lineCharOffset, charOffset, text, TokenType.String);
            
            Assert.AreEqual(lineNumber, token.LineNumber);
            Assert.AreEqual(lineCharOffset, token.LineCharOffset);
            Assert.AreEqual(charOffset, token.CharOffset);
            Assert.AreEqual(text, token.Text);
            Assert.AreEqual(TokenType.String, token.Type);
        }
        
        [TestMethod]
        public void ConstructFromSymbol()
        {
            const int lineNumber = 1;
            const int lineCharOffset = 211;
            const int charOffset = 52878;
            Token token = new Token(lineNumber, lineCharOffset, charOffset, ';');
            
            Assert.AreEqual(lineNumber, token.LineNumber);
            Assert.AreEqual(lineCharOffset, token.LineCharOffset);
            Assert.AreEqual(charOffset, token.CharOffset);
            Assert.AreEqual(";", token.Text);
            Assert.AreEqual(TokenType.Semicolon, token.Type);
        }
        
        [TestMethod]
        public void ConstructEmptyQuotedString()
        {
            const int lineNumber = 1;
            const int lineCharOffset = 0;
            const int charOffset = 0;
            Token token = new Token(lineNumber, lineCharOffset, charOffset, "", TokenType.QuotedString);
            
            Assert.AreEqual(lineNumber, token.LineNumber);
            Assert.AreEqual(lineCharOffset, token.LineCharOffset);
            Assert.AreEqual(charOffset, token.CharOffset);
            Assert.IsTrue(token.Text.Empty());
            Assert.AreEqual(TokenType.QuotedString, token.Type);
        }
    }
}