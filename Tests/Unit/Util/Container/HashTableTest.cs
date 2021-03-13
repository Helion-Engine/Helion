using FluentAssertions;
using Helion.Util.Container;
using Xunit;

namespace Helion.Tests.Unit.Util.Container
{
    public class HashTableTest
    {
        private static HashTable<int, string, string> Create()
        {
            HashTable<int, string, string> table = new();
            table.Insert(1, "hi", "1hi");
            table.Insert(1, "yes", "1yes");
            table.Insert(2, "stuff", "2stuff");
            table.Insert(4, "four", "4four");
            return table;
        }
        
        [Fact(DisplayName = "Look up key pair by index")]
        public void LookUpKeyPair()
        {
            HashTable<int, string, string> table = Create();

            table[1, "yes"].Should().Be("1yes");
        }
        
        [Fact(DisplayName = "Look up value by trying")]
        public void TryGetValue()
        {
            HashTable<int, string, string> table = Create();

            string? result = null;
            table.TryGet(1, "yes", ref result).Should().BeTrue();
            result.Should().Be("1yes");
            
            table.TryGet(1234, "yes123123", ref result).Should().BeFalse();
            result.Should().BeNull();
        }
        
        [Fact(DisplayName = "Can insert into hash table")]
        public void InsertValue()
        {
            HashTable<int, string, string> table = new();

            string? output = null;
            table.TryGet(1, "1", ref output).Should().BeFalse();
            
            table.Insert(1, "1", "stuff");
            table.TryGet(1, "1", ref output).Should().BeTrue();
            output.Should().Be("stuff");
        }
        
        [Fact(DisplayName = "Can clear table")]
        public void CanClear()
        {
            HashTable<int, string, string> table = new();
            table.Insert(1, "1", "stuff");
            table.Insert(1, "2", "stuff!");
            table.CountAll().Should().Be(2);
            
            table.Clear();
            table.CountAll().Should().Be(0);
        }
        
        [Fact(DisplayName = "Can remove value from the table")]
        public void CanRemove()
        {
            HashTable<int, string, string> table = new();
            table.Insert(1, "1", "stuff");
            table.Insert(1, "2", "stuff!");
            
            string? value = null;
            table.TryGet(1, "1", ref value).Should().BeTrue();
            
            table.Remove(1, "1");
            table.TryGet(1, "1", ref value).Should().BeFalse();
        }
        
        [Fact(DisplayName = "Can get the first set of keys in the table")]
        public void GetFirstKeys()
        {
            HashTable<int, string, string> table = Create();

            table.GetFirstKeys().Should().Equal(1, 2, 4);
        }
        
        [Fact(DisplayName = "Count all of the values")]
        public void CountAllValues()
        {
            HashTable<int, string, string> table = Create();

            table.CountAll().Should().Be(4);
        }
        
        [Fact(DisplayName = "Get all values in the table")]
        public void GetAllValues()
        {
            HashTable<int, string, string> table = Create();

            table.GetValues().Should().Equal("1hi", "1yes", "2stuff", "4four");
        }
        
        [Fact(DisplayName = "Get all values in the table from the first key")]
        public void GetAllValuesByFirstKey()
        {
            HashTable<int, string, string> table = Create();

            table.GetValues(1).Should().Equal("1hi", "1yes");
        }
    }
}
