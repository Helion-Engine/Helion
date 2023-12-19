using FluentAssertions;
using Helion.Util;
using Xunit;

namespace Helion.Tests.Unit.Util.Parser
{
    public class DoubleParsing
    {
        [Fact(DisplayName = "TryParseDouble")]
        public void TryParseDouble()
        {
            Parsing.TryParseDouble("420.69", out var d).Should().BeTrue();
            d.Should().Be(420.69);
        }

        [Fact(DisplayName = "TryParseDouble int")]
        public void TryParseDoubleInt()
        {
            Parsing.TryParseDouble("420", out var d).Should().BeTrue();
            d.Should().Be(420);
        }

        [Fact(DisplayName = "TryParseDouble ending comma")]
        public void TryParseDoubleEndingComma()
        {
            Parsing.TryParseDouble("420.", out var d).Should().BeTrue();
            d.Should().Be(420);
        }

        [Fact(DisplayName = "TryParseDouble with comma")]
        public void TryParseDoubleComma()
        {
            // Only support period as decimal separator. Commas are replaced if user enters in non en-US format
            Parsing.TryParseDouble("420,69", out var d).Should().BeTrue();
            d.Should().Be(420.69);
        }

        [Fact(DisplayName = "TryParseDouble with comma")]
        public void TryParseDoubleBadFormat()
        {
            // Not supporting comma and period
            Parsing.TryParseDouble("420,690.69", out var d).Should().BeFalse();
        }

        [Fact(DisplayName = "TryParseDouble failure")]
        public void TryParseDoubleFailure()
        {
            Parsing.TryParseDouble("420lol", out var d).Should().BeFalse();
        }
    }
}
