using System.Collections.Generic;
using FluentAssertions;
using Helion.Util.Extensions;
using Xunit;

namespace Helion.Tests.Unit.Util.Extensions;

public class LinkedListExtensionsTest
{
    [Fact(DisplayName = "Check if a linked list is empty")]
    public void CheckEmptyLinkedList()
    {
        LinkedList<int> list = new();
        list.Empty().Should().BeTrue();

        list.AddLast(1);
        list.Empty().Should().BeFalse();
    }
}
