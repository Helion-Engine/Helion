using System;
using System.Collections.Generic;
using FluentAssertions;
using Helion.Graphics;
using Helion.Graphics.Fonts;
using Helion.Render.OpenGL.Texture.Fonts;
using Helion.Util;
using Xunit;

namespace Helion.Tests.Unit.Graphics.String;

public class RGBColoredStringDecoderTest
{
    private static void AssertMatches(string rawString, params Tuple<string, Color>[] expectedColors)
    {
        DataCache dataCache = new();
        var image = new Image((16, 16), ImageType.Argb);
        var glyphs = new Dictionary<char, Glyph>() { { 'A', new Glyph() } };
        var font = new Font("test", glyphs, image);
        RenderableString colorStr = new(dataCache, rawString, font, 12);

        int startIndex = 0;
        int endIndex = 0;

        foreach (var stringColorPair in expectedColors)
        {
            (string str, Color color) = stringColorPair;

            endIndex = startIndex + str.Length;

            for (int i = startIndex; i < endIndex; i++)
            {
                char expectedChar = str[i - startIndex];
                colorStr.Sentences[0].Glyphs[i].Character.Should().Be(expectedChar);
                colorStr.Sentences[0].Glyphs[i].Color.Should().Be(color);
            }

            startIndex = endIndex;
        }

        if (rawString.Length > 0)
            colorStr.Sentences[0].Glyphs.Length.Should().Be(endIndex);
    }

    [Fact(DisplayName = "Checks empty string")]
    public void EmptyString()
    {
        AssertMatches("", Tuple.Create("", RenderableString.DefaultColor));
    }

    [Fact(DisplayName = "Handles no color decoding")]
    public void NoColorDecoding()
    {
        AssertMatches("some str", Tuple.Create("some str", RenderableString.DefaultColor));
    }

    [Fact(DisplayName = "Handles a single color")]
    public void SingleColor()
    {
        AssertMatches(@"a\c[123,45,6]color", Tuple.Create("a", Color.White), Tuple.Create("color", new Color(123, 45, 6)));
    }

    [Fact(DisplayName = "Color at end of the string does nothing")]
    public void ColorAtEndOfStringDoesNothing()
    {
        AssertMatches(@"some str\c[1,2,3]", Tuple.Create("some str", RenderableString.DefaultColor));
    }

    [Fact(DisplayName = "Decodes multiple colors")]
    public void MultipleColors()
    {
        AssertMatches(@"\c[123,45,6] \c[0,0,0]some c\c[255,0,255]olor\c[1,2,1]s", Tuple.Create(" ", new Color(123, 45, 6)), Tuple.Create("some c", new Color(0, 0, 0)), Tuple.Create("olor", new Color(255, 0, 255)), Tuple.Create("s", new Color(1, 2, 1)));
    }

    [Fact(DisplayName = "Malformed color codes are ignored")]
    public void MalformedColorCodeIsIgnored()
    {
        AssertMatches(@"\c[0,0,0hi", Tuple.Create(@"\c[0,0,0hi", RenderableString.DefaultColor));
    }

    [Fact(DisplayName = "Not allowed to do RGB values larger than 255")]
    public void HigherThan255IsClamped()
    {
        AssertMatches(@"\c[0,5,982]hi", Tuple.Create("hi", new Color(0, 5, 255)));
    }

    [Fact(DisplayName = "Negatives for RGB do not work")]
    public void CannotUseNegatives()
    {
        AssertMatches(@"\c[0,-5,1]hi", Tuple.Create(@"\c[0,-5,1]hi", RenderableString.DefaultColor));
    }
}
