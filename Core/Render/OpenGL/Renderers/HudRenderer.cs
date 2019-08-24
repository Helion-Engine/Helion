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
        public abstract void AddImage(CIString textureName, Vec2I topLeft, Color color, float alpha);
        public abstract void AddImage(CIString textureName, Rectangle drawArea, Color color, float alpha);
        public abstract void Render(Rectangle viewport);
    }
}