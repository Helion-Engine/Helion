using System.Collections.Generic;
using FluentAssertions;
using Helion.Util.Extensions;
using Xunit;

namespace Helion.Tests.Unit.Util.Extensions;

public class HashSetExtensions
{
    [Fact(DisplayName = "Check if a hash set is empty")]
    public void CheckEmptySet()
    {
        HashSet<int> set = new();
        set.Empty().Should().BeTrue();

        set.Add(1);
        set.Empty().Should().BeFalse();
    }
}
