using System;
using System.Drawing;
using Helion.Graphics.String;
using Helion.Util;
using Helion.Util.Geometry;
using Helion.Util.Geometry.Vectors;

namespace Helion.Render.OpenGL.Renderers
{
    public abstract class HudRenderer : IDisposable
    {
        public abstract void Clear();
        public abstract void Dispose();
        public abstract void DrawImage(CIString textureName, Vec2I topLeft, Color mixColor, Color multiplyColor, float alpha);
        public abstract void DrawImage(CIString textureName, Rectangle drawArea, Color mixColor, Color multiplyColor, float alpha);
        public abstract void DrawShape(Rectangle area, Color color, float alpha);
        public abstract void DrawText(ColoredString text, CIString fontName, int fontHeight, Vec2I topLeft, float alpha);
        public abstract void Render(Rectangle viewport);
    }
}