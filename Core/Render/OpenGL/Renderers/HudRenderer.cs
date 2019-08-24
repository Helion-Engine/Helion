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
        public abstract void AddImage(CIString textureName, Vec2I topLeft, float alpha);
        public abstract void AddImage(CIString textureName, Rectangle drawArea, float alpha);
        public abstract void Render();
    }
}