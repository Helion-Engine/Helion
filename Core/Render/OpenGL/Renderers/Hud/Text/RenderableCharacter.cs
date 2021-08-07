using Helion.Geometry.Boxes;
using Helion.Render.Common;

namespace Helion.Render.OpenGL.Renderers.Hud.Text
{
    public readonly struct RenderableCharacter
    {
        public readonly char Character;
        public readonly HudBox Area;
        public readonly Box2F UV;

        public RenderableCharacter(char character, HudBox area, Box2F uv)
        {
            Character = character;
            Area = area;
            UV = uv;
        }

        public override string ToString() => $"{Character}, Area: {Area}, UV: {UV}";
    }
}
