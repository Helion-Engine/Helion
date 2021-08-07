using Helion.Render.Common;

namespace Helion.Render.OpenGL.Renderers.Hud.Text
{
    public readonly struct RenderableSentence
    {
        public readonly int StartIndex;
        public readonly int Count;
        public readonly HudBox Bounds;

        public RenderableSentence(int startIndex, int count, HudBox bounds)
        {
            StartIndex = startIndex;
            Count = count;
            Bounds = bounds;
        }

        public override string ToString() => $"Index: {StartIndex}, Count: {Count}, Bounds: {Bounds}";
    }
}
