using System;
using System.Drawing;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics.Fonts.Renderable;
using Helion.Graphics.Geometry;
using Helion.Graphics.String;
using Helion.Render.OpenGL.Legacy.Commands;
using Helion.Render.OpenGL.Legacy.Commands.Alignment;
using Font = Helion.Graphics.Fonts.Font;

namespace Helion.Render.OpenGL.Legacy.Shared.Drawers.Helper
{
    /// <summary>
    /// Intended to provide convenience methods for drawing so that the user
    /// does not have to interface with the abstraction layer that sits above
    /// the different renderers.
    /// </summary>
    /// <remarks>
    /// The stack of abstractions looks like this:
    ///
    /// DrawHelper
    ///    ^
    ///    |
    /// Renderer abstraction
    ///    ^
    ///    |
    /// Low level renderer
    /// </remarks>
    public class DrawHelper
    {
        private const float Opaque = 1.0f;
        private static readonly Color NoColor = Color.White;

        private readonly RenderCommands m_renderCommands;

        public IImageDrawInfoProvider DrawInfoProvider => m_renderCommands.ImageDrawInfoProvider;

        public DrawHelper(RenderCommands renderCommands)
        {
            m_renderCommands = renderCommands;
        }

        /// <summary>
        /// See <see cref="DrawInfoProvider"/> for this function.
        /// </summary>
        public bool ImageExists(string name) => DrawInfoProvider.ImageExists(name);

        /// <summary>
        /// Performs a series of commands at some resolution. This allows you
        /// to make some calls without mutating the virtual resolution state
        /// in the case that you want to preserve whatever resolution is being
        /// used currently. Will revert back to the virtual resolution after
        /// this function is done calling the actions.
        /// </summary>
        /// <param name="resolutionInfo">The new temporary information.</param>
        /// <param name="action">The actions to perform while at the resolution
        /// provided.</param>
        public void AtResolution(ResolutionInfo resolutionInfo, Action action)
        {
            ResolutionInfo current = m_renderCommands.ResolutionInfo;
            m_renderCommands.SetVirtualResolution(resolutionInfo);

            action();

            m_renderCommands.SetVirtualResolution(current);
        }

        /// <summary>
        /// Draws a pixel at the position provided.
        /// </summary>
        /// <remarks>
        /// This is very bad for performance, and should only be done
        /// sparingly.
        /// </remarks>
        /// <param name="x">The X coordinate on the screen from the top left.
        /// </param>
        /// <param name="y">The Y coordinate on the screen from the top left.
        /// </param>
        /// <param name="color">The color to draw.</param>
        public void Pixel(int x, int y, Color color)
        {
            FillRect(x, y, 1, 1, color);
        }

        // TODO: Support window and box alignment.
        public void DrawRect(int x, int y, int width, int height, Color color, float alpha = Opaque)
        {
            // TODO: Call FillRect for 4 edge lines 1 pixel thick?
            throw new NotImplementedException();
        }

        // TODO: Support window and box alignment.
        public void FillRect(int x, int y, int width, int height, Color color, float alpha = Opaque)
        {
            ImageBox2I drawArea = new ImageBox2I(x, y, x + width, y + height);
            m_renderCommands.FillRect(drawArea, color, alpha);
        }
        
        public void FillWindow(Color color, float alpha = Opaque)
        {
            (int windowW, int windowH) = m_renderCommands.WindowDimension;
            FillRect(0, 0, windowW, windowH, Color.Black);
        }

        /// <summary>
        /// Draws an image at the aligned origin. Does not care about returning
        /// the drawing area.
        /// See the method that has all the options for the parameter info.
        /// </summary>
        public void Image(string name, int? width = null, int? height = null,
            Align window = Align.TopLeft, Align image = Align.TopLeft, Align? both = null,
            Color? color = null, float alpha = 1.0f, bool drawInvul = false)
        {
            Image(name, 0, 0, out _, width, height, window, image, both, color, alpha, drawInvul);
        }

        /// <summary>
        /// Draws an image. Does not care about returning the drawing area.
        /// See the method that has all the options for the parameter info.
        /// </summary>
        public void Image(string name, int x, int y, int? width = null, int? height = null,
            Align window = Align.TopLeft, Align image = Align.TopLeft, Align? both = null,
            Color? color = null, float alpha = 1.0f, bool drawInvul = false)
        {
            Image(name, x, y, out _, width, height, window, image, both, color, alpha, drawInvul);
        }

