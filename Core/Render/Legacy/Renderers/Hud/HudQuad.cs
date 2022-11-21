using Helion;
using Helion.Render;
using Helion.Render.Legacy;
using Helion.Render.Legacy.Renderers;
using Helion.Render.Legacy.Renderers.Hud;

namespace Helion.Render.Legacy.Renderers.Hud;

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
