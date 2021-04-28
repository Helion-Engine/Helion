using FluentAssertions;
using Helion.Util.Parser;
using MoreLinq;
using Xunit;

namespace Helion.Tests.Unit.Util.Parser
{
    public class SimpleParserTest
    {
        [Fact(DisplayName = "Gets the current line")]
        public void GetCurrentLineTest()
        {
            const string data = @"Line1
Line2";
            SimpleParser parser = new();
            parser.Parse(data);

            parser.GetCurrentLine().Should().Be(0);
            parser.ConsumeString();
            parser.GetCurrentLine().Should().Be(1);
        }

        [Fact(DisplayName = "Check if done")]
        public void IsDoneTest()
        {
            const string data = @"Line1
Line2";
            SimpleParser parser = new();
            parser.Parse(data);

            parser.IsDone().Should().BeFalse();
            parser.ConsumeString();
            parser.IsDone().Should().BeFalse();
            parser.ConsumeString();
            parser.IsDone().Should().BeTrue();
        }

        [Fact(DisplayName = "Can peek")]
        public void PeekTest()
        {
            const string data = @"Line1
Line2";
            SimpleParser parser = new();
            parser.Parse(data);

            parser.Peek('L').Should().BeTrue();
            parser.Peek('l').Should().BeTrue();
            
            parser.Peek("Line1").Should().BeTrue();
            parser.Peek("Line1").Should().BeTrue();
        }

        [Fact(DisplayName = "Consumes strings")]
        public void ConsumeStringTest()
        {
            const string data = @"Line1 Data1
Line2 Data2";
            SimpleParser parser = new();
            parser.Parse(data);

            parser.ConsumeString().Should().Be("Line1");
            parser.ConsumeString().Should().Be("Data1");
            parser.GetCurrentLine().Should().Be(1);
            parser.ConsumeString().Should().Be("Line2");
            parser.ConsumeString().Should().Be("Data2");
        }

        [Fact(DisplayName = "Consumes strings with equality")]
        public void ConsumeStringEqualTest()
        {
            const string data = @"Line1 Data1
Line2 Data2";
            SimpleParser parser = new();
            parser.Parse(data);

            parser.ConsumeString("Line1");
            parser.ConsumeString("data1");
            parser.ConsumeString("line2");
            parser.ConsumeString("data2");
        }

        [Fact(DisplayName = "Consumes integers")]
        public void ConsumeIntegerTest()
        {
            const string data = "1 2 3 69";
            SimpleParser parser = new();
            parser.Parse(data);

            parser.ConsumeInteger().Should().Be(1);
            parser.ConsumeInteger().Should().Be(2);
            parser.ConsumeInteger().Should().Be(3);
            parser.ConsumeInteger().Should().Be(69);
        }

        [Fact(DisplayName = "Consumes booleans")]
        public void ConsumeBoolTest()
        {
            const string data = "true false True False";
            SimpleParser parser = new();
            parser.Parse(data);

            parser.ConsumeBool().Should().BeTrue();
            parser.ConsumeBool().Should().BeFalse();
            parser.ConsumeBool().Should().BeTrue();
            parser.ConsumeBool().Should().BeFalse();
        }

        [Fact(DisplayName = "Consumes characters")]
        public void ConsumeCharTest()
        {
            const string data = "t e s t test";
            SimpleParser parser = new();
            parser.Parse(data);

            "TeSt".ForEach(c => parser.Consume(c));

            bool success = false;
            try
            {
                parser.Consume('T');
            }
            catch (ParserException)
            {
                success = true;
            }

            success.Should().BeTrue();
        }

        [Fact(DisplayName = "Consumes lines")]
        public void ConsumeLineTest()
        {
            const string data = @"Line1 Data1
                Line2 Data2 Data22";
            SimpleParser parser = new();
            parser.Parse(data);

            parser.GetCurrentLine().Should().Be(0);
            parser.ConsumeLine().Should().Be("Line1 Data1");
            parser.GetCurrentLine().Should().Be(1);
            parser.ConsumeString().Should().Be("Line2");
            parser.ConsumeLine().Should().Be("Data2 Data22");
        }

        [Fact(DisplayName = "Consume single line comment on its own line")]
        public void SingleLineCommentTest()
        {
             const string data = @"Line1 Data1
                 //This is a comment
                 Line2 Data2";
             SimpleParser parser = new();
             parser.Parse(data);

             foreach (string s in new[] { "Line1", "Data1", "Line2", "Data2" })
                 parser.ConsumeString().Should().Be(s);
        }

        [Fact(DisplayName = "Consume single line comment at the end of a line")]
        public void SingleLineCommentMiddleTest()
        {
            const string data = @"Line1 //This is a comment Data1
                Line2 Data2";
            SimpleParser parser = new();
            parser.Parse(data);

            foreach (string s in new[] { "Line1", "Line2", "Data2" })
                parser.ConsumeString().Should().Be(s);
        }

