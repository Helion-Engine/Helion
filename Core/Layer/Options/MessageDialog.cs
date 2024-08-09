using Helion.Graphics;
using Helion.Render.Common.Renderers;
using Helion.Util.Configs.Components;
using System.Collections.Generic;

namespace Helion.Layer.Options;

internal class MessageDialog(ConfigHud config, string title, IList<string> message, string? acceptButton, string? cancelButton) 
    : DialogBase(config, acceptButton, cancelButton)
{
    private readonly string m_title = title;
    private readonly IList<string> m_message = message;

    public override void Render(IRenderableSurfaceContext ctx, IHudRenderContext hud)
    {
        base.Render(ctx, hud);

        int height = hud.MeasureText("I", Font, m_fontSize).Height;
        hud.PushOffset((m_dialogOffset.X + m_padding, m_dialogOffset.Y + m_padding));

        hud.Text(m_title, Font, m_fontSize, (0, 0), color: Color.Red);
        hud.AddOffset((0, height * 2));

        foreach (var message in m_message)
        {
            hud.Text(message, Font, m_fontSize, (0, 0));
            hud.AddOffset((0, height));
        }

        hud.PopOffset();
    }
}
