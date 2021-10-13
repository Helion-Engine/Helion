using FluentAssertions;
using Helion.Util.Container;
using Xunit;

namespace Helion.Tests.Unit.Util.Container;

public class DynamicArrayTest
{
    [Fact(DisplayName = "Add element to dynamic array")]
    public void CanAdd()
    {
        DynamicArray<int> array = new();

        array.Add(5);

        array.Length.Should().Be(1);
        array.Data[0].Should().Be(5);
    }

    [Fact(DisplayName = "Add multiple element to dynamic array")]
    public void CanAddMultiple()
    {
        DynamicArray<int> array = new(2);

        array.Add(5, 7);
        array.Length.Should().Be(2);
        array.Data.Should().Equal(5, 7);
        array.Capacity.Should().Be(2);

        array.Add(1);
        array.Length.Should().Be(3);
        array.Data.Should().Equal(5, 7, 1, default(int)); // Capacity is doubled, so add a default.
        array.Capacity.Should().BeGreaterThan(2);
    }

    [Fact(DisplayName = "Can index into a dynamic array")]
    public void CanIndex()
    {
        DynamicArray<int> array = new();

        array.Add(1, -5, 4);

        array.Data[0].Should().Be(1);
        array.Data[1].Should().Be(-5);
        array.Data[2].Should().Be(4);
    }

    [Fact(DisplayName = "Can clear a dynamic array")]
    public void CanClear()
    {
        DynamicArray<int> array = new(2);

        array.Add(1, -5, 4);
        array.Data.Should().Equal(1, -5, 4, default(int));

        int capacity = array.Capacity;
        array.Clear();

        array.Data.Should().Equal(1, -5, 4, default(int));
        array.Length.Should().Be(0);
        array.Capacity.Should().Be(capacity);
    }

    [Fact(DisplayName = "Remove last dynamic array element")]
    public void CanRemoveLast()
    {
        DynamicArray<int> array = new(2);

        array.Add(1, -5, 4);
        array.Data.Should().Equal(1, -5, 4, default(int));
        int capacity = array.Capacity;

        array.RemoveLast();
        array.Data.Should().Equal(1, -5, 4, default(int));
        array.Capacity.Should().Be(capacity);
        array.Length.Should().Be(2);

        array.RemoveLast();
        array.RemoveLast();
        array.Data.Should().Equal(1, -5, 4, default(int));
        array.Capacity.Should().Be(capacity);
        array.Length.Should().Be(0);

        // Last remove should not do anything.
        array.RemoveLast();
        array.Data.Should().Equal(1, -5, 4, default(int));
        array.Capacity.Should().Be(capacity);
        array.Length.Should().Be(0);
    }
}
