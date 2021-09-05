using FluentAssertions;
using Xunit;

namespace Helion.Tests.Unit.Util.Configs.Impl
{
    public class ConfigKeyMappingTest
    {
        [Fact(DisplayName = "Can add defaults")]
        public void CanAddDefaults()
        {
            false.Should().BeTrue();
        }
        
        [Fact(DisplayName = "Look up by key")]
        public void LookUpByKey()
        {
            false.Should().BeTrue();
        }
        
        [Fact(DisplayName = "Look up by command")]
        public void LookUpByCommand()
        {
            false.Should().BeTrue();
        }
        
        [Fact(DisplayName = "Can add new key/command mapping")]
        public void AddNewMapping()
        {
            false.Should().BeTrue();
        }
        
        [Fact(DisplayName = "Can add existing key mapping to a new command")]
        public void AddExistingMappingNewCommand()
        {
            false.Should().BeTrue();
        }
        
        [Fact(DisplayName = "Can add an existing command to a new key")]
        public void AddExistingMappingNewKey()
        {
            false.Should().BeTrue();
        }
        
        [Fact(DisplayName = "Can consume a key press for a command")]
        public void CanConsumeKeyCommandPress()
        {
            false.Should().BeTrue();
        }
        
        [Fact(DisplayName = "Can consume key down for a command")]
        public void CanConsumeKeyDownCommand()
        {
            false.Should().BeTrue();
        }
        
        [Fact(DisplayName = "Can unbind all")]
        public void CanUnbindAll()
        {
            false.Should().BeTrue();
        }
        
        [Fact(DisplayName = "Unbind all marks change if key was bound")]
        public void UnbindAllMarksChangedIfKeyBound()
        {
            false.Should().BeTrue();
        }
        
        [Fact(DisplayName = "Adding a new key value marks a change")]
        public void AddNewKeyMarksChanged()
        {
            false.Should().BeTrue();
        }
        
        [Fact(DisplayName = "Adding a new command marks a change")]
        public void AddNewCommandMarksChanged()
        {
            false.Should().BeTrue();
        }
        
        [Fact(DisplayName = "Iterate over all keys")]
        public void CanIterateOverKeys()
        {
            false.Should().BeTrue();
        }
    }
}
