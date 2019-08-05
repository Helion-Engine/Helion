using System;
using Helion.Util.Parser;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helion.Test.Unit.Util.Parser
{
    [TestClass]
    public class ParserTest
    {
        [TestMethod]
        public void InvokesTheParsingStartMethod()
        {
            ParserTestImpl parser = new ParserTestImpl("hello");
            parser.Parse("test");
            Assert.AreEqual(1, parser.PerformParsingInvocations);
        }

        [TestMethod]
        public void CanPeekType()
        {
            ParserTestImpl parser = new ParserTestImpl("hello");
            Assert.IsTrue(parser.Peek(TokenType.String));
            Assert.AreEqual(0, parser.CurrentTokenIndex);
            Assert.IsFalse(parser.Peek(TokenType.Integer));
            Assert.AreEqual(0, parser.CurrentTokenIndex);
            
            parser = new ParserTestImpl("");
            Assert.IsFalse(parser.Peek(TokenType.String));
            Assert.AreEqual(0, parser.CurrentTokenIndex);
        }

        [TestMethod]
        public void CanPeekChar()
        {
            ParserTestImpl parser = new ParserTestImpl("{}");
            Assert.IsTrue(parser.Peek('{'));
            Assert.AreEqual(0, parser.CurrentTokenIndex);
            Assert.IsFalse(parser.Peek('}'));
            Assert.AreEqual(0, parser.CurrentTokenIndex);
            
            parser = new ParserTestImpl("");
            Assert.IsFalse(parser.Peek(':'));
            Assert.AreEqual(0, parser.CurrentTokenIndex);
        }
        
        [TestMethod]
        public void CanPeekString()
        {
            ParserTestImpl parser = new ParserTestImpl("hello yes");
            Assert.IsTrue(parser.Peek("hello"));
            Assert.AreEqual(0, parser.CurrentTokenIndex);
            Assert.IsTrue(parser.Peek("HeLlO"));
            Assert.AreEqual(0, parser.CurrentTokenIndex);
            Assert.IsFalse(parser.Peek("hell"));
            Assert.AreEqual(0, parser.CurrentTokenIndex);
            Assert.IsFalse(parser.Peek(""));
            Assert.AreEqual(0, parser.CurrentTokenIndex);
            
            parser = new ParserTestImpl("\"hello \" yes");
            Assert.IsTrue(parser.Peek("hello "));
            Assert.AreEqual(0, parser.CurrentTokenIndex);
            Assert.IsTrue(parser.Peek("HeLlO "));
            Assert.AreEqual(0, parser.CurrentTokenIndex);
            Assert.IsFalse(parser.Peek("hello"));
            Assert.AreEqual(0, parser.CurrentTokenIndex);
            Assert.IsFalse(parser.Peek(""));
            Assert.AreEqual(0, parser.CurrentTokenIndex);
            
            parser = new ParserTestImpl("");
            Assert.IsFalse(parser.Peek("hello"));
            Assert.AreEqual(0, parser.CurrentTokenIndex);
        }

        [TestMethod]
        public void CanPeekInteger()
        {
            ParserTestImpl parser = new ParserTestImpl("123 number");
            Assert.IsTrue(parser.PeekInteger());
            Assert.AreEqual(0, parser.CurrentTokenIndex);
            
            parser = new ParserTestImpl("-123 number");
            Assert.IsFalse(parser.PeekInteger());
            Assert.AreEqual(0, parser.CurrentTokenIndex);
            
            parser = new ParserTestImpl("number 123");
            Assert.IsFalse(parser.PeekInteger());
            Assert.AreEqual(0, parser.CurrentTokenIndex);
            
            parser = new ParserTestImpl("");
            Assert.IsFalse(parser.PeekInteger());
            Assert.AreEqual(0, parser.CurrentTokenIndex);
        }
        
        [TestMethod]
        public void CanPeekFloat()
        {
            ParserTestImpl parser = new ParserTestImpl("123 number");
            Assert.IsTrue(parser.PeekFloat());
            Assert.AreEqual(0, parser.CurrentTokenIndex);
            
            parser = new ParserTestImpl("123.4 number");
            Assert.IsTrue(parser.PeekFloat());
            Assert.AreEqual(0, parser.CurrentTokenIndex);
            
            parser = new ParserTestImpl("-123.4 number");
            Assert.IsFalse(parser.PeekFloat());
            Assert.AreEqual(0, parser.CurrentTokenIndex);
            
            parser = new ParserTestImpl("0.1238745 number");
            Assert.IsTrue(parser.PeekFloat());
            Assert.AreEqual(0, parser.CurrentTokenIndex);
            
            parser = new ParserTestImpl("- 0.1238745 number");
            Assert.IsFalse(parser.PeekFloat());
            Assert.AreEqual(0, parser.CurrentTokenIndex);
            
            parser = new ParserTestImpl("number 123.4");
            Assert.IsFalse(parser.PeekFloat());
            Assert.AreEqual(0, parser.CurrentTokenIndex);
            
            parser = new ParserTestImpl("");
            Assert.IsFalse(parser.PeekFloat());
            Assert.AreEqual(0, parser.CurrentTokenIndex);
        }
        
        [TestMethod]
        public void CanPeekSignedInteger()
        {
            ParserTestImpl parser = new ParserTestImpl("123 number");
            Assert.IsTrue(parser.PeekSignedInteger());
            Assert.AreEqual(0, parser.CurrentTokenIndex);
            
            parser = new ParserTestImpl("- 123");
            Assert.IsTrue(parser.PeekSignedInteger());
            Assert.AreEqual(0, parser.CurrentTokenIndex);
            
            parser = new ParserTestImpl("-123 number");
            Assert.IsTrue(parser.PeekSignedInteger());
            Assert.AreEqual(0, parser.CurrentTokenIndex);
            
            parser = new ParserTestImpl("number 123");
            Assert.IsFalse(parser.PeekSignedInteger());
            Assert.AreEqual(0, parser.CurrentTokenIndex);
            
            parser = new ParserTestImpl("");
            Assert.IsFalse(parser.PeekSignedInteger());
            Assert.AreEqual(0, parser.CurrentTokenIndex);
        }

        [TestMethod]
        public void CanPeekSignedFloat()
        {
            ParserTestImpl parser = new ParserTestImpl("123 number");
            Assert.IsTrue(parser.PeekSignedFloat());
            Assert.AreEqual(0, parser.CurrentTokenIndex);
            
            parser = new ParserTestImpl("-123 number");
            Assert.IsTrue(parser.PeekSignedFloat());
            Assert.AreEqual(0, parser.CurrentTokenIndex);

            parser = new ParserTestImpl("-123.4 number");
            Assert.IsTrue(parser.PeekSignedFloat());
            Assert.AreEqual(0, parser.CurrentTokenIndex);
            
            parser = new ParserTestImpl("123.4 number");
            Assert.IsTrue(parser.PeekSignedFloat());
            Assert.AreEqual(0, parser.CurrentTokenIndex);
            
            parser = new ParserTestImpl("0.1238745 number");
            Assert.IsTrue(parser.PeekSignedFloat());
            Assert.AreEqual(0, parser.CurrentTokenIndex);
            
            parser = new ParserTestImpl("-0.1238745 number");
            Assert.IsTrue(parser.PeekSignedFloat());
            Assert.AreEqual(0, parser.CurrentTokenIndex);
            
            parser = new ParserTestImpl("number 123.4");
            Assert.IsFalse(parser.PeekSignedFloat());
            Assert.AreEqual(0, parser.CurrentTokenIndex);
            
            parser = new ParserTestImpl("");
            Assert.IsFalse(parser.PeekSignedFloat());
            Assert.AreEqual(0, parser.CurrentTokenIndex);
        }
        
        [TestMethod]
        public void CanPeekAnyString()
        {
            ParserTestImpl parser = new ParserTestImpl("hello yes");
            Assert.IsTrue(parser.PeekString());
            Assert.AreEqual(0, parser.CurrentTokenIndex);
            
            parser = new ParserTestImpl("\"hello\"");
            Assert.IsTrue(parser.PeekString());
            Assert.AreEqual(0, parser.CurrentTokenIndex);
            
            parser = new ParserTestImpl("; yes");
            Assert.IsFalse(parser.PeekString());
            Assert.AreEqual(0, parser.CurrentTokenIndex);
            
            parser = new ParserTestImpl("");
            Assert.IsFalse(parser.PeekString());
            Assert.AreEqual(0, parser.CurrentTokenIndex);
        }

        [TestMethod]
        public void CanConsumeToken()
        {
            ParserTestImpl parser = new ParserTestImpl("hello ;");
            parser.Consume(TokenType.String);
            Assert.AreEqual(1, parser.CurrentTokenIndex);
            parser.Consume(TokenType.Semicolon);
            Assert.AreEqual(2, parser.CurrentTokenIndex);
            Assert.IsTrue(parser.Done);
        }
        
        [TestMethod]
        [ExpectedException(typeof(ParserException))]
        public void ConsumeWrongTypeThrows()
        {
            ParserTestImpl parser = new ParserTestImpl("hello ;");
            parser.Consume(TokenType.Colon);
        }
        
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ConsumeTypePastEndThrows()
        {
            ParserTestImpl parser = new ParserTestImpl("hello ;");
            parser.Consume(TokenType.String);
            parser.Consume(TokenType.Semicolon);
            parser.Consume(TokenType.Semicolon);
        }
        
        [TestMethod]
        public void CanConsumeChar()
        {
            ParserTestImpl parser = new ParserTestImpl(";-");
            parser.Consume(';');
            Assert.AreEqual(1, parser.CurrentTokenIndex);
            parser.Consume('-');
            Assert.AreEqual(2, parser.CurrentTokenIndex);
            Assert.IsTrue(parser.Done);
        }
        
        [TestMethod]
        [ExpectedException(typeof(ParserException))]
        public void ConsumeWrongCharThrows()
        {
            ParserTestImpl parser = new ParserTestImpl(";-");
            parser.Consume('+');
        }
        
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ConsumeCharPastEndThrows()
        {
            ParserTestImpl parser = new ParserTestImpl("; -");
            parser.Consume(';');
            parser.Consume('-');
            parser.Consume('+');
        }
        
        [TestMethod]
        public void CanConsumeInteger()
        {
            ParserTestImpl parser = new ParserTestImpl("123 01");
            Assert.AreEqual(123, parser.ConsumeInteger());
            Assert.AreEqual(1, parser.CurrentTokenIndex);
            Assert.AreEqual(1, parser.ConsumeInteger());
            Assert.AreEqual(2, parser.CurrentTokenIndex);
            Assert.IsTrue(parser.Done);
        }
        
        [TestMethod]
        [ExpectedException(typeof(ParserException))]
        public void ConsumeWrongIntegerThrows()
        {
            ParserTestImpl parser = new ParserTestImpl(";-");
            parser.ConsumeInteger();
        }
        
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ConsumeIntegerPastEndThrows()
        {
            ParserTestImpl parser = new ParserTestImpl("123");
            parser.ConsumeInteger();
            parser.ConsumeInteger();
        }
        
        [TestMethod]
        public void CanConsumeSignedInteger()
        {
            ParserTestImpl parser = new ParserTestImpl("123 -5123 0 -0");
            Assert.AreEqual(123, parser.ConsumeSignedInteger());
            Assert.AreEqual(1, parser.CurrentTokenIndex);
            Assert.AreEqual(-5123, parser.ConsumeSignedInteger());
            Assert.AreEqual(3, parser.CurrentTokenIndex);
            Assert.AreEqual(0, parser.ConsumeSignedInteger());
            Assert.AreEqual(4, parser.CurrentTokenIndex);
            Assert.AreEqual(0, parser.ConsumeSignedInteger());
            Assert.AreEqual(6, parser.CurrentTokenIndex);
            Assert.IsTrue(parser.Done);
        }
        
        [TestMethod]
        [ExpectedException(typeof(ParserException))]
        public void ConsumeWrongSignedIntegerThrows()
        {
            ParserTestImpl parser = new ParserTestImpl(";-");
            parser.ConsumeSignedInteger();
        }
        
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ConsumeSignedIntegerPastEndThrows()
        {
            ParserTestImpl parser = new ParserTestImpl("-123");
            parser.ConsumeSignedInteger();
            parser.ConsumeSignedInteger();
        }
        
        [TestMethod]
        public void CanConsumeFloat()
        {
            ParserTestImpl parser = new ParserTestImpl("123.45 123 0.1234");
            Assert.AreEqual(123.45, parser.ConsumeFloat());
            Assert.AreEqual(1, parser.CurrentTokenIndex);
            Assert.AreEqual(123, parser.ConsumeFloat());
            Assert.AreEqual(2, parser.CurrentTokenIndex);
            Assert.AreEqual(0.1234, parser.ConsumeFloat());
            Assert.AreEqual(3, parser.CurrentTokenIndex);
            Assert.IsTrue(parser.Done);
        }
        
        [TestMethod]
        [ExpectedException(typeof(ParserException))]
        public void ConsumeWrongFloatThrows()
        {
            ParserTestImpl parser = new ParserTestImpl(";-");
            parser.ConsumeFloat();
        }
        
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ConsumeFloatPastEndThrows()
        {
            ParserTestImpl parser = new ParserTestImpl("123.456");
            parser.ConsumeFloat();
            parser.ConsumeFloat();
        }
        
        [TestMethod]
        public void CanConsumeSignedFloat()
        {
            ParserTestImpl parser = new ParserTestImpl("-123.45 123 -17 -0.1234 0.0");
            Assert.AreEqual(-123.45, parser.ConsumeSignedFloat());
            Assert.AreEqual(2, parser.CurrentTokenIndex);
            Assert.AreEqual(123.0, parser.ConsumeSignedFloat());
            Assert.AreEqual(3, parser.CurrentTokenIndex);
            Assert.AreEqual(-17.0, parser.ConsumeSignedFloat());
            Assert.AreEqual(5, parser.CurrentTokenIndex);
            Assert.AreEqual(-0.1234, parser.ConsumeSignedFloat());
            Assert.AreEqual(7, parser.CurrentTokenIndex);
            Assert.AreEqual(0.0, parser.ConsumeSignedFloat());
            Assert.AreEqual(8, parser.CurrentTokenIndex);
            Assert.IsTrue(parser.Done);
        }
        
        [TestMethod]
        [ExpectedException(typeof(ParserException))]
        public void ConsumeWrongSignedFloatThrows()
        {
            ParserTestImpl parser = new ParserTestImpl(";-");
            parser.ConsumeSignedFloat();
        }
        
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ConsumeSignedFloatPastEndThrows()
        {
            ParserTestImpl parser = new ParserTestImpl("-123.456");
            parser.ConsumeSignedFloat();
            parser.ConsumeSignedFloat();
        }
        
        [TestMethod]
        public void CanConsumeString()
        {
            ParserTestImpl parser = new ParserTestImpl("hi \"yes\" \"\"");
            Assert.AreEqual("hi", parser.ConsumeString());
            Assert.AreEqual(1, parser.CurrentTokenIndex);
            Assert.AreEqual("yes", parser.ConsumeString());
            Assert.AreEqual(2, parser.CurrentTokenIndex);
            Assert.AreEqual("", parser.ConsumeString());
            Assert.AreEqual(3, parser.CurrentTokenIndex);
        }
        
        [TestMethod]
        [ExpectedException(typeof(ParserException))]
        public void ConsumeWrongStringThrows()
        {
            ParserTestImpl parser = new ParserTestImpl(";-");
            parser.ConsumeString();
        }
        
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ConsumeStringPastEndThrows()
        {
            ParserTestImpl parser = new ParserTestImpl("");
            parser.ConsumeString();
        }
        
        [TestMethod]
        public void CanConsumeStringText()
        {
            ParserTestImpl parser = new ParserTestImpl("hi \"yes\" \"\"");
            parser.ConsumeString("hi");
            Assert.AreEqual(1, parser.CurrentTokenIndex);
            parser.ConsumeString("yes");
            Assert.AreEqual(2, parser.CurrentTokenIndex);
            parser.ConsumeString("");
            Assert.AreEqual(3, parser.CurrentTokenIndex);
        }
        
        [TestMethod]
        [ExpectedException(typeof(ParserException))]
        public void ConsumeWrongStringTextThrows()
        {
            ParserTestImpl parser = new ParserTestImpl(";-");
            parser.ConsumeString("stuff");
        }
        
        [TestMethod]
        [ExpectedException(typeof(ArgumentOutOfRangeException))]
        public void ConsumeStringTextPastEndThrows()
        {
            ParserTestImpl parser = new ParserTestImpl("");
            parser.ConsumeString("stuff");
        }
    }

    // Allow us access to the internals to manipulate them. This makes it sort
    // of a black box and white box test at the same time.
    public class ParserTestImpl : ParserBase
    {
        public int PerformParsingInvocations;
        public new int CurrentTokenIndex => base.CurrentTokenIndex;
        public new bool Done => base.Done;

        public ParserTestImpl(string text)
        {
            Tokens = Tokenizer.Read(text);
        }

        public new bool Peek(TokenType type) => base.Peek(type);
        public new bool Peek(char c) => base.Peek(c);
        public new bool Peek(string str) => base.Peek(str);
        public new bool PeekInteger() => base.PeekInteger();
        public new bool PeekFloat() => base.PeekFloat();
        public new bool PeekSignedInteger() => base.PeekSignedInteger();
        public new bool PeekSignedFloat() => base.PeekSignedFloat();
        public new bool PeekString() => base.PeekString();
        public new void Consume(TokenType type) => base.Consume(type);
        public new void Consume(char c) => base.Consume(c);
        public new int ConsumeInteger() => base.ConsumeInteger();
        public new int ConsumeSignedInteger() => base.ConsumeSignedInteger();
        public new double ConsumeFloat() => base.ConsumeFloat();
        public new double ConsumeSignedFloat() => base.ConsumeSignedFloat();
        public new string ConsumeString() => base.ConsumeString();
        public new void ConsumeString(string str) => base.ConsumeString(str);
        
        protected override void PerformParsing()
        {
            PerformParsingInvocations++;
        }
    }
}