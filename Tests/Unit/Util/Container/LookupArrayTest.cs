using FluentAssertions;
using Helion.Util.Container;
using Xunit;

namespace Helion.Tests.Unit.Util.Container;

public class LookupArrayTest
{
    [Fact(DisplayName = "Set element")]
    public void Set()
    {
        LookupArray<string?> array = new();

        array.TryGetValue(420, out var value).Should().BeFalse();
        value.Should().BeNull();
        array.Set(420, "69");
        array.TryGetValue(420, out value).Should().BeTrue();
        value.Should().Be("69");
    }

    [Fact(DisplayName = "Set and change element")]
    public void SetAndChange()
    {
        LookupArray<string?> array = new();

        array.TryGetValue(420, out var value).Should().BeFalse();
        value.Should().BeNull();
        array.Set(420, "69");
        array.TryGetValue(420, out value).Should().BeTrue();
        value.Should().Be("69");

        array.Set(420, "100");
        array.TryGetValue(420, out value).Should().BeTrue();
        value.Should().Be("100");
    }

    [Fact(DisplayName = "Set and clear to null element")]
    public void SetAndClear()
    {
        LookupArray<string?> array = new();

        array.TryGetValue(420, out var value).Should().BeFalse();
        value.Should().BeNull();
        array.Set(420, "69");
        array.TryGetValue(420, out value).Should().BeTrue();
        value.Should().Be("69");

        array.Set(420, null);
        array.TryGetValue(420, out value).Should().BeFalse();
        value.Should().BeNull();
    }

    [Fact(DisplayName = "Set many elements")]
    public void SetMany()
    {
        int count = 1000;
        LookupArray<string?> array = new();

        for (int i = 0; i < count; i++)
        {
            array.TryGetValue(i, out var value).Should().BeFalse();
            value.Should().BeNull();
        }

        for (int i = 0; i < count; i++)
        {
            array.Set(i, i.ToString());
            array.TryGetValue(i, out var value).Should().BeTrue();
            value.Should().Be(i.ToString());
        }
    }

    [Fact(DisplayName = "Set alternating elements")]
    public void SetAlternating()
    {
        int count = 150;
        LookupArray<string?> array = new();
        for (int i = 0; i < count; i++)
        {
            if (i % 2 == 0)
                array.Set(i, i.ToString());
        }

        string? value;
        for (int i = 0; i < count; i++)
        {
            if (i % 2 == 0)
            {
                array.TryGetValue(i, out value).Should().BeTrue();
                value.Should().Be(i.ToString());
                continue;
            }

            array.TryGetValue(i, out value).Should().BeFalse();
            value.Should().BeNull();
        }
    }

    [Fact(DisplayName = "Set all elements")]
    public void SetAll()
    {
        int count = 100;
        LookupArray<string?> array = new();

        for (int i = 0; i < count; i++)
        {
            array.Set(i, i.ToString());
            array.TryGetValue(i, out var value).Should().BeTrue();
            value.Should().Be(i.ToString());
        }

        array.SetAll(null);
        array.TryGetValue(0, out var newValue).Should().BeFalse();
        newValue.Should().BeNull();

        array.SetAll("test");
        for (int i = 0; i < count; i++)
        {
            array.TryGetValue(0, out newValue).Should().BeTrue();
            newValue.Should().Be("test");
        }
    }
}
