﻿using FluentAssertions;
using Helion.Strings;
using Xunit;

namespace Helion.Tests.Unit.Strings;

public class StringBufferTests
{
    [Fact(DisplayName = "Get buffer strings")]
    public void GetString()
    {
        string str = StringBuffer.GetString(16);
        str.Length.Should().BeGreaterThanOrEqualTo(16);
        StringBuffer.StringLength(str).Should().Be(0);

        str = StringBuffer.GetString(80);
        str.Length.Should().BeGreaterThanOrEqualTo(80);
        StringBuffer.StringLength(str).Should().Be(0);

        str = StringBuffer.GetString(128);
        str.Length.Should().BeGreaterThanOrEqualTo(128);
        StringBuffer.StringLength(str).Should().Be(0);
    }

    [Fact(DisplayName = "Append chars")]
    public void AppendChar()
    {
        string str = StringBuffer.GetString(16);
        int length = str.Length * 2;
        for (int i = 0; i < length; i++)
            str = StringBuffer.Append(str, (i % 10).ToString()[0]);

        for (int i = 0; i < length; i++)
            str[i].Should().Be((i % 10).ToString()[0]);
    }

    [Fact(DisplayName = "ToStringExact")]
    public void ToStringExact()
    {
        string str = StringBuffer.GetString(16);
        str = StringBuffer.Append(str, "test");

        string exact = StringBuffer.ToStringExact(str);
        exact.Should().Be("test");

        str = StringBuffer.Append(str, " test2");
        exact = StringBuffer.ToStringExact(str);
        exact.Should().Be("test test2");
    }

    [Fact(DisplayName = "Clear string")]
    public void Clear()
    {
        string str = StringBuffer.GetString(16);
        str = StringBuffer.Set(str, "test1234");

        CompareString(str, "test1234");
        StringBuffer.Clear(str);
        CompareString(str, "");
    }

    [Fact(DisplayName = "Free string")]
    public void Free()
    {
        // Make sure to start clean with this test
        StringBuffer.ClearStringCache();

        string first = StringBuffer.GetString(16);
        first = StringBuffer.Set(first, "first");

        string second = StringBuffer.GetString(16);
        second = StringBuffer.Set(second, "second");

        CompareString(first, "first");
        CompareString(second, "second");

        StringBuffer.FreeString(first);

        string third = StringBuffer.GetString(16);
        ReferenceEquals(first, third).Should().BeTrue();

        StringBuffer.FreeString(third);

        // This string request is too large so a new string should be allocated
        third = StringBuffer.GetString(1024);
        ReferenceEquals(first, third).Should().BeFalse();

        third = StringBuffer.GetString(16);
        ReferenceEquals(first, third).Should().BeTrue();
    }

    [Fact(DisplayName = "AsSpan")]
    public void AsSpan()
    {
        string str = StringBuffer.GetString();
        str = StringBuffer.Append(str, "testing");
        var span = StringBuffer.AsSpan(str);
        span.Length.Should().Be(7);
        for (int i = 0; i < span.Length; i++)
            span[i].Should().Be(str[i]);
    }

    private static void CompareString(string stringBuffer, string str)
    {
        int length = StringBuffer.StringLength(stringBuffer);
        length.Should().Be(str.Length);

        stringBuffer.Substring(0, length).Should().Be(str);
    }
}
