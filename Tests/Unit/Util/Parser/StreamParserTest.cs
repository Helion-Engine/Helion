using FluentAssertions;
using Helion.Util.Container;
using Helion.Util.Parser;
using System.Collections.Frozen;
using System.IO;
using Xunit;

namespace Helion.Tests.Unit.Util.Parser;

public class StreamParserTest
{
    [Fact]
    public void ParseSingleLine()
    {
        var str = @"/block1;{test/test;}";
        var parser = CreateParser(str);
        parser.ConsumeString().Should().Be("/block1");
        parser.Peek(';').Should().BeTrue();
        parser.Peek('{').Should().BeFalse();
        parser.Consume(';');
        parser.Peek('{').Should().BeTrue();
        parser.ConsumeString().Should().Be("{");
        parser.ConsumeString().Should().Be("test/test");
        parser.ConsumeString().Should().Be(";");
        parser.ConsumeString("}");
        parser.IsDone().Should().BeTrue();
    }

    [Fact]
    public void ParseWithNewLines()
    {
        var str = @"
    block1 // comment 
    {
        test1=""value 1"";
        test2 = ""value 2"";
        something(test3);
        test4 = 420.69;
    /*
    multi-line
    comment
    */
    }";
        var array = new DynamicArray<char>();
        var parser = CreateParser(str);
        parser.ConsumeStringSpan(array).ToString().Should().Be("block1");
        parser.Peek('{').Should().BeTrue();
        parser.Consume('{');
        parser.ConsumeString().Should().Be("test1");
        parser.ConsumeString().Should().Be("=");
        parser.ConsumeString().Should().Be("value 1");
        parser.ConsumeString().Should().Be(";");
        parser.ConsumeString().Should().Be("test2");
        parser.ConsumeString().Should().Be("=");
        parser.ConsumeString().Should().Be("value 2");
        parser.ConsumeString().Should().Be(";");
        parser.ConsumeString().Should().Be("something");
        parser.ConsumeString().Should().Be("(");
        parser.ConsumeString().Should().Be("test3");
        parser.ConsumeString().Should().Be(")");
        parser.ConsumeString().Should().Be(";");
        parser.ConsumeString("test4");
        parser.Consume('=');
        parser.ConsumeDouble().Should().Be(420.69);
        parser.Consume(';');
        parser.ConsumeString().Should().Be("}");

        var throws = false;
        try
        {
            parser.ConsumeString();
        }
        catch
        {
            throws = true;
        }

        throws.Should().BeTrue();
        parser.IsDone().Should().BeTrue();
    }

    private static StreamParser CreateParser(string str)
    {
        char[] ParseChars = [';', '=', ')', '(', '{', '}'];
        var ParseCharSet = ParseChars.ToFrozenSet();
        var ms = new MemoryStream(System.Text.Encoding.UTF8.GetBytes(str));
        return new StreamParser(ms, ParseCharSet);
    }
}
