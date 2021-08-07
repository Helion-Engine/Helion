using Helion.Geometry.Boxes;

namespace Helion.Render.OpenGL.Renderers.Hud.Text
{
    public readonly struct RenderableSentence
    {
        public readonly int StartIndex;
        public readonly int Count;
        public readonly Box2I Bounds;

        public RenderableSentence(int startIndex, int count, Box2I bounds)
        {
            StartIndex = startIndex;
            Count = count;
            Bounds = bounds;
        }
    }
}
