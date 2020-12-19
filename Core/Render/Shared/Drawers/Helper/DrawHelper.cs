using System.Drawing;
using Helion.Graphics.String;
using Helion.Render.Commands;
using Helion.Render.Commands.Align;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;

namespace Helion.Render.Shared.Drawers.Helper
{
    public class DrawHelper
    {
        private const float Opaque = 1.0f;
        private const float DoomViewAspectRatio = 640.0f / 480.0f;
        private const float DoomDrawAspectRatio = 320.0f / 200.0f;
        private const float DoomDrawWidth = 320.0f;
        private const float DoomDrawHeight = 200.0f;
        private static readonly Color NoColor = Color.Transparent;

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
        
        public void FillRect(int x, int y, int width, int height, Color color)
        {
            FillRect(x, y, width, height, color, Opaque);
        }
        
        public void FillRect(int x, int y, int width, int height, Color color, float alpha)
        {
            m_renderCommands.FillRect(new Rectangle(x, y, width, height), color, alpha);
        }

        public void Image(string name, int x, int y)
        {
            var (width, height) = DrawInfoProvider.GetImageDimension(name);
            Image(name, x, y, width, height, Alignment.TopLeft, NoColor, Opaque);
        }
        
        public void Image(string name, int x, int y, out Dimension area)
        {
            area = DrawInfoProvider.GetImageDimension(name);
            Image(name, x, y, area.Width, area.Height, Alignment.TopLeft, NoColor, Opaque);
        }
        
        public void Image(string name, int x, int y, int width, int height)
        {
            Image(name, x, y, width, height, Alignment.TopLeft, NoColor, Opaque);
        }

        public void Image(string name, int x, int y, Alignment align)
        {
            var (width, height) = DrawInfoProvider.GetImageDimension(name);
            Image(name, x, y, width, height, align, NoColor, Opaque);
        }
        
        public void Image(string name, int x, int y, Alignment align, out Dimension area)
        {
            area = DrawInfoProvider.GetImageDimension(name);
            Image(name, x, y, area.Width, area.Height, align, NoColor, Opaque);
        }

        public void Image(string name, int x, int y, Color color)
        {
            var (width, height) = DrawInfoProvider.GetImageDimension(name);
            Image(name, x, y, width, height, Alignment.TopLeft, color, Opaque);
        }
        
        public void Image(string name, int x, int y, Color color, out Dimension area)
        {
            area = DrawInfoProvider.GetImageDimension(name);
            Image(name, x, y, area.Width, area.Height, Alignment.TopLeft, color, Opaque);
        }

        public void Image(string name, int x, int y, float alpha)
        {
            var (width, height) = DrawInfoProvider.GetImageDimension(name);
            Image(name, x, y, width, height, Alignment.TopLeft, NoColor, alpha);
        }
        
        public void Image(string name, int x, int y, float alpha, out Dimension area)
        {
            area = DrawInfoProvider.GetImageDimension(name);
            Image(name, x, y, area.Width, area.Height, Alignment.TopLeft, NoColor, alpha);
        }

        public void Image(string name, int x, int y, int width, int height, Alignment align)
        {
            Image(name, x, y, width, height, align, NoColor, Opaque);
        }
        
        public void Image(string name, int x, int y, int width, int height, Color color)
        {
            Image(name, x, y, width, height, Alignment.TopLeft, color, Opaque);
        }
        
        public void Image(string name, int x, int y, int width, int height, float alpha)
        {
            Image(name, x, y, width, height, Alignment.TopLeft, NoColor, alpha);
        }
        
        public void Image(string name, int x, int y, Alignment align, Color color)
        {
            var (width, height) = DrawInfoProvider.GetImageDimension(name);
            Image(name, x, y, width, height, align, color, Opaque);
        }
        
        public void Image(string name, int x, int y, Alignment align, Color color, out Dimension area)
        {
            area = DrawInfoProvider.GetImageDimension(name);
            Image(name, x, y, area.Width, area.Height, align, color, Opaque);
        }
        
        public void Image(string name, int x, int y, Alignment align, float alpha)
        {
            var (width, height) = DrawInfoProvider.GetImageDimension(name);
            Image(name, x, y, width, height, align, NoColor, alpha);
        }
        
        public void Image(string name, int x, int y, Alignment align, float alpha, out Dimension area)
        {
            area = DrawInfoProvider.GetImageDimension(name);
            Image(name, x, y, area.Width, area.Height, align, NoColor, alpha);
        }
        
        public void Image(string name, int x, int y, Color color, float alpha)
        {
            var (width, height) = DrawInfoProvider.GetImageDimension(name);
            Image(name, x, y, width, height, Alignment.TopLeft, color, alpha);
        }
        
        public void Image(string name, int x, int y, Color color, float alpha, out Dimension area)
        {
            area = DrawInfoProvider.GetImageDimension(name);
            Image(name, x, y, area.Width, area.Height, Alignment.TopLeft, color, alpha);
        }
        
        public void Image(string name, int x, int y, int width, int height, Alignment align, Color color)
        {
            Image(name, x, y, width, height, align, color, Opaque);
        }
        
        public void Image(string name, int x, int y, int width, int height, Alignment align, float alpha)
        {
            Image(name, x, y, width, height, align, NoColor, alpha);
        }
        
        public void Image(string name, int x, int y, int width, int height, Color color, float alpha)
        {
            Image(name, x, y, width, height, Alignment.TopLeft, color, alpha);
        }
        
        public void Image(string name, int x, int y, Alignment align, Color color, float alpha)
        {
            var (width, height) = DrawInfoProvider.GetImageDimension(name);
            Image(name, x, y, width, height, align, color, alpha);
        }
        
        public void Image(string name, int x, int y, Alignment align, Color color, float alpha, out Dimension area)
        {
            area = DrawInfoProvider.GetImageDimension(name);
            Image(name, x, y, area.Width, area.Height, align, color, alpha);
        }
        
        public void Image(string name, int x, int y, int width, int height, Alignment align, Color color, float alpha)
        {
            int left = x;
            int top = y;
            
            switch (align)
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

            m_renderCommands.DrawImage(name, left, top, width, height, color, alpha);
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