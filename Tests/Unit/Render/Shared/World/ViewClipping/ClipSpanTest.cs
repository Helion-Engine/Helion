using FluentAssertions;
using Helion.Render.OpenGL.Legacy.Shared.World.ViewClipping;
using Xunit;

namespace Helion.Tests.Unit.Render.Shared.World.ViewClipping
{
    public class ClipSpanTest
    {
        private const uint Start = 123984;
        private const uint End = 384728799;

        [Theory(DisplayName = "Range contains a single value")]
        [InlineData(Start, true)]
        [InlineData(End, true)]
        [InlineData((Start + End) / 2, true)]
        [InlineData(Start - 1, false)]
        [InlineData(End + 1, false)]
        [InlineData(0, false)]
        [InlineData(uint.MaxValue, false)]
        public void ContainsSingleValue(uint value, bool isContained)
        {
            new ClipSpan(Start, End).Contains(value).Should().Be(isContained);
        }
        
        [Theory(DisplayName = "Range contains a range")]
        [InlineData(Start, End, true)]
        [InlineData(Start, Start, true)]
        [InlineData(End, End, true)]
        [InlineData(Start + 1, End - 1, true)]
        [InlineData((Start + End) / 2, (Start + End) / 2, true)]
        [InlineData(Start - 1, End, false)]
        [InlineData(Start, End + 1, false)]
        [InlineData(Start - 1, End + 1, false)]
        [InlineData(0, uint.MaxValue, false)]
        public void ContainsRange(uint start, uint end, bool isContained)
        {
            new ClipSpan(Start, End).Contains(start, end).Should().Be(isContained);
        }
    }
}
