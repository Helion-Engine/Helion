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

    private readonly List<string> m_lines = [];
    private readonly StringBuilder m_builder = new();

    public override void Render(IRenderableSurfaceContext ctx, IHudRenderContext hud)
    {
        base.Render(ctx, hud);

        int height = hud.MeasureText("I", Font, m_fontSize).Height;
        hud.PushOffset((m_dialogOffset.X + m_padding, m_dialogOffset.Y + m_padding));

        LineWrap.Calculate(m_title, Font, m_fontSize, m_box.Width, hud, m_lines, m_builder, out _);
        foreach (var line in m_lines)
        {
            hud.Text(line, Font, m_fontSize, (0, 0), color: Color.Red);
            hud.AddOffset((0, height + m_padding));
        }

        hud.AddOffset((0, height));

        foreach (var message in m_message)
        {
            if (message.Length == 0)
                hud.AddOffset((0, height));

            LineWrap.Calculate(message, Font, m_fontSize, m_box.Width, hud, m_lines, m_builder, out _);
            foreach (var line in m_lines)
            {
                hud.Text(line, Font, m_fontSize, (0, 0));
                hud.AddOffset((0, height + m_padding));
            }
        }

        hud.PopOffset();
    }
}
