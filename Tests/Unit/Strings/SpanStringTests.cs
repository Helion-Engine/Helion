using FluentAssertions;
using Helion.Strings;
using System;
using Xunit;

namespace Helion.Tests.Unit.Strings;

public class SpanStringTests
{
    [Fact(DisplayName = "Append strings")]
    public void AppendString()
    {
        SpanString str = new();
        int length = str.Capacity * 2;
        for (int i = 0; i < length; i++)
            str.Append((i % 10).ToString());

        var span = str.AsSpan();
        for (int i = 0; i < length; i++)
            span[i].Should().Be((i % 10).ToString()[0]);
    }


    [Fact(DisplayName = "Append numbers")]
    public void AppendNumber()
    {
        SpanString str = new();
        str.Length.Should().Be(0);
        str.Append(420);
        CompareString(str, "420");
        str.Append(69);
        CompareString(str, "42069");
    }

    [Fact(DisplayName = "Append zero")]
    public void AppendZero()
    {
        SpanString str = new();
        for (int i = 0; i < 8; i++)
            str.Append(0);
        CompareString(str, "00000000");
    }

    [Fact(DisplayName = "Append negative number")]
    public void AppendNegativeNumber()
    {
        SpanString str = new();
        str.Append(-1);
        CompareString(str, "-1");

        str.Append(-9876);
        CompareString(str, "-1-9876");
    }

    [Fact(DisplayName = "Pad numbers")]
    public void PadNumber()
    {
        SpanString str = new();
        str.Append(0, 2);
        CompareString(str, "00");

        str.Append(':');
        str.Append(123, 6);
        CompareString(str, "00:000123");
    }

    [Fact(DisplayName = "Pad number set pad char")]
    public void PadNumberSetPadChar()
    {
        SpanString str = new();
        str.Append(0, 2, 'x');
        CompareString(str, "x0");

        str.Append(':');
        str.Append(123, 6, 'x');
        CompareString(str, "x0:xxx123");
    }

    private static void CompareString(SpanString spanString, string str)
    {        
        spanString.Length.Should().Be(str.Length);
        var span = spanString.AsSpan();
        for (int i = 0; i < span.Length; i++)
            span[i].Should().Be(str[i]);
    }
}
