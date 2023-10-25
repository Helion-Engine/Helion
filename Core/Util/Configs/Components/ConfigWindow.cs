using Helion.Geometry;
using Helion.Util.Configs.Options;
using Helion.Util.Configs.Values;
using OpenTK.Windowing.Common;
using System.ComponentModel;
using static Helion.Util.Configs.Values.ConfigFilters;

namespace Helion.Util.Configs.Components;

public enum RenderWindowState
{
    [Description("Window")]
    Normal,
    Fullscreen,
}

public class ConfigWindowVirtual
{
    [ConfigInfo("The width and height of the virtual dimension.")]
    [OptionMenu(OptionSectionType.Video, "Virtual size", spacer: true)]
    public readonly ConfigValue<Dimension> Dimension = new((800, 600), (_, dim) => dim.Width >= 320 && dim.Height >= 200);

    [ConfigInfo("Whether virtual dimensions should be used or not.")]
    [OptionMenu(OptionSectionType.Video, "Use virutal size")]
    public readonly ConfigValue<bool> Enable = new(false);

    [ConfigInfo("Will stretch the image if widescreen, otherwise will render black bars on the side.")]
    [OptionMenu(OptionSectionType.Video, "Stretch virtual size")]
    public readonly ConfigValue<bool> Stretch = new(false);
}

public class ConfigWindow
{
    [ConfigInfo("The state of the window, such as if it is fullscreen or windowed.")]
    [OptionMenu(OptionSectionType.Video, "Fullscreen/Window")]
    public readonly ConfigValue<RenderWindowState> State = new(RenderWindowState.Fullscreen, OnlyValidEnums<RenderWindowState>());

    [ConfigInfo("The width and height of the window.")]
    [OptionMenu(OptionSectionType.Video, "Window size")]
    public readonly ConfigValue<Dimension> Dimension = new((1024, 768), (_, dim) => dim.HasPositiveArea);

    [ConfigInfo("The border of the window.")]
    [OptionMenu(OptionSectionType.Video, "Border")]
    public readonly ConfigValue<WindowBorder> Border = new(WindowBorder.Resizable, OnlyValidEnums<WindowBorder>());

    public readonly ConfigWindowVirtual Virtual = new();

    [ConfigInfo("The display number for the window. (0 = default. Use command ListDisplays for display numbers).")]
    [OptionMenu(OptionSectionType.Video, "Display Number [0 default]", spacer: true)]
    public readonly ConfigValue<int> Display = new(0, GreaterOrEqual(0));
}
