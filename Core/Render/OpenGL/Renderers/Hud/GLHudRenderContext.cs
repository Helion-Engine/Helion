using System.Drawing;
using Helion.Geometry;
using Helion.Geometry.Boxes;
using Helion.Geometry.Segments;
using Helion.Geometry.Vectors;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;

namespace Helion.Render.OpenGL.Renderers.Hud
{
    public class GLHudRenderContext : IHudRenderContext
    {
        internal void Begin()
        {
            throw new System.NotImplementedException();
        }
        
        internal void End()
        {
            throw new System.NotImplementedException();
        }
        
        public void Clear(Color color)
        {
            throw new System.NotImplementedException();
        }

        public void Point(Vec2I point, Color color, Align window = Align.TopLeft)
        {
            throw new System.NotImplementedException();
        }

        public void Points(Vec2I[] points, Color color, Align window = Align.TopLeft)
        {
            throw new System.NotImplementedException();
        }

        public void Line(Seg2D seg, Color color, Align window = Align.TopLeft)
        {
            throw new System.NotImplementedException();
        }

        public void Lines(Seg2D[] segs, Color color, Align window = Align.TopLeft)
        {
            throw new System.NotImplementedException();
        }

        public void DrawBox(Box2I box, Color color, Align window = Align.TopLeft)
        {
            throw new System.NotImplementedException();
        }

        public void DrawBoxes(Box2I[] boxes, Color color, Align window = Align.TopLeft)
        {
            throw new System.NotImplementedException();
        }

        public void FillBox(Box2I box, Color color, Align window = Align.TopLeft)
        {
            throw new System.NotImplementedException();
        }

        public void FillBoxes(Box2I[] boxes, Color color, Align window = Align.TopLeft)
        {
            throw new System.NotImplementedException();
        }

        public void Image(string texture, Vec2I origin, Dimension? dimension = null, Align window = Align.TopLeft,
            Align image = Align.TopLeft, Align? both = null, Color? color = null, float alpha = 1)
        {
            throw new System.NotImplementedException();
        }

        public void Text(string text, string font, int height, Vec2I origin, Color? color = null)
        {
            throw new System.NotImplementedException();
        }

        public void PushVirtualDimension(Dimension dimension, ResolutionScale? scale = null)
        {
            throw new System.NotImplementedException();
        }

        public void PopVirtualDimension()
        {
            throw new System.NotImplementedException();
        }

        public void Render(Dimension viewport)
        {
            throw new System.NotImplementedException();
        }
        
        public void Dispose()
        {
            throw new System.NotImplementedException();
        }
    }
}
