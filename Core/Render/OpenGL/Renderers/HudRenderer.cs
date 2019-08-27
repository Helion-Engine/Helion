using System;
using System.Drawing;
using Helion.Util;
using Helion.Util.Geometry;

namespace Helion.Render.OpenGL.Renderers
{
    public abstract class HudRenderer : IDisposable
    {
        public abstract void Clear();
        public abstract void Dispose();
        public abstract void DrawImage(CIString textureName, Vec2I topLeft, Color mixColor, Color multiplyColor, float alpha);
        public abstract void DrawImage(CIString textureName, Rectangle drawArea, Color mixColor, Color multiplyColor, float alpha);
        public abstract void DrawText(string text, CIString fontName, Vec2I topLeft, Color mixColor, Color multiplyColor, float alpha);
        public abstract void Render(Rectangle viewport);
    }
}