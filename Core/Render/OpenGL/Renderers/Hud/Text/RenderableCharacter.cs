using Helion.Geometry.Boxes;

namespace Helion.Render.OpenGL.Renderers.Hud.Text
{
    public readonly struct RenderableCharacter
    {
        public readonly Box2I Area;
        public readonly Box2F UV;

        public RenderableCharacter(Box2I area, Box2F uv)
        {
            Area = area;
            UV = uv;
        }
    }
}
