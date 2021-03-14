using FluentAssertions;
using Helion.Util.Extensions;
using Xunit;

namespace Helion.Tests.Unit.Util.Extensions
{
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
    }
}