        /// <summary>
        /// Draws an image. Does not care about returning the drawing area.
        /// See the method that has all the options for the parameter info.
        /// </summary>
        public void Image(string name, Vec2I offset, int? width = null, int? height = null,
            Align window = Align.TopLeft, Align image = Align.TopLeft, Align? both = null,
            Color? color = null, float alpha = 1.0f, bool drawInvul = false, float scale = 1.0f)
        {
            Image(name, offset, out _, width, height, window, image, both, color, alpha, drawInvul, scale);
        }

        /// <summary>
        /// Draws an image.
        /// See the method that has all the options for the parameter info.
        /// </summary>
        public void Image(string name, int x, int y, out Dimension drawDimension, int? width = null,
            int? height = null, Align window = Align.TopLeft, Align image = Align.TopLeft,
            Align? both = null, Color? color = null, float alpha = 1.0f, bool drawInvul = false, float scale = 1.0f)
        {
            Image(name, new Vec2I(x, y), out drawDimension, width, height, window, image, both, color, alpha, drawInvul, scale);
        }

        /// <summary>
        /// Draw an image.
        /// </summary>
        /// <param name="name">The image name.</param>
        /// <param name="offset">The offset. Positive is down/right.</param>
        /// <param name="drawArea">The output of the area that is drawn.
        /// This is populated if either width or height are null.</param>
        /// <param name="width">If not null, uses the width instead of the
        /// native width of the image.</param>
        /// <param name="height">If not null, uses the height instead of the
        /// native height of the image.</param>
        /// <param name="window">The alignment to the window pivot. For more
        /// info, see <see cref="GetDrawingCoordinateFromAlign"/>. The top
        /// left is the default.</param>
        /// <param name="image">The alignment to the image pivot. For more
        /// info, see <see cref="GetDrawingCoordinateFromAlign"/>. The top
        /// left is the default.</param>
        /// <param name="both">Instead of specifying the same alignment for
        /// both the `window` and `image`, you can set this value and it sets
        /// both of them to be this value. Whatever value is present overrides
        /// the other two (provided this is not null).</param>
        /// <param name="color">The color to draw with. If not present, the
        /// color of the image will be unchanged. This can be used to select
        /// certain channels (ex: to make it red, use [255, 0, 0]).</param>
        /// <param name="alpha">The transparency (1.0 is unchanged, 0.5 is
        /// half visible, 0.0 is not visible at all). Default is 1.0.</param>
        /// <param name="drawInvul">If invulnerability coloring should be
        /// applied to the image.</param>
        /// <param name="scale">Scales the draw area.</param>
        public void Image(string name, Vec2I offset, out Dimension drawArea, int? width = null,
            int? height = null, Align window = Align.TopLeft, Align image = Align.TopLeft, Align? both = null,
            Color? color = null, float alpha = 1.0f, bool drawInvul = false, float scale = 1.0f)
        {
            Align alignWindow = both ?? window;
            Align alignImage = both ?? image;

            if (width == null || height == null)
            {
                (int imageWidth, int imageHeight) = DrawInfoProvider.GetImageDimension(name);
                drawArea = new Dimension(width ?? imageWidth, height ?? imageHeight);
            }
            else
                drawArea = new Dimension(width.Value, height.Value);

            if (scale != 1.0f)
                drawArea.Scale(scale);

            Vec2I pos = GetDrawingCoordinateFromAlign(offset.X, offset.Y, drawArea.Width, drawArea.Height,
                alignWindow, alignImage);
            m_renderCommands.DrawImage(name, pos.X, pos.Y, drawArea.Width, drawArea.Height,
                color ?? NoColor, alpha, drawInvul);
        }

        /// <summary>
        /// Draws text. The entire message will be colored by the provided
        /// color in a scalar way (multiply the channel by the normalized
        /// value. For example, white is [1.0, 1.0, 1.0], so the color will
        /// not be changed. If the color is red [1.0, 0.0, 0.0], then the
        /// blue and green channels will be zero, making the text look red.
        /// Since most (or all) text is colored white by default, this is
        /// more or less the same as being the color that will be drawn.
        /// Finally, Does not care about returning the drawing area.
        /// See the method that has all the options for the parameter info.
        /// </summary>
        public void Text(Color color, string message, Font font, int fontSize, int x, int y,
            TextAlign text = TextAlign.Left, Align window = Align.TopLeft, Align textbox = Align.TopLeft,
            Align? both = null, int maxWidth = int.MaxValue, float alpha = 1.0f)
        {
            Text(color, message, font, fontSize, out _, x, y, text, window, textbox, both, maxWidth, alpha);
        }

