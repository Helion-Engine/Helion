using System.Drawing;
using Helion.Graphics.String;
using Helion.Render.Commands;
using Helion.Render.Commands.Align;
using Helion.Util.Geometry;

namespace Helion.Render.Shared.Drawers.Helper
{
    public class DrawHelper
    {
        private const float Opaque = 1.0f;
        private static readonly Color NoColor = Color.Transparent;

        private readonly RenderCommands m_renderCommands;
        private readonly IImageDrawInfoProvider m_drawInfoProvider;

        public DrawHelper(RenderCommands renderCommands)
        {
            m_renderCommands = renderCommands;
            m_drawInfoProvider = renderCommands.ImageDrawInfoProvider;
        }
        
        public bool ImageExists(string name) => m_drawInfoProvider.ImageExists(name);

        public int FontHeight(string fontName) => m_drawInfoProvider.GetFontHeight(fontName);
        
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
            var (width, height) = m_drawInfoProvider.GetImageDimension(name);
            Image(name, x, y, width, height, Alignment.TopLeft, NoColor, Opaque);
        }
        
        public void Image(string name, int x, int y, int width, int height)
        {
            Image(name, x, y, width, height, Alignment.TopLeft, NoColor, Opaque);
        }

        public void Image(string name, int x, int y, Alignment align)
        {
            var (width, height) = m_drawInfoProvider.GetImageDimension(name);
            Image(name, x, y, width, height, align, NoColor, Opaque);
        }

        public void Image(string name, int x, int y, Color color)
        {
            var (width, height) = m_drawInfoProvider.GetImageDimension(name);
            Image(name, x, y, width, height, Alignment.TopLeft, color, Opaque);
        }

        public void Image(string name, int x, int y, float alpha)
        {
            var (width, height) = m_drawInfoProvider.GetImageDimension(name);
            Image(name, x, y, width, height, Alignment.TopLeft, NoColor, alpha);
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
            var (width, height) = m_drawInfoProvider.GetImageDimension(name);
            Image(name, x, y, width, height, align, color, Opaque);
        }
        
        public void Image(string name, int x, int y, Alignment align, float alpha)
        {
            var (width, height) = m_drawInfoProvider.GetImageDimension(name);
            Image(name, x, y, width, height, align, NoColor, alpha);
        }
        
        public void Image(string name, int x, int y, Color color, float alpha)
        {
            var (width, height) = m_drawInfoProvider.GetImageDimension(name);
            Image(name, x, y, width, height, Alignment.TopLeft, color, alpha);
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
            var (width, height) = m_drawInfoProvider.GetImageDimension(name);
            Image(name, x, y, width, height, align, color, alpha);
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
            Text(ColoredStringBuilder.From(color, text), font, fontSize, x, y, Alignment.TopLeft, TextAlignment.TopLeft, 
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
            drawArea = m_drawInfoProvider.GetDrawArea(text, font, fontSize, maxWidth, wrap);
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