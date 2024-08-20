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

    protected override void RenderDialogContents(IRenderableSurfaceContext ctx, IHudRenderContext hud)
    {
        hud.AddOffset((m_dialogOffset.X + m_padding, 0));

        RenderDialogText(hud, m_title, Color.Red);

        foreach (var message in m_message)
        {
            RenderDialogText(hud, message);
        }
    }
}