        /// <summary>
        /// Draws a text. Does not care about returning the drawing area.
        /// See the method that has all the options for the parameter info.
        /// </summary>
        public void Text(ColoredString message, Font font, int fontSize, int x, int y,
            TextAlign text = TextAlign.Left, Align window = Align.TopLeft, Align textbox = Align.TopLeft,
            Align? both = null, int maxWidth = int.MaxValue, float alpha = 1.0f)
        {
            Text(message, font, fontSize, out _, x, y, text, window, textbox, both, maxWidth, alpha);
        }

        /// <summary>
        /// Draws text. The entire message will be colored by the provided
        /// color in a scalar way (multiply the channel by the normalized
        /// value. For example, white is [1.0, 1.0, 1.0], so the color will
        /// not be changed. If the color is red [1.0, 0.0, 0.0], then the
        /// blue and green channels will be zero, making the text look red.
        /// Since most (or all) text is colored white by default, this is
        /// more or less the same as being the color that will be drawn.
        /// See the method that has all the options for the parameter info.
        /// </summary>
        public void Text(Color color, string message, Font font, int fontSize, out Dimension drawArea,
            int x, int y, TextAlign text = TextAlign.Left, Align window = Align.TopLeft,
            Align textbox = Align.TopLeft, Align? both = null, int maxWidth = int.MaxValue, float alpha = 1.0f)
        {
            ColoredString coloredString = ColoredStringBuilder.From(color, message);
            Text(coloredString, font, fontSize, out drawArea, x, y, text, window, textbox, both, maxWidth, alpha);
        }

        /// <summary>
        /// Draws text.
        /// </summary>
        /// <param name="message">The message to draw.</param>
        /// <param name="font">The font to draw with.</param>
        /// <param name="fontSize">The font size (vertical height in pixels).
        /// </param>
        /// <param name="drawArea">The dimensions of the text that is drawn.
        /// </param>
        /// <param name="x">The X offset. Positive is right, negative is left
        /// on the screen.</param>
        /// <param name="y">The Y offset. Positive is down, negative is up on
        /// the screen.</param>
        /// <param name="text">The text alignment. By default draws from left
        /// to right and aligns with the left side.</param>
        /// <param name="window">The alignment to the window pivot. For more
        /// info, see <see cref="GetDrawingCoordinateFromAlign"/>. The top
        /// left is the default.</param>
        /// <param name="textbox">The alignment to the image pivot. For more
        /// info, see <see cref="GetDrawingCoordinateFromAlign"/>. The top
        /// left is the default.</param>
        /// <param name="both">Instead of specifying the same alignment for
        /// `window` and `textbox`, you can set this value and it sets both
        /// of them to be this value. Whatever value is present overrides the
        /// other two (provided this is not null).</param>
        /// <param name="maxWidth">How many pixels wide the text drawing should
        /// go before it stops, or wraps if wrap is true. If wrap is false then
        /// it will terminate drawing as soon as it exceeds the max width. It
        /// will not draw a character that leaks past the edge, meaning if the
        /// space remaining is half a character, then the last character will
        /// not be drawn.</param>
        /// <param name="alpha">The transparency (1.0 is unchanged, 0.5 is
        /// half visible, 0.0 is not visible at all). Default is 1.0.</param>
        public void Text(ColoredString message, Font font, int fontSize, out Dimension drawArea,
            int x, int y, TextAlign text = TextAlign.Left, Align window = Align.TopLeft,
            Align textbox = Align.TopLeft, Align? both = null, int maxWidth = int.MaxValue, float alpha = 1.0f)
        {
            Align alignWindow = both ?? window;
            Align alignTextbox = both ?? textbox;

            RenderableString renderableString = new(message, font, fontSize, text, maxWidth);
            drawArea = renderableString.DrawArea;

            Vec2I pos = GetDrawingCoordinateFromAlign(x, y, drawArea.Width, drawArea.Height, alignWindow, alignTextbox);
            m_renderCommands.DrawText(renderableString, pos.X, pos.Y, alpha);
        }

        public Dimension TextDrawArea(string text, Font font, int fontSize,
            TextAlign align = TextAlign.Left, int maxWidth = int.MaxValue)
        {
            ColoredString coloredString = ColoredStringBuilder.From(Color.Black, text);
            RenderableString renderableString = new(coloredString, font, fontSize, align, maxWidth);
            return renderableString.DrawArea;
        }

