using FluentAssertions;
using Helion.Util.Extensions;
using System.Text;
using Xunit;

namespace Helion.Tests.Unit.Util.Extensions;

public class StringExtensionsTest
{
    [Fact(DisplayName = "Check if a string is empty")]
    public void CheckEmptyLinkedList()
    {
        "".Empty().Should().BeTrue();
        "hi".Empty().Should().BeFalse();
    }

    [Theory(DisplayName = "MD5 strings are classified properly")]
    [InlineData("", false)]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", true)]
    [InlineData("ABCDEFabcdef123456789012398a7adc", true)]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", false)]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaag", false)]
    [InlineData("aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa", false)]
    public void MD5StringVerification(string str, bool isMD5)
    {
        str.IsMD5().Should().Be(isMD5);
    }

    [Theory(DisplayName = "String with requested spaces are inserted correctly")]
    [InlineData("", "")]
    [InlineData("abc", "abc")]
    [InlineData("Abc", "Abc")]
    [InlineData("qweF", "qwe F")]
    [InlineData("CommandSlot1", "Command Slot 1")]
    [InlineData("CenterView", "Center View")]
    [InlineData("Center View", "Center View")]
    public void TestWithSpaces(string input, string expected)
    {
        input.WithWordSpaces(new StringBuilder()).Should().Be(expected);
    }
}
