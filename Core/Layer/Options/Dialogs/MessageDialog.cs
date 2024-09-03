using Helion.Graphics;
using Helion.Render.Common.Renderers;
using Helion.Util.Configs.Components;
using System.Collections.Generic;

namespace Helion.Layer.Options.Dialogs;

internal class MessageDialog(ConfigHud config, string title, IList<string> message, string? acceptButton, string? cancelButton)
    : DialogBase(config, acceptButton, cancelButton)
{
    private readonly string m_title = title;
    private readonly IList<string> m_message = message;
    private readonly List<string> m_messageFormatted = new List<string>();

    protected override void RenderDialogContents(IRenderableSurfaceContext ctx, IHudRenderContext hud, bool sizeChanged)
    {
        hud.AddOffset((m_dialogOffset.X + m_padding, 0));

        RenderDialogText(hud, m_title, Color.Red);

        if (sizeChanged)
        {
            m_messageFormatted.Clear();

            List<string> temp = new List<string>();
            foreach (string str in m_message)
            {
                WrapTextToDialogWidth(str, hud, temp);
                m_messageFormatted.AddRange(temp);
            }
        }

        foreach (var message in m_messageFormatted)
        {
            RenderDialogText(hud, message);
        }
    }
}
