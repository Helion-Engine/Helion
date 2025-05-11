using Helion.Graphics;
using Helion.Render.Common.Renderers;
using Helion.Util.Configs.Components;
using System.Collections.Generic;

namespace Helion.Layer.Options.Dialogs;

internal class MessageDialog(ConfigWindow config, string title, IList<string> message, string? acceptButton, string? cancelButton)
    : DialogBase(config, acceptButton, cancelButton)
{
    private readonly string m_title = title;
    private readonly IList<string> m_message = message;
    private readonly List<string> m_messageFormatted = [];
    private readonly List<string> m_lines = [];

    protected override void RenderDialogContents(IRenderableSurfaceContext ctx, IHudRenderContext hud, bool sizeChanged)
    {
        hud.AddOffset((m_dialogOffset.X + m_padding, 0));

        RenderDialogText(hud, m_title, Color.Red);

        if (sizeChanged)
        {
            m_messageFormatted.Clear();
            m_lines.Clear();

            foreach (string str in m_message)
            {
                if (str.Length == 0)
                {
                    m_messageFormatted.Add(str);
                    continue;
                }

                WrapTextToDialogWidth(str, hud, m_lines);
                m_messageFormatted.AddRange(m_lines);
            }
        }

        foreach (var message in m_messageFormatted)
        {
            RenderDialogText(hud, message);
        }
    }
}
