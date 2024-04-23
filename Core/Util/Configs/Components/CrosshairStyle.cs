using System.ComponentModel;

namespace Helion.Util.Configs.Components;

public enum CrosshairStyle
{
    [Description("Cross 1")]
    Cross1,
    [Description("Cross 2")]
    Cross2,
    [Description("Cross 3")]
    Cross3,
    [Description("Dot")]
    Dot,
}

public enum CrossColor
{
    Green,
    White,
    Red,
    Blue,
    Yellow
}