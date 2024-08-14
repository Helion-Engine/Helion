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
    [ConfigInfo("Virtual screen size.")]
    [OptionMenu(OptionSectionType.Video, "Virtual Size", spacer: true)]
    public readonly ConfigValue<Dimension> Dimension = new((800, 600), (_, dim) => dim.Width >= 320 && dim.Height >= 200);

    [ConfigInfo("Use virtual screen size.")]
    [OptionMenu(OptionSectionType.Video, "Use Virtual Size")]
    public readonly ConfigValue<bool> Enable = new(false);

    [ConfigInfo("Stretch the image if widescreen, or render black bars on the sides.")]
    [OptionMenu(OptionSectionType.Video, "Stretch Virtual Size")]
    public readonly ConfigValue<bool> Stretch = new(false);
}

public class ConfigWindow
{
    [ConfigInfo("Display fullscreen or windowed.")]
    [OptionMenu(OptionSectionType.Video, "Fullscreen/Window")]
    public readonly ConfigValue<RenderWindowState> State = new(RenderWindowState.Fullscreen, OnlyValidEnums<RenderWindowState>());

    [ConfigInfo("Window border.")]
    [OptionMenu(OptionSectionType.Video, "Border")]
    public readonly ConfigValue<WindowBorder> Border = new(WindowBorder.Resizable, OnlyValidEnums<WindowBorder>());

    [ConfigInfo("Window width and height.")]
    [OptionMenu(OptionSectionType.Video, "Window Size")]
    public readonly ConfigValue<Dimension> Dimension = new((1024, 768), (_, dim) => dim.Width >= 320 && dim.Height >= 200);

    public readonly ConfigWindowVirtual Virtual = new();

    [ConfigInfo("Display number for the window. (0 = default. Use command ListDisplays for display numbers.)")]
    [OptionMenu(OptionSectionType.Video, "Display Number", spacer: true)]
    public readonly ConfigValue<int> Display = new(0, GreaterOrEqual(0));

    [ConfigInfo("Color rendering mode: Palette uses Doom's colormaps and disables texture filtering, producing output that resembles software rendering. True Color interpolates color values. Application restart required.", restartRequired: true)]
    [OptionMenu(OptionSectionType.Video, "Color Mode")]
    public readonly ConfigValue<RenderColorMode> ColorMode = new(RenderColorMode.TrueColor);
}
