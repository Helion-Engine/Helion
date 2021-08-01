using System;
using System.Drawing;
using Helion.Geometry.Vectors;
using Helion.Graphics.Geometry;
using Helion.Render.Legacy.Texture.Fonts;

namespace Helion.Render.Legacy.Renderers
{
    public abstract class HudRenderer : IDisposable
    {
        public abstract void Clear();
        public abstract void Dispose();
        public abstract void DrawImage(string textureName, Vec2I topLeft, Color multiplyColor, float alpha, bool drawInvul);
        public abstract void DrawImage(string textureName, ImageBox2I drawArea, Color multiplyColor, float alpha, bool drawInvul);
        public abstract void DrawShape(ImageBox2I area, Color color, float alpha);
        public abstract void DrawText(RenderableString text, ImageBox2I drawArea, float alpha);
        public abstract void Render(Rectangle viewport);
    }
}