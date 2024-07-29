using Helion.Geometry.Vectors;
using Helion.Render.Common.Renderers;
using System;

namespace Helion.Layer.Options;

public record struct DialogCloseArgs(bool Accepted);

internal interface IDialog : IGameLayer
{
    public event EventHandler<DialogCloseArgs>? OnClose;
    public void Render(IRenderableSurfaceContext ctx, IHudRenderContext hud);
    public bool OnClickableItem(Vec2I mousePosition);
}