        [Fact(DisplayName = "Consume multiline comment as a single line")]
        public void CommentMultiLineTest()
        {
            const string data = @"Line1 Data1
                /* This is a comment */
                Line2 Data2";
            SimpleParser parser = new();
            parser.Parse(data);

            foreach (string s in new[] { "Line1", "Data1", "Line2", "Data2" })
                parser.ConsumeString().Should().Be(s);
        }

        [Fact(DisplayName = "Consume multiline comment over multiple lines")]
        public void CommentMultiLineTest2()
        {
            const string data = @"Line1 Data1
                /* This is a comment 
                    that spans
                    multiple lines */
                Line2 Data2";
            SimpleParser parser = new();
            parser.Parse(data);

            foreach (string s in new[] { "Line1", "Data1", "Line2", "Data2" })
                parser.ConsumeString().Should().Be(s);
        }

        [Fact(DisplayName = "Consume multiline comment with all asterisks")]
        public void CommentMultiLineTest3()
        {
            const string data = @"Line1 Data1
                /*******/
                Line2 Data2";
            SimpleParser parser = new();
            parser.Parse(data);

            foreach (string s in new[] { "Line1", "Data1", "Line2", "Data2" })
                parser.ConsumeString().Should().Be(s);
        }

        [Fact(DisplayName = "Consume multiline in between valid lines")]
        public void CommentMultiLineMiddle()
        {
            const string data = @"Line1 /*lol comment*/ Data1";
            SimpleParser parser = new();
            parser.Parse(data);

            foreach (string s in new[] { "Line1", "Data1" })
                parser.ConsumeString().Should().Be(s);
        }

        [Fact(DisplayName = "Consume string with quotes")]
        public void TestQuote()
        {
            const string data = @"Line1 ""data with spaces"" Data1";
            SimpleParser parser = new();
            parser.Parse(data);

            foreach (string s in new[] { "Line1", "data with spaces", "Data1" })
                parser.ConsumeString().Should().Be(s);
        }


        [Fact(DisplayName = "Consume string with start and end quotes")]
        public void TestQuote2()
        {
            const string data = @"""data with spaces""";
            SimpleParser parser = new();
            parser.Parse(data);

            parser.ConsumeString().Should().Be("data with spaces");
        }

        [Fact(DisplayName = "Consume string with multiple quoted strings")]
        public void TestQuote3()
        {
            const string data = "\"data 1\"\"data 2\"";
            SimpleParser parser = new();
            parser.Parse(data);

            parser.ConsumeString().Should().Be("data 1");
            parser.ConsumeString().Should().Be("data 2");
        }

        [Fact(DisplayName = "Consume string with multiple quoted integers")]
        public void TestQuote4()
        {
            const string data = "Thing = \"1 2\" \"3 4\"";
            SimpleParser parser = new SimpleParser();
            parser.Parse(data);

            foreach (string s in new[] { "Thing", "=", "1 2", "3 4" })
                parser.ConsumeString().Should().Be(s);
        }

        [Fact(DisplayName = "Consume special tokens with strings and integers")]
        public void TestSpecial()
        {
            const string data = @"item{thing=1}";
            SimpleParser parser = new();
            parser.Parse(data);

            foreach (string s in new[] { "item", "{", "thing", "=", "1", "}" })
                parser.ConsumeString().Should().Be(s);
        }

        [Fact(DisplayName = "Consume special tokens with strings and integers over multiple lines")]
        public void TestBrace2()
        {
            const string data = @"item
            {
                thing = 1
            }";
            SimpleParser parser = new();
            parser.Parse(data);

            foreach (string s in new[] { "item", "{", "thing", "=", "1", "}" })
                parser.ConsumeString().Should().Be(s);
        }
        
        [Fact(DisplayName = "Consume multiline string if not terminated with quotes in the same line")]
        public void TestMultilineString()
        {
            const string data = "YES1,\"Did you like\nthe new\nssg?\"";
            SimpleParser parser = new();
            parser.Parse(data);

            parser.ConsumeString().Should().Be("YES1");
            parser.ConsumeString().Should().Be(",");
            parser.ConsumeString().Should().Be("Did you like\nthe new\nssg?");
        }

        [Fact(DisplayName = "Consume multiline string if not terminated with quotes in the same line with semicolon")]
        public void TestMultilineStringWithSemicolon()
        {
            string data = "QUITMSG = \"Are you sure you want to\nquit this great game?\";";
            SimpleParser parser = new();
            parser.Parse(data);

            parser.ConsumeString().Should().Be("QUITMSG");
            parser.ConsumeString().Should().Be("=");
            parser.ConsumeString().Should().Be("Are you sure you want to\nquit this great game?");
            parser.ConsumeString().Should().Be(";");
            parser.IsDone().Should().Be(true);
        }
    }
}
