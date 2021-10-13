using System.Drawing;
using FluentAssertions;
using Helion.Graphics.Palettes;
using Xunit;

namespace Helion.Tests.Unit.Graphics.Palettes;

public class PaletteTest
{
    [Fact(DisplayName = "Can create palette from appropriate data")]
    public void CreatePalette()
    {
        const int NumLayers = 2;

        byte[] data = new byte[Palette.BytesPerLayer * NumLayers];

        for (int i = 0; i < Palette.NumColors; i++)
        {
            byte color = (byte)(255 - i);
            data[i * 3] = color;
            data[(i * 3) + 1] = color;
            data[(i * 3) + 2] = color;
        }

        for (int i = Palette.BytesPerLayer; i < Palette.BytesPerLayer * NumLayers; i++)
            data[i] = 128;

        Palette palette = Palette.From(data)!;
        palette.Should().NotBeNull();

        palette.Count.Should().Be(NumLayers);

        Color[] topLayer = palette.DefaultLayer;
        for (int i = 0; i < Palette.NumColors; i++)
        {
            Color expected = Color.FromArgb(255 - i, 255 - i, 255 - i);
            topLayer[i].Should().Be(expected);
        }

        Color[] bottomLayer = palette.Layer(1);
        Color bottomColor = Color.FromArgb(128, 128, 128);
        for (int i = 0; i < Palette.NumColors; i++)
            bottomLayer[i].Should().Be(bottomColor);
    }

    [Theory(DisplayName = "Creating a palette from malformed size data fails")]
    [InlineData(0)]
    [InlineData(767)]
    [InlineData(769)]
    public void CreateBadSizedPaletteFails(int size)
    {
        Palette.From(new byte[size]).Should().BeNull();
    }
}
