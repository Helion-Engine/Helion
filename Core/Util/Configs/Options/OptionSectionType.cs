﻿namespace Helion.Util.Configs.Options;

// Ordered by which should be shown on pages before others. Lower enum
// values are shown before higher enum values when rendering.
public enum OptionSectionType
{
    Keys,
    Mouse,
    General,
    Video,
    Audio,
    Render,
    Hud,
    Compatibility,
    SlowTick,
    Demo,
    Console,
}