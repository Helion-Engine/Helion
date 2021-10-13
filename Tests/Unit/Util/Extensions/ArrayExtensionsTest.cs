using FluentAssertions;
using Helion.Util.Extensions;
using Xunit;

namespace Helion.Tests.Unit.Util.Extensions;

public class ArrayExtensions
{
    [Fact(DisplayName = "Can fill array with values")]
    public void FillArray()
    {
        int[] ints = new int[4];
        ints.Fill(123);

        ints.Should().Equal(123, 123, 123, 123);
    }
}
