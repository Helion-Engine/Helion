using FluentAssertions;
using Xunit;

namespace Helion.Tests.Unit.Util.Configs.Impl
{
    public class ConfigTest
    {
        [Fact(DisplayName = "Can iterate over all config elements")]
        public void CanIterateOverAllElements()
        {
            false.Should().BeTrue();
        }
        
        [Fact(DisplayName = "Can get existing component")]
        public void CanGetExistingComponent()
        {
            false.Should().BeTrue();
        }
        
        [Fact(DisplayName = "Cannot get a missing component")]
        public void CannotGetMissingComponent()
        {
            false.Should().BeTrue();
        }
        
        [Fact(DisplayName = "Apply queued changes affects only those with proper bit flags")]
        public void TestAppliedQueuedChanges()
        {
            false.Should().BeTrue();
        }
    }
}
