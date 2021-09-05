using FluentAssertions;
using Xunit;

namespace Helion.Tests.Unit.Util.Configs.Impl
{
    public class FileConfigTest
    {
        [Fact(DisplayName = "Can read config file with no default values set")]
        public void CanReadWithNoDefaults()
        {
            false.Should().BeTrue();
        }
        
        [Fact(DisplayName = "Can read config file with default values set")]
        public void CanReadWithDefaults()
        {
            false.Should().BeTrue();
        }
        
        [Fact(DisplayName = "Can write config file")]
        public void CanWrite()
        {
            false.Should().BeTrue();
        }
        
        [Fact(DisplayName = "Writing config file with no changes leads to no writing")]
        public void WritingFailsIfNoChanges()
        {
            false.Should().BeTrue();
        }
        
        [Fact(DisplayName = "Can force write a config file")]
        public void CanWriteForced()
        {
            false.Should().BeTrue();
        }
    }
}
