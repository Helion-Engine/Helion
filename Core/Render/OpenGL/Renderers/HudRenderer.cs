using System;
using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Graphics.Geometry;
using Helion.Render.OpenGL.Renderers.Legacy.World;
using Helion.Render.OpenGL.Texture.Fonts;

namespace Helion.Render.OpenGL.Renderers;

public abstract class HudRenderer : IDisposable
{
    public abstract void Clear();
    public abstract void Dispose();
    public abstract void DrawImage(string textureName, Vec2I topLeft, Color multiplyColor, float alpha, bool drawInvul, bool drawFuzz, bool drawColorMap);
    public abstract void DrawImage(string textureName, ImageBox2I drawArea, Color multiplyColor, float alpha, bool drawInvul, bool drawFuzz, bool drawColorMap);
    public abstract void DrawShape(ImageBox2I area, Color color, float alpha);
    public abstract void DrawText(RenderableString text, ImageBox2I drawArea, float alpha, bool drawColorMap);
    public abstract void Render(Rectangle viewport, ShaderUniforms uniforms);
}
