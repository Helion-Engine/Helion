using Helion.Graphics;
using Helion.Render.Common.Renderers;
using Helion.Util.Configs.Components;
using Helion.World.Geometry.Lines;
using System.Collections.Generic;
using System.Text;

namespace Helion.Layer.Options;

internal class MessageDialog(ConfigHud config, string title, IList<string> message, string? acceptButton, string? cancelButton) 
    : DialogBase(config, acceptButton, cancelButton)
{
    private readonly string m_title = title;
    private readonly IList<string> m_message = message;

    protected override void RenderImpl(IRenderableSurfaceContext ctx, IHudRenderContext hud)
    {
        hud.PushOffset((m_dialogOffset.X + m_padding, m_dialogOffset.Y + m_padding));

        PrintMessage(hud, m_title, Color.Red);

        foreach (var message in m_message)
        {
            PrintMessage(hud, message);
        }
    }
}
