using System.Linq;
using FluentAssertions;
using Helion.Util.Container;
using Xunit;

namespace Helion.Tests.Unit.Util.Container
{
    public class LinkableListTest
    {
        [Fact(DisplayName = "Add value to list")]
        public void CanAddValue()
        {
            LinkableList<int> list = new();
            LinkableNode<int> node = list.Add(5);

            list.Head.Should().NotBeNull();
            list.Head.Should().BeSameAs(node);
            
            node.Value.Should().Be(5);
            node.Previous.Should().NotBeNull();
            node.Next.Should().BeNull();
        }
        
        [Fact(DisplayName = "Add node to list")]
        public void CanAddNodeValue()
        {
            LinkableList<int> list = new();
            LinkableNode<int> first = list.Add(5);
            LinkableNode<int> second = list.Add(10);

            second.Unlink();

            // This inserts at the front.
            list.Add(second);

            list.Head.Should().BeSameAs(second);
            second.Next.Should().BeSameAs(first);
        }
        
        [Fact(DisplayName = "Check if list contains value")]
        public void CheckContains()
        {
            LinkableList<int> list = new();
            list.Add(5);
            list.Add(10);

            list.Contains(5).Should().BeTrue();
            list.Contains(10).Should().BeTrue();
            list.Contains(7).Should().BeFalse();
        }
        
        [Fact(DisplayName = "Can iterate over list elements")]
        public void IterateOver()
        {
            LinkableList<int> list = new();
            list.Add(5);
            list.Add(10);

            list.ToList().Should().Equal(10, 5);
        }

        [Fact(DisplayName = "Can unlink node from front")]
        public void CanUnlinkFront()
        {
            LinkableList<int> list = new();
            LinkableNode<int> first = list.Add(5);
            LinkableNode<int> second = list.Add(10);
            
            first.Unlink();

            list.Head.Should().BeSameAs(second);
            second.Next.Should().BeNull();
        }
        
        [Fact(DisplayName = "Can unlink node from back")]
        public void CanUnlinkBack()
        {
            LinkableList<int> list = new();
            LinkableNode<int> first = list.Add(5);
            LinkableNode<int> second = list.Add(10);
            
            second.Unlink();

            list.Head.Should().BeSameAs(first);
            first.Next.Should().BeNull();
        }
    }
}
