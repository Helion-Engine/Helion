using FluentAssertions;
using Helion.Util.Container;
using System;
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
        CompareArrays(array, 5, 7);
        array.Capacity.Should().BeGreaterThanOrEqualTo(2);

        array.Add(1);
        array.Length.Should().Be(3);
        CompareArrays(array, 5, 7, 1);
        array.Capacity.Should().BeGreaterThanOrEqualTo(3);
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
        CompareArrays(array, 1, -5, 4);

        int capacity = array.Capacity;
        array.Clear();

        CompareArrays(array, 1, -5, 4);
        array.Length.Should().Be(0);
        array.Capacity.Should().Be(capacity);
    }

    [Fact(DisplayName = "Remove last dynamic array element")]
    public void CanRemoveLast()
    {
        DynamicArray<int> array = new(2);

        array.Add(1, -5, 4);
        CompareArrays(array, 1, -5, 4);
        int capacity = array.Capacity;

        array.RemoveLast().Should().Be(4);
        CompareArrays(array, 1, -5, 4);
        array.Capacity.Should().Be(capacity);
        array.Length.Should().Be(2);

        array.RemoveLast().Should().Be(-5);
        array.RemoveLast().Should().Be(1);
        CompareArrays(array, 1, -5, 4);
        array.Capacity.Should().Be(capacity);
        array.Length.Should().Be(0);

        // No more elements, RemoveLast should throw
        Action a = () => array.RemoveLast();
        a.Should().Throw<InvalidOperationException>();
        array.Capacity.Should().Be(capacity);
        array.Length.Should().Be(0);
    }

    private static bool CompareArrays(DynamicArray<int> x, params int[] y)
    {
        if (x.Length != y.Length)
            return false;

        for (int i = 0; i < x.Length; i++)
        {
            if (x[i] != y[i])
                return false;
        }

        return true;
    }
}
