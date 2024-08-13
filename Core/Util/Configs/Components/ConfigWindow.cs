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
    [OptionMenu(OptionSectionType.Video, "Virtual Size", spacer: true)]
    public readonly ConfigValue<Dimension> Dimension = new((800, 600), (_, dim) => dim.Width >= 320 && dim.Height >= 200);

    [ConfigInfo("Whether virtual dimensions should be used or not.")]
    [OptionMenu(OptionSectionType.Video, "Use Virtual Size")]
    public readonly ConfigValue<bool> Enable = new(false);

    [ConfigInfo("Will stretch the image if widescreen, otherwise will render black bars on the side.")]
    [OptionMenu(OptionSectionType.Video, "Stretch Virtual Size")]
    public readonly ConfigValue<bool> Stretch = new(false);
}

public class ConfigWindow
{
    [ConfigInfo("Whether display should be fullscreen or windowed.")]
    [OptionMenu(OptionSectionType.Video, "Fullscreen/Window")]
    public readonly ConfigValue<RenderWindowState> State = new(RenderWindowState.Fullscreen, OnlyValidEnums<RenderWindowState>());

    [ConfigInfo("Window border.")]
    [OptionMenu(OptionSectionType.Video, "Border")]
    public readonly ConfigValue<WindowBorder> Border = new(WindowBorder.Resizable, OnlyValidEnums<WindowBorder>());

    [ConfigInfo("Width and height of the window.")]
    [OptionMenu(OptionSectionType.Video, "Window Size")]
    public readonly ConfigValue<Dimension> Dimension = new((1024, 768), (_, dim) => dim.Width >= 320 && dim.Height >= 200);

    public readonly ConfigWindowVirtual Virtual = new();

    [ConfigInfo("Display number for the window. (0 = default. Use command ListDisplays for display numbers).")]
    [OptionMenu(OptionSectionType.Video, "Display Number", spacer: true)]
    public readonly ConfigValue<int> Display = new(0, GreaterOrEqual(0));

    [ConfigInfo("Palette uses Doom's colormaps and disables texture filtering, producing output that resembles software rendering. True Color interpolates color values. Application restart required.", restartRequired: true)]
    [OptionMenu(OptionSectionType.Video, "Color Mode")]
    public readonly ConfigValue<RenderColorMode> ColorMode = new(RenderColorMode.TrueColor);
}
