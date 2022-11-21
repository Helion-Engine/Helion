using Helion;
using Helion.Render;
using Helion.Render.Renderers.Hud;

namespace Helion.Render.Renderers.Hud;

public readonly struct HudQuad
{
    public readonly HudVertex TopLeft;
    public readonly HudVertex TopRight;
    public readonly HudVertex BottomLeft;
    public readonly HudVertex BottomRight;

    public HudQuad(HudVertex topLeft, HudVertex topRight, HudVertex bottomLeft, HudVertex bottomRight)
    {
        TopLeft = topLeft;
        TopRight = topRight;
        BottomLeft = bottomLeft;
        BottomRight = bottomRight;
    }
}
