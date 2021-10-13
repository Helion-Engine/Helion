using System.Collections.Generic;
using FluentAssertions;
using Helion.Util.Extensions;
using Xunit;

namespace Helion.Tests.Unit.Util.Extensions;

public class ListExtensionsTest
{
    [Fact(DisplayName = "Check if a list is empty")]
    public void CheckEmptyList()
    {
        List<int> list = new();
        list.Empty().Should().BeTrue();

        list.Add(1);
        list.Empty().Should().BeFalse();
    }
}
