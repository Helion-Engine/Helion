using Helion.Util;
using Helion.Util.Geometry;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using static Helion.Util.Assertion.Assert;

namespace BspVisualizer
{
    public enum Corner
    {
        TopLeft,
        TopRight,
        BottomLeft,
        BottomRight
    }

    /// <summary>
    /// A helper class that draws string at some specified corner. It must be
    /// loaded with information for each draw invocation.
    /// </summary>
    public class StringCornerDrawer
    {
        private Corner corner;
        private Vec2I cornerOffset;
        private List<List<Tuple<Color, string>>> messageLists = new List<List<Tuple<Color, string>>>();

        public StringCornerDrawer(Corner targetCorner, Vec2I targetCornerOffset)
        {
            corner = targetCorner;
            cornerOffset = targetCornerOffset;
        }

        private SizeF CalculateTotalSize(Graphics g, Font font, List<Tuple<Color, string>> coloredMessages)
        {
            // Because we are drawing them all in the same horizontal line, we
            // perform a reduce along the X axis by summary, however we are not
            // drawing vertically so we only want the maximum height.
            return coloredMessages.Select(colorMsg => g.MeasureString(colorMsg.Item2, font))
                                  .Aggregate(new SizeF(0, 0), (sum, val) => new SizeF(sum.Width + val.Width, Math.Max(sum.Height, val.Height)));
        }

        private void DrawAtBottomLeft(Graphics g, Rectangle windowBounds, Font font)
        {
            Vec2I offset = new Vec2I(cornerOffset.X, windowBounds.Bottom - cornerOffset.Y);

            messageLists.ForEach(coloredMessages =>
            {
                SizeF totalSize = CalculateTotalSize(g, font, coloredMessages);
                int messageOffsetX = offset.X;

                coloredMessages.ForEach(coloredMessage =>
                {
                    (Color color, string message) = coloredMessage;
                    SizeF size = g.MeasureString(message, font);
                    int yOffset = offset.Y - (int)size.Height;

                    g.DrawString(message, font, new SolidBrush(color), new PointF(messageOffsetX, yOffset));

                    messageOffsetX += (int)size.Width;
                });

                offset.Y -= (int)totalSize.Height;
            });
        }

        public void Add(params object[] coloredMessages)
        {
            if (coloredMessages.Length % 2 != 0)
                throw new HelionException("Expecting an even sequence of color/string objects");

            List<Tuple<Color, string>> messageComponents = new List<Tuple<Color, string>>();

            for (int i = 0; i < coloredMessages.Length; i += 2)
            {
                // Unfortunately nullable types and structs are not friendly,
                // so this needs to be done instead. We just want the program
                // to die if the developer ever makes a mistake that causes it
                // to fail here.
                if (!(coloredMessages[i] is Color))
                    throw new HelionException("Expecting a color as the first type of the colored message pair");
                if (!(coloredMessages[i + 1] is string))
                    throw new HelionException("Expecting a string as the second type of the colored message pair");

                Color color = (Color)coloredMessages[i];
                string message = (string)coloredMessages[i + 1];

                messageComponents.Add(Tuple.Create(color, message));
            }

            messageLists.Add(messageComponents);
        }

        public void Clear() => messageLists.Clear();

        public void DrawAndClear(Graphics g, Rectangle windowBounds, Font font)
        {
            switch (corner)
            {
            case Corner.BottomLeft:
                DrawAtBottomLeft(g, windowBounds, font);
                break;
            default:
                Fail("Unknown corner enumeration (or not supported currently)");
                break;
            }

            Clear();
        }
    }
}
