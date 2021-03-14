using System.Collections.Generic;
using FluentAssertions;
using Helion.Util.Extensions;
using Xunit;

namespace Helion.Tests.Unit.Util.Extensions
{
    public class StackExtensionsTest
    {
        [Fact(DisplayName = "Check if a stack is empty")]
        public void CheckEmptyStack()
        {
            Stack<int> list = new();
            list.Empty().Should().BeTrue();

            list.Push(1);
            list.Empty().Should().BeFalse();
        }
    }
}
