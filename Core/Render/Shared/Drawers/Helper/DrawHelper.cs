using System;
using System.Drawing;
using Helion.Graphics.String;
using Helion.Render.Commands;
using Helion.Render.Commands.Align;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;

namespace Helion.Render.Shared.Drawers.Helper
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
        private const float DoomViewAspectRatio = 640.0f / 480.0f;
        private const float DoomDrawAspectRatio = 320.0f / 200.0f;
        private const float DoomDrawWidth = 320.0f;
        private const float DoomDrawHeight = 200.0f;
        private static readonly Color NoColor = Color.White;

        public readonly IImageDrawInfoProvider DrawInfoProvider;
        private readonly RenderCommands m_renderCommands;

        public static void ScaleImageDimensions(Dimension viewport, ref int width, ref int height)
        {
            float viewWidth = viewport.Height * DoomViewAspectRatio;
            float scaleWidth = viewWidth / DoomDrawWidth;
            float scaleHeight = viewport.Height / DoomDrawHeight;
            width = (int)(width * scaleWidth);
            height = (int)(height * scaleHeight);
        }

        public static void ScaleImageOffset(Dimension viewport, ref int x, ref int y)
        {
            float viewWidth = viewport.Height * DoomViewAspectRatio;
            float scaleWidth = viewWidth / DoomDrawWidth;
            float scaleHeight = viewport.Height / DoomDrawHeight;
            x = (int)(x * scaleWidth) + (int)(viewport.Width - viewWidth);
            y = (int)(y * scaleHeight);
        }

        public static Vec2I ScaleWorldOffset(Dimension viewport, in Vec2D offset)
        {
            float viewWidth = viewport.Height * DoomViewAspectRatio;
            float scaleWidth = viewWidth / DoomDrawWidth;
            float scaleHeight = viewport.Height / DoomDrawHeight;
            return new Vec2I((int)(offset.X * scaleWidth), (int)(offset.Y * scaleHeight));
        }

        public DrawHelper(RenderCommands renderCommands)
        {
            m_renderCommands = renderCommands;
            DrawInfoProvider = renderCommands.ImageDrawInfoProvider;
        }

        public bool ImageExists(string name) => DrawInfoProvider.ImageExists(name);

        public int FontHeight(string fontName) => DrawInfoProvider.GetFontHeight(fontName);

        public void Pixel(int x, int y, Color color)
        {
            FillRect(x, y, 1, 1, color);
        }

        public void DrawRect(int x, int y, int width, int height, Color color)
        {
            DrawRect(x, y, width, height, color, Opaque);
        }

        public void DrawRect(int x, int y, int width, int height, Color color, float alpha)
        {
            // TODO: Call FillRect for 4 edge lines 1 pixel thick?
        }

        public void FillRect(int x, int y, int width, int height, Color color, float alpha = Opaque)
        {
            m_renderCommands.FillRect(new Rectangle(x, y, width, height), color, alpha);
        }

        public void AtResolution(int width, int height, Action action)
        {
            ResolutionInfo current = m_renderCommands.ResolutionInfo;
            Dimension dimension = new(width, height);
            ResolutionInfo tempInfo = new() { VirtualDimensions = dimension };
            m_renderCommands.SetVirtualResolution(tempInfo);

            action();

            m_renderCommands.SetVirtualResolution(current);
        }

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

        public void Image(string name, int x, int y, int? width = null, int? height = null,
            Alignment align = Alignment.TopLeft, Color? color = null, float alpha = 1.0f)
        {
            Image(name, x, y, out _, width, height, align, color, alpha);
        }

        public void Image(string name, int x, int y, out Dimension dimension, int? width = null, int? height = null,
            Alignment align = Alignment.TopLeft, Color? color = null, float alpha = 1.0f)
        {
            if (width == null || height == null)
            {
                var (imageWidth, imageHeight) = DrawInfoProvider.GetImageDimension(name);
                dimension = new Dimension(width ?? imageWidth, height ?? imageHeight);
            }
            else
                dimension = new(width.Value, height.Value);

            (int left, int top) = GetDrawingCoordinateFromAlign(x, y, dimension.Width, dimension.Height, align);
            m_renderCommands.DrawImage(name, left, top, dimension.Width, dimension.Height, color ?? NoColor, alpha);
        }

        private static (int left, int top) GetDrawingCoordinateFromAlign(int x, int y, int width, int height, Alignment align)
        {
            return align switch
            {
                Alignment.TopLeft => (x, y),
                Alignment.TopMiddle => (x - (width / 2), y),
                Alignment.TopRight => (x - width, y),
                Alignment.MiddleLeft => (x, y - (height / 2)),
                Alignment.Center => (x - (width / 2), y - (height / 2)),
                Alignment.MiddleRight => (x - width, y - (height / 2)),
                Alignment.BottomLeft => (x, y - height),
                Alignment.BottomMiddle => (x - (width / 2), y - height),
                Alignment.BottomRight => (x - width, y - height),
                _ => throw new Exception($"Unsupported alignment: {align}")
            };
        }

        public void Text(Color color, string text, string font, int fontSize, int x, int y, Alignment locationAlign,
            out Dimension drawArea)
        {
            Text(ColoredStringBuilder.From(color, text), font, fontSize, x, y, locationAlign, TextAlignment.TopLeft, int.MaxValue, false,
                Opaque, out drawArea);
        }

        public void Text(ColoredString text, string font, int fontSize, int x, int y, Alignment locationAlign,
            out Dimension drawArea)
        {
            Text(text, font, fontSize, x, y, locationAlign, TextAlignment.TopLeft, int.MaxValue, false,
                 Opaque, out drawArea);
        }

        public void Text(Color color, string text, string font, int fontSize, int x, int y, Alignment locationAlign,
            TextAlignment textAlign, out Dimension drawArea)
        {
            Text(ColoredStringBuilder.From(color, text), font, fontSize, x, y, locationAlign, textAlign, int.MaxValue, false, Opaque,
                 out drawArea);
        }

        public void Text(ColoredString text, string font, int fontSize, int x, int y, Alignment locationAlign,
            TextAlignment textAlign, out Dimension drawArea)
        {
            Text(text, font, fontSize, x, y, locationAlign, textAlign, int.MaxValue, false, Opaque,
                 out drawArea);
        }

        public void Text(Color color, string text, string font, int fontSize, int x, int y, int maxWidth, bool wrap,
            out Dimension drawArea)
        {
            Text(ColoredStringBuilder.From(color, text), font, fontSize, x, y, Alignment.TopLeft, TextAlignment.TopLeft, maxWidth,
                 wrap, Opaque, out drawArea);
        }

        public void Text(ColoredString text, string font, int fontSize, int x, int y, int maxWidth, bool wrap,
            out Dimension drawArea)
        {
            Text(text, font, fontSize, x, y, Alignment.TopLeft, TextAlignment.TopLeft, maxWidth,
                 wrap, Opaque, out drawArea);
        }

        public void Text(Color color, string text, string font, int fontSize, int x, int y, Alignment locationAlign,
            float alpha, out Dimension drawArea)
        {
            Text(ColoredStringBuilder.From(color, text), font, fontSize, x, y, locationAlign, TextAlignment.TopLeft,
                 int.MaxValue, false, alpha, out drawArea);
        }

        public void Text(ColoredString text, string font, int fontSize, int x, int y, Alignment locationAlign,
            float alpha, out Dimension drawArea)
        {
            Text(text, font, fontSize, x, y, Alignment.TopLeft, TextAlignment.TopLeft,
                 int.MaxValue, false, alpha, out drawArea);
        }

        public void Text(Color color, string text, string font, int fontSize, int x, int y, Alignment locationAlign,
            int maxWidth, bool wrap, out Dimension drawArea)
        {
            Text(ColoredStringBuilder.From(color, text), font, fontSize, x, y, locationAlign,
                 TextAlignment.TopLeft, maxWidth, wrap, Opaque, out drawArea);
        }

        public void Text(ColoredString text, string font, int fontSize, int x, int y, Alignment locationAlign,
            int maxWidth, bool wrap, out Dimension drawArea)
        {
            Text(text, font, fontSize, x, y, locationAlign, TextAlignment.TopLeft, maxWidth, wrap,
                 Opaque, out drawArea);
        }

        public void Text(Color color, string text, string font, int fontSize, int x, int y, Alignment locationAlign,
            TextAlignment textAlign, float alpha, out Dimension drawArea)
        {
            Text(ColoredStringBuilder.From(color, text), font, fontSize, x, y, locationAlign,
                 textAlign, int.MaxValue, false, alpha, out drawArea);
        }

        public void Text(ColoredString text, string font, int fontSize, int x, int y, Alignment locationAlign,
            TextAlignment textAlign, float alpha, out Dimension drawArea)
        {
            Text(text, font, fontSize, x, y, locationAlign, textAlign, int.MaxValue, false, alpha,
                 out drawArea);
        }

        public void Text(Color color, string text, string font, int fontSize, int x, int y, Alignment locationAlign,
            TextAlignment textAlign, int maxWidth, bool wrap, out Dimension drawArea)
        {
            Text(ColoredStringBuilder.From(color, text), font, fontSize, x, y, locationAlign,
                 textAlign, maxWidth, wrap, Opaque, out drawArea);
        }

        public void Text(ColoredString text, string font, int fontSize, int x, int y, Alignment locationAlign,
            TextAlignment textAlign, int maxWidth, bool wrap, out Dimension drawArea)
        {
            Text(text, font, fontSize, x, y, locationAlign, textAlign, maxWidth, wrap, Opaque, out drawArea);
        }

        public void Text(Color color, string text, string font, int fontSize, int x, int y, Alignment locationAlign,
            TextAlignment textAlign, int maxWidth, bool wrap, float alpha, out Dimension drawArea)
        {
            Text(ColoredStringBuilder.From(color, text), font, fontSize, x, y, locationAlign,
                 textAlign, maxWidth, wrap, alpha, out drawArea);
        }

        public void Text(ColoredString text, string font, int fontSize, int x, int y, Alignment locationAlign,
            TextAlignment textAlign, int maxWidth, bool wrap, float alpha, out Dimension drawArea)
        {
            drawArea = DrawInfoProvider.GetDrawArea(text, font, fontSize, maxWidth, wrap);
            (int width, int height) = drawArea;

            int left = x;
            int top = y;

            switch (locationAlign)
            {
            case Alignment.TopLeft:
                // Already done!
                break;
            case Alignment.TopMiddle:
                left = x - (width / 2);
                break;
            case Alignment.TopRight:
                left = x - width;
                break;
            case Alignment.MiddleLeft:
                top = y - (height / 2);
                break;
            case Alignment.Center:
                left = x - (width / 2);
                top = y - (height / 2);
                break;
            case Alignment.MiddleRight:
                left = x - width;
                top = y - (height / 2);
                break;
            case Alignment.BottomLeft:
                top = y - height;
                break;
            case Alignment.BottomMiddle:
                left = x - (width / 2);
                top = y - height;
                break;
            case Alignment.BottomRight:
                left = x - width;
                top = y - height;
                break;
            }

            m_renderCommands.DrawText(text, font, fontSize, left, top, width, height, textAlign, alpha);
        }
    }
}