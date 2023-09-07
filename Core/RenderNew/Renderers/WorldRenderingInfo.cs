using Helion.RenderNew.Util;

namespace Helion.RenderNew.Renderers;

public readonly record struct WorldRenderingInfo(Camera Camera, int Gametick, float TickFraction)
{
}