        /// <summary>
        /// This takes offsets, image dimensions, alignment values, and it
        /// calculates the position on the virtual window where it should be
        /// drawn. See the remarks section for all of the logic.
        /// </summary>
        /// <remarks>
        /// The X/Y offsets are always relative to the origin's direction. This
        /// means if they're negative, it moves up/left on the screen, and also
        /// down/right if positive. These are applied after the image is placed
        /// at its location based on alignment.
        ///
        /// The alignment works as follows. It selects the point on the image
        /// to which the point corresponds to. The image below demonstrates
        /// this (these are also straightforward, but here for completeness'
        /// sake):
        ///                     (TopMiddle)
        /// (TopLeft)    o-----------o-----------o (TopRight)
        ///              |                       |
        ///              |        (Center)       |
        /// (MiddleLeft) o           o           o (MiddleRight)
        ///              |                       |
        ///              |                       |
        /// (BottomLeft) o-----------o-----------o (BottomRight)
        ///                   (BottomMiddle)
        ///
        /// The window alignment finds the point above on the screen. Then the
        /// same thing is done for the image, and then the image 'pivot' is
        /// placed on top of the window pivot.
        ///
        /// For example, suppose we had a window alignment of "Center", and an
        /// image alignment of "BottomMiddle". The image alignment says that
        /// the pivot point is at the bottom middle of the image. Then we place
        /// this pivot point onto the window point. It would draw like so, and
        /// both pivot points are represented by an 'o' (which there's only one
        /// since we place the pivots on top of each other), and the image to be
        /// drawn is represented by dots:
        ///
        ///              +-----------------------+
        ///              |                       |
        ///              |         .....         |
        ///              |         .....         |   This is the entire screen.
        ///              |         ..o..         |
        ///              |                       |   The pivot point in the image
        ///              |                       |   is always placed on top of
        ///              |                       |   the window one.
        ///              +-----------------------+
        /// </remarks>
        /// <param name="xOffset">The X offset (positive is left).</param>
        /// <param name="yOffset">The Y offset (positive is down).</param>
        /// <param name="width">The width of the drawing object. If this is an
        /// image, then it's the image width.</param>
        /// <param name="height">See 'width', but for height.
        /// </param>
        /// <param name="windowAlign">The alignment relative to a window point.
        /// </param>
        /// <param name="imageAlign">The alignment of the image (like above)
        /// though it anchors the point onto the window align point.</param>
        /// <returns>The position to pass to the renderer. Equal to the top
        /// left drawing coordinate.</returns>
        private Vec2I GetDrawingCoordinateFromAlign(int xOffset, int yOffset, int width, int height,
            Align windowAlign, Align imageAlign)
        {
            Vec2I offset = new Vec2I(xOffset, yOffset);
            Dimension window = m_renderCommands.ResolutionInfo.VirtualDimensions;

            Vec2I windowPos = windowAlign switch
            {
                Align.TopLeft => new Vec2I(0, 0),
                Align.TopMiddle => new Vec2I(window.Width / 2, 0),
                Align.TopRight => new Vec2I(window.Width - 1, 0),
                Align.MiddleLeft => new Vec2I(0, window.Height / 2),
                Align.Center => new Vec2I(window.Width / 2, window.Height / 2),
                Align.MiddleRight => new Vec2I(window.Width - 1, window.Height / 2),
                Align.BottomLeft => new Vec2I(0, window.Height - 1),
                Align.BottomMiddle => new Vec2I(window.Width / 2, window.Height - 1),
                Align.BottomRight => new Vec2I(window.Width - 1, window.Height - 1),
                _ => throw new Exception($"Unsupported window alignment: {windowAlign}")
            };

            // This is relative to the window position.
            Vec2I imageOffset = imageAlign switch
            {
                Align.TopLeft => -new Vec2I(0, 0),
                Align.TopMiddle => -new Vec2I(width / 2, 0),
                Align.TopRight => -new Vec2I(width - 1, 0),
                Align.MiddleLeft => -new Vec2I(0, height / 2),
                Align.Center => -new Vec2I(width / 2, height / 2),
                Align.MiddleRight => -new Vec2I(width - 1, height / 2),
                Align.BottomLeft => -new Vec2I(0, height - 1),
                Align.BottomMiddle => -new Vec2I(width / 2, height - 1),
                Align.BottomRight => -new Vec2I(width - 1, height - 1),
                _ => throw new Exception($"Unsupported image alignment: {imageAlign}")
            };

            return windowPos + imageOffset + offset;
        }

        public void TranslateDoomOffset(ref Vec2I offset, in Dimension dimension)
        {
            offset.X = (offset.X / 2) - (dimension.Width / 2);
            offset.Y = -offset.Y - dimension.Height;
        }
    }
}