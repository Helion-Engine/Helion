using Helion.Geometry;
using Helion.Geometry.Vectors;
using Helion.Graphics;
using Helion.Layer.Options.Sections;
using Helion.Render.Common.Enums;
using Helion.Render.Common.Renderers;
using Helion.Util.Configs.Components;
using Helion.Util.Configs.Extensions;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using Helion.Window;
using Helion.Window.Input;
using System;
using System.IO;

namespace Helion.Layer.Options;

internal class FileListDialog : DialogBase
{
    const string Selector = ">";

    private int m_rowHeight;
    private int m_valueStartX;
    private Dimension m_selectorSize;

    private readonly IConfigValue m_configValue;
    private readonly OptionMenuAttribute m_attr;
    private int m_row;
    
    private FileInfo m_file;
    private DirectoryInfo m_directory;

    public FileInfo SelectedFile => m_file;
    public IConfigValue ConfigValue => m_configValue;

    public FileListDialog(ConfigHud config, IConfigValue configValue, OptionMenuAttribute attr)
        : base(config, "OK", "Cancel")
    {
        m_configValue = configValue;
        m_attr = attr;
        m_file = (FileInfo)ConfigValue.ObjectValue;
        try
        {
            m_directory = m_file.Directory ?? new DirectoryInfo(AppContext.BaseDirectory);
        }
        catch
        {
            // Handle directory-not-found and security exceptions that File::Directory might throw
            m_directory = new DirectoryInfo(AppContext.BaseDirectory);
        }
    }

    public override void HandleInput(IConsumableInput input)
    {
        base.HandleInput(input);

        if (input.ConsumePressOrContinuousHold(Key.Down))
            m_row = ++m_row;
        if (input.ConsumePressOrContinuousHold(Key.Up))
            m_row = --m_row;
    }

    public override void Render(IRenderableSurfaceContext ctx, IHudRenderContext hud)
    {
        base.Render(ctx, hud);

        //m_fontSize = m_config.GetSmallFontSize();
        //m_padding = m_config.GetScaled(8);
        //int border = m_config.GetScaled(1);
        //var size = new Dimension(Math.Max(hud.Width / 2, 320), Math.Max(hud.Height / 2, 200));
        //hud.FillBox((0, 0, hud.Width, hud.Height), Color.Black, alpha: 0.5f);

        //hud.FillBox((0, 0, size.Width, size.Height), Color.Gray, window: Align.Center, anchor: Align.Center);
        //hud.FillBox((0, 0, size.Width - (border * 2), size.Height - (border * 2)), Color.Black, window: Align.Center, anchor: Align.Center);

        //m_selectorSize = hud.MeasureText(Selector, Font, m_fontSize);
        //m_rowHeight = hud.MeasureText("I", Font, m_fontSize).Height;
        //m_valueStartX = hud.MeasureText("Green", Font, m_fontSize).Width + m_padding * 4;

        //hud.PushOffset((0, m_dialogOffset.Y + m_padding));

        //// Draw the option name
        //hud.Text(m_attr.Name, Font, m_fontSize, (0, 0), window: Align.TopMiddle, anchor: Align.TopMiddle);
        //hud.AddOffset((0, m_rowHeight + m_padding));
        //hud.Text($"Look in: {m_directory}", Font, m_fontSize, (0, 0), window: Align.TopLeft, anchor: Align.TopLeft);
        ////yhud.Text(m_configValue.ObjectValue.ToString() ?? string.Empty, Font, m_fontSize, (0, 0), window: Align.TopMiddle, anchor: Align.TopMiddle);




        hud.PopOffset();
    }



}
