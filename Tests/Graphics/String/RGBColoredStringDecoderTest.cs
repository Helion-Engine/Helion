using Helion.Graphics.String;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Drawing;

namespace Helion.Test.Graphics.String
{
    [TestClass]
    public class RGBColoredStringDecoderTest
    {
        private static void AssertMatches(string rawString, params Tuple<string, Color>[] expectedColors)
        {
            ColoredString colorStr = RGBColoredStringDecoder.Decode(rawString);

            int startIndex = 0;
            int endIndex = 0;

            foreach (var stringColorPair in expectedColors)
            {
                (string str, Color color) = stringColorPair;

                endIndex = startIndex + str.Length;

                for (int i = startIndex; i < endIndex; i++)
                {
                    char expectedChar = str[i - startIndex];
                    Assert.AreEqual(colorStr[i].C, expectedChar);
                    Assert.AreEqual(colorStr[i].Color, color);
                }

                startIndex = endIndex;
            }

            Assert.AreEqual(colorStr.Length, endIndex);
        }

        [TestMethod]
        public void EmptyString()
        {
            AssertMatches("", new Tuple<string, Color>[]
                {
                    Tuple.Create("", ColoredString.DefaultColor),
                }
            );
        }

        [TestMethod]
        public void NoColorDecoding()
        {
            AssertMatches("some str", new Tuple<string, Color>[]
                {
                    Tuple.Create("some str", ColoredString.DefaultColor),
                }
            );
        }

        [TestMethod]
        public void SingleColor()
        {
            AssertMatches(@"a\c[123,45,6]color", new Tuple<string, Color>[]
                {
                    Tuple.Create("a", Color.White),
                    Tuple.Create("color", Color.FromArgb(123, 45, 6)),
                }
            );
        }

        [TestMethod]
        public void ColorAtEndOfStringDoesNothing()
        {
            AssertMatches(@"some str\c[1,2,3]", new Tuple<string, Color>[]
                {
                    Tuple.Create("some str", ColoredString.DefaultColor),
                }
            );
        }

        [TestMethod]
        public void MultipleColors()
        {
            AssertMatches(@"\c[123,45,6] \c[0,0,0]some c\c[255,0,255]olor\c[1,2,1]s", new Tuple<string, Color>[]
                {
                    Tuple.Create(" ", Color.FromArgb(123, 45, 6)),
                    Tuple.Create("some c", Color.FromArgb(0, 0, 0)),
                    Tuple.Create("olor", Color.FromArgb(255, 0, 255)),
                    Tuple.Create("s", Color.FromArgb(1, 2, 1)),
                }
            );
        }

        [TestMethod]
        public void MalformedColorCodeIsIgnored()
        {
            AssertMatches(@"\c[0,0,0hi", new Tuple<string, Color>[]
                {
                    Tuple.Create(@"\c[0,0,0hi", ColoredString.DefaultColor),
                }
            );
        }

        [TestMethod]
        public void HigherThan255IsClamped()
        {
            AssertMatches(@"\c[0,5,982]hi", new Tuple<string, Color>[]
                {
                    Tuple.Create("hi", Color.FromArgb(0, 5, 255)),
                }
            );
        }

        [TestMethod]
        public void CannotUseNegatives()
        {
            AssertMatches(@"\c[0,-5,1]hi", new Tuple<string, Color>[]
                {
                    Tuple.Create(@"\c[0,-5,1]hi", ColoredString.DefaultColor),
                }
            );
        }
    }
}
