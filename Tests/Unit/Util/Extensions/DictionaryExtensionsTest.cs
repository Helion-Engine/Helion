using System.Collections.Generic;
using FluentAssertions;
using Helion.Util.Extensions;
using Xunit;

namespace Helion.Tests.Unit.Util.Extensions;

public class DictionaryExtensionsTest
{
    [Fact(DisplayName = "Check if a dictionary is empty")]
    public void CheckEmptyDictionary()
    {
        Dictionary<int, string> dictionary = new();
        dictionary.Empty().Should().BeTrue();

        dictionary[1] = "hi";
        dictionary.Empty().Should().BeFalse();
    }
}
