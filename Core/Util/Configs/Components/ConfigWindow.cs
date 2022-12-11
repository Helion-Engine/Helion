using Helion.Geometry;
using Helion.Util.Configs.Values;
using OpenTK.Windowing.Common;
using static Helion.Util.Configs.Values.ConfigFilters;

namespace Helion.Util.Configs.Components;

public enum RenderWindowState
{
    Normal,
    Maximized,
    Fullscreen
}

public class ConfigWindowVirtual
{
    [ConfigInfo("The width and height of the virtual dimension.")]
    public readonly ConfigValue<Dimension> Dimension = new((800, 600), (_, dim) => dim.HasPositiveArea);

    [ConfigInfo("Whether virtual dimensions should be used or not.")]
    public readonly ConfigValue<bool> Enable = new(false);

    [ConfigInfo("Will stretch the image if widescreen, otherwise will render black bars on the side.")]
    public readonly ConfigValue<bool> Stretch = new(false);
}

public class ConfigWindow
{
    [ConfigInfo("The border of the window.", restartRequired: true)]
    public readonly ConfigValue<WindowBorder> Border = new(WindowBorder.Resizable, OnlyValidEnums<WindowBorder>());

    [ConfigInfo("The width and height of the window.", restartRequired: true)]
    public readonly ConfigValue<Dimension> Dimension = new((1024, 768), (_, dim) => dim.HasPositiveArea);

    public readonly ConfigWindowVirtual Virtual = new();

    [ConfigInfo("The state of the window, such as if it is fullscreen or windowed.", restartRequired: true)]
    public readonly ConfigValue<RenderWindowState> State = new(RenderWindowState.Fullscreen, OnlyValidEnums<RenderWindowState>());

    [ConfigInfo("The display number for the window. (0 = default. Use command ListDisplays for display numbers).", restartRequired: true)]
    public readonly ConfigValue<int> Display = new(0, GreaterOrEqual(0));
}
