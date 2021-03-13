using FluentAssertions;
using Helion.Util.Container;
using Xunit;

namespace Helion.Tests.Unit.Util.Container
{
    public class AvailableIndexTrackerTest
    {
        [Fact]
        public void GetIndices()
        {
            AvailableIndexTracker tracker = new();

            for (int i = 0; i < 100; i++)
                tracker.Next().Should().Be(i);
        }
        
        [Fact]
        public void CheckIfTracked()
        {
            AvailableIndexTracker tracker = new();
            
            for (int i = 0; i < 10; i++)
                tracker.Next();

            tracker.IsTracked(-1).Should().BeFalse();
            tracker.IsTracked(10).Should().BeFalse();
            tracker.IsTracked(12345).Should().BeFalse();
            for (int i = 0; i < 10; i++)
                tracker.IsTracked(i).Should().BeTrue();
        }
        
        [Fact]
        public void MakeIndexAvailable()
        {
            AvailableIndexTracker tracker = new();
            
            for (int i = 0; i < 10; i++)
                tracker.Next();

            tracker.MakeAvailable(4);
            tracker.MakeAvailable(6);
            tracker.IsTracked(4).Should().BeFalse();
            tracker.IsTracked(6).Should().BeFalse();

            tracker.Next().Should().Be(4);
            tracker.Next().Should().Be(6);
            tracker.Next().Should().Be(10);
        }
    }
}
